using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NWScript.Language;
using NWScript.Lexer;

namespace NWScript.Parser
{
  public class Parser
  {
    public CompilationUnit CompilationUnit { get; private set; }

    public List<ILanguageToken> Tokens { get; private set; }

    public List<string> Errors { get; private set; }

    public static Regex FunctionCallRegex = new Regex(@"^(?<function_name>\w+)\((?>(?(param),)(?<param>(?>(?>[^\(\),""]|(?<p>\()|(?<-p>\))|(?(p)[^\(\)]|(?!))|(?(g)(?:""""|[^""]|(?<-g>""))|(?!))|(?<g>"")))*))+\)$", RegexOptions.Compiled);

    public int Parse(string name, string[] sourceData, List<ILanguageToken> tokens)
    {
      CompilationUnit = new CompilationUnit();
      Tokens = tokens;
      Errors = new List<string>();

      {
        // METADATA
        CompilationUnitMetadata metadata = new CompilationUnitMetadata();
        metadata.m_Name = name;
        CompilationUnit.m_Metadata = metadata;
      }

      {
        // DEBUG INFO
        CompilationUnitDebugInfo debugInfo = new CompilationUnitDebugInfo();
        debugInfo.m_SourceData = sourceData;
        CompilationUnit.m_DebugInfo = debugInfo;
      }

      for (int baseIndex = 0; baseIndex < tokens.Count; ++baseIndex)
      {
        int baseIndexLast = baseIndex;

        int err = Parse(ref baseIndex);
        if (err != 0)
        {
          return err;
        }
      }

      return 0;
    }

    private int Parse(ref int baseIndexRef)
    {
      int baseIndexLast = baseIndexRef;

      // This is the root scope.
      //
      // Here it's valid to have either ...
      //
      // - Preprocessor commands
      // - Comments
      // - Functions (declaration or implementation)
      // - Variables (constant or global)
      // - Struct declarations

      {
        // PREPROCESSOR
        NSSNode node = ConstructPreprocessor(ref baseIndexRef);
        if (node != null) CompilationUnit.m_Nodes.Add(node);
        if (baseIndexLast != baseIndexRef) return 0;
      }

      {
        // COMMENT
        NSSNode node = ConstructComment(ref baseIndexRef);
        if (node != null) CompilationUnit.m_Nodes.Add(node);
        if (baseIndexLast != baseIndexRef) return 0;
      }

      {
        // FUNCTION
        NSSNode node = ConstructFunction(ref baseIndexRef);
        if (node != null) CompilationUnit.m_Nodes.Add(node);
        if (baseIndexLast != baseIndexRef) return 0;
      }

      {
        // VARIABLES
        NSSNode node = ConstructLvalueDecl(ref baseIndexRef);
        if (node != null) CompilationUnit.m_Nodes.Add(node);
        if (baseIndexLast != baseIndexRef) return 0;
      }

      {
        // STRUCT DECLARATIONS
        NSSNode node = ConstructStructDeclaration(ref baseIndexRef);
        if (node != null) CompilationUnit.m_Nodes.Add(node);
        if (baseIndexLast != baseIndexRef) return 0;
      }

      {
        // REDUNDANT SEMI COLON
        NSSNode node = ConstructRedundantSemiColon(ref baseIndexRef);
        if (node != null) CompilationUnit.m_Nodes.Add(node);
        if (baseIndexLast != baseIndexRef) return 0;
      }

      if (TraverseNextToken(out ILanguageToken token, ref baseIndexRef) == 0)
      {
        ReportTokenError(token, "Unrecognised / unhandled token");
        return 1;
      }

      return 0;
    }

    private Preprocessor ConstructPreprocessor(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(PreprocessorToken)) return null;

      baseIndexRef = baseIndex;

      return new UnknownPreprocessor {Value = ((PreprocessorToken) token).Data};
    }

    private Comment ConstructComment(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex, false);
      if (err != 0 || token.GetType() != typeof(CommentToken)) return null;
      CommentToken commentToken = (CommentToken) token;

      Comment comment;

      if (commentToken.CommentType == CommentType.LineComment)
      {
        comment = new LineComment {Comment = commentToken.Comment};
      }
      else
      {
        if (!commentToken.Terminated) return null;
        comment = new BlockComment {CommentLines = commentToken.Comment.Split('\n').ToList()};
      }

      baseIndexRef = baseIndex;
      return comment;
    }

    private NSSType ConstructType(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;

      NSSType ret = null;

      switch (((KeywordToken) token).m_Keyword)
      {
        case NssKeywords.Void:
          ret = new VoidType();
          break;
        case NssKeywords.Int:
          ret = new IntType();
          break;
        case NssKeywords.Float:
          ret = new FloatType();
          break;
        case NssKeywords.String:
          ret = new StringType();
          break;
        case NssKeywords.Struct:
        {
          StructType str = new StructType();

          err = TraverseNextToken(out token, ref baseIndex);
          if (err != 0 || token.GetType() != typeof(IdentifierToken)) return null;

          str.TypeName = ((IdentifierToken) token).Identifier;
          ret = str;

          break;
        }

        case NssKeywords.Object:
          ret = new ObjectType();
          break;
        case NssKeywords.Location:
          ret = new LocationType();
          break;
        case NssKeywords.Vector:
          ret = new VectorType();
          break;
        case NssKeywords.ItemProperty:
          ret = new ItemPropertyType();
          break;
        case NssKeywords.Effect:
          ret = new EffectType();
          break;
        case NssKeywords.Talent:
          ret = new TalentType();
          break;
        case NssKeywords.Action:
          ret = new ActionType();
          break;
        case NssKeywords.Event:
          ret = new EventType();
          break;
        case NssKeywords.SqlQuery:
          ret = new SQLQueryType();
          break;
        case NssKeywords.Cassowary:
          ret = new CassowaryType();
          break;
        case NssKeywords.Json:
          ret = new JsonType();
          break;
        default:
          return null;
      }

      baseIndexRef = baseIndex;
      return ret;
    }

    private Function ConstructFunction(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      NSSType returnType = ConstructType(ref baseIndex);
      if (returnType == null) return null;

      Lvalue functionName = ConstructLvalue(ref baseIndex);
      if (functionName == null) return null;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.OpenParen) return null;

      List<FunctionParameter> parameters = new List<FunctionParameter>();

      while (true)
      {
        err = TraverseNextToken(out token, ref baseIndex);
        if (err != 0) return null;

        // Terminate the loop if we're a close paren, or step back if not so we can continue our scan.
        if (token.GetType() == typeof(SeparatorToken) && ((SeparatorToken) token).m_Separator == NssSeparators.CloseParen) break;
        else --baseIndex;

        NSSType paramType = ConstructType(ref baseIndex);
        if (paramType == null) return null;

        Lvalue paramName = ConstructLvalue(ref baseIndex);
        if (paramName == null) return null;

        err = TraverseNextToken(out token, ref baseIndex);
        if (err != 0) return null;

        FunctionParameter param = null;

        // Default value.
        if (token.GetType() == typeof(OperatorToken))
        {
          if (((OperatorToken) token).m_Operator != NssOperators.Equals) return null;

          Value defaultVal = ConstructRvalue(ref baseIndex);
          if (defaultVal == null)
          {
            defaultVal = ConstructLvalue(ref baseIndex);
            if (defaultVal == null) return null;
          }

          param = new FunctionParameterWithDefault {m_Default = defaultVal};
          param.m_Type = paramType;
          param.m_Lvalue = paramName;
          parameters.Add(param);

          err = TraverseNextToken(out token, ref baseIndex);
          if (err != 0) return null;

          // If we're not a comman, just step back so the loop above can handle us.
          if (token.GetType() == typeof(SeparatorToken) && (((SeparatorToken) token).m_Separator != NssSeparators.Comma)) --baseIndex;

          continue;
        }
        // Close paren or comma

        if (token.GetType() == typeof(SeparatorToken))
        {
          SeparatorToken sepParams = (SeparatorToken) token;

          if (sepParams.m_Separator == NssSeparators.CloseParen ||
            sepParams.m_Separator == NssSeparators.Comma)
          {
            param = new FunctionParameter();
            param.m_Type = paramType;
            param.m_Lvalue = paramName;
            parameters.Add(param);

            if (sepParams.m_Separator == NssSeparators.CloseParen) break;
          }
          else
          {
            return null;
          }

          continue;
        }

        return null;
      }

      Function ret = null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;

      if (((SeparatorToken) token).m_Separator == NssSeparators.Semicolon)
      {
        ret = new FunctionDeclaration();
      }
      else if (((SeparatorToken) token).m_Separator == NssSeparators.OpenCurlyBrace)
      {
        --baseIndex; // Step base index back for the block function

        Block block = ConstructBlock_r(ref baseIndex);
        if (block == null) return null;

        ret = new FunctionImplementation {m_Block = block};
      }
      else
      {
        return null;
      }

      ret.Name = functionName;
      ret.ReturnType = returnType;
      ret.Parameters = parameters;

      baseIndexRef = baseIndex;
      return ret;
    }

    private LvaluePreinc ConstructLvaluePreinc(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(OperatorToken)) return null;
      if (((OperatorToken) token).m_Operator != NssOperators.Addition) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(OperatorToken)) return null;
      if (((OperatorToken) token).m_Operator != NssOperators.Addition) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(IdentifierToken)) return null;
      string identifier = ((IdentifierToken) token).Identifier;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.Semicolon) return null;

      LvaluePreinc ret = new LvaluePreinc {m_Identifier = identifier};
      baseIndexRef = baseIndex;
      return ret;
    }

    private LvaluePostinc ConstructLValuePostinc(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(IdentifierToken)) return null;
      string identifier = ((IdentifierToken) token).Identifier;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(OperatorToken)) return null;
      if (((OperatorToken) token).m_Operator != NssOperators.Addition) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(OperatorToken)) return null;
      if (((OperatorToken) token).m_Operator != NssOperators.Addition) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.Semicolon) return null;

      LvaluePostinc ret = new LvaluePostinc {m_Identifier = identifier};
      baseIndexRef = baseIndex;
      return ret;
    }

    private LvaluePredec ConstructLvaluePredec(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(OperatorToken)) return null;
      if (((OperatorToken) token).m_Operator != NssOperators.Subtraction) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(OperatorToken)) return null;
      if (((OperatorToken) token).m_Operator != NssOperators.Subtraction) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(IdentifierToken)) return null;
      string identifier = ((IdentifierToken) token).Identifier;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.Semicolon) return null;

      LvaluePredec ret = new LvaluePredec {m_Identifier = identifier};
      baseIndexRef = baseIndex;
      return ret;
    }

    private LvaluePostdec ConstructLValuePostdec(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(IdentifierToken)) return null;
      string identifier = ((IdentifierToken) token).Identifier;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(OperatorToken)) return null;
      if (((OperatorToken) token).m_Operator != NssOperators.Subtraction) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(OperatorToken)) return null;
      if (((OperatorToken) token).m_Operator != NssOperators.Subtraction) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.Semicolon) return null;

      LvaluePostdec ret = new LvaluePostdec {m_Identifier = identifier};
      baseIndexRef = baseIndex;
      return ret;
    }

    private Lvalue ConstructLvalue(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(IdentifierToken)) return null;
      string identifier = ((IdentifierToken) token).Identifier;

      Lvalue ret = new Lvalue {Identifier = identifier};
      baseIndexRef = baseIndex;
      return ret;
    }

    private VectorLiteral ConstructVectorLiteral(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.OpenSquareBracket) return null;

      VectorLiteral ret = new VectorLiteral();

      ret.m_X = ConstructRvalue(ref baseIndex) as FloatLiteral;
      if (ret.m_X == null) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.Comma) return null;

      ret.m_Y = ConstructRvalue(ref baseIndex) as FloatLiteral;
      if (ret.m_Y == null) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.Comma) return null;

      ret.m_Z = ConstructRvalue(ref baseIndex) as FloatLiteral;
      if (ret.m_Z == null) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.CloseSquareBracket) return null;

      baseIndexRef = baseIndex;
      return ret;
    }

    private Rvalue ConstructRvalue(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      {
        // VECTOR LITERAL
        VectorLiteral vector = ConstructVectorLiteral(ref baseIndex);
        if (vector != null)
        {
          baseIndexRef = baseIndex;
          return vector;
        }
      }

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0) return null;

      bool negative = false;

      if (token.GetType() == typeof(OperatorToken))
      {
        if (((OperatorToken) token).m_Operator != NssOperators.Subtraction) return null;
        err = TraverseNextToken(out token, ref baseIndex);
        if (err != 0) return null;
        negative = true;
      }

      if (token.GetType() != typeof(LiteralToken)) return null;

      Rvalue ret = null;

      LiteralToken lit = (LiteralToken) token;
      string literal = (negative ? "-" : "") + lit.Literal;

      try
      {
        switch (lit.LiteralType)
        {
          case NssLiteralType.Int:
          {
            int value = lit.IsHex ? Convert.ToInt32(literal, 16) : int.Parse(literal);
            ret = new IntLiteral {Value = value};
            break;
          }

          case NssLiteralType.Float:
          {
            literal = literal.TrimEnd('f');
            if (!float.TryParse(literal, out float value)) return null;
            ret = new FloatLiteral {Value = value};
            break;
          }

          case NssLiteralType.String:
          {
            ret = new StringLiteral {Value = literal};
            break;
          }

          default: return null;
        }
      }
      catch (InvalidCastException e)
      {
        return null;
      }

      baseIndexRef = baseIndex;
      return ret;
    }

    private LvalueDecl ConstructLvalueDecl(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      // Constness
      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0) return null;
      bool constness = token.GetType() == typeof(KeywordToken) && ((KeywordToken) token).m_Keyword == NssKeywords.Const;
      if (!constness) --baseIndex;

      // Typename
      NSSType type = ConstructType(ref baseIndex);
      if (type == null) return null;

      // Identifier
      Lvalue lvalue = ConstructLvalue(ref baseIndex);
      if (lvalue == null) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0) return null;

      LvalueDecl ret = null;

      // Declaration
      if (token.GetType() == typeof(SeparatorToken))
      {
        if (constness) return null;

        SeparatorToken sep = (SeparatorToken) token;

        // If it's a comma, we're working on multiple.
        if (sep.m_Separator == NssSeparators.Comma)
        {
          LvalueDeclMultiple decl = new LvalueDeclMultiple();
          decl.m_Type = type;

          while (true)
          {
            lvalue = ConstructLvalue(ref baseIndex);
            if (lvalue == null) return null;

            decl.m_Lvalues.Add(lvalue);

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;

            sep = (SeparatorToken) token;
            if (sep.m_Separator == NssSeparators.Semicolon) break;
            else if (sep.m_Separator != NssSeparators.Comma) return null;
          }

          ret = decl;
        }
        else
        {
          LvalueDeclSingle decl = new LvalueDeclSingle();
          decl.m_Type = type;
          decl.m_Lvalue = lvalue;
          ret = decl;
        }
      }
      // Declaration with assignment
      else if (token.GetType() == typeof(OperatorToken))
      {
        OperatorToken op = (OperatorToken) token;
        if (op.m_Operator != NssOperators.Equals) return null;

        ArithmeticExpression expr = ConstructArithmeticExpression(ref baseIndex);
        if (expr == null) return null;

        LvalueDeclSingleWithAssignment decl = constness ? new ConstLvalueDeclSingleWithAssignment() : new LvalueDeclSingleWithAssignment();
        decl.m_Type = type;
        decl.m_Lvalue = lvalue;
        decl.m_Expression = expr;
        ret = decl;
      }

      baseIndexRef = baseIndex;
      return ret;
    }

    private StructDeclaration ConstructStructDeclaration(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;
      if (((KeywordToken) token).m_Keyword != NssKeywords.Struct) return null;

      Lvalue structName = ConstructLvalue(ref baseIndex);
      if (structName == null) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.OpenCurlyBrace) return null;

      StructDeclaration ret = new StructDeclaration
      {
        m_Name = structName,
        m_Members = new List<LvalueDecl>()
      };

      while (true)
      {
        LvalueDecl decl = ConstructLvalueDecl(ref baseIndex);
        if (decl == null)
        {
          err = TraverseNextToken(out token, ref baseIndex);
          if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
          if (((SeparatorToken) token).m_Separator != NssSeparators.CloseCurlyBrace) return null;
          break;
        }

        ret.m_Members.Add(decl);
      }

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.Semicolon) return null;

      baseIndexRef = baseIndex;
      return ret;
    }

    private RedundantSemiColon ConstructRedundantSemiColon(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;
      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.Semicolon) return null;
      baseIndexRef = baseIndex;
      return new RedundantSemiColon();
    }

    public AssignmentOpChain ConstructAssignmentOpChain(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      NssOperators?[] ops = new NssOperators?[] {null, null};

      for (int i = 0; i < ops.Length; ++i)
      {
        int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
        if (err != 0) return null;

        OperatorToken op = token as OperatorToken;
        if (op == null)
        {
          --baseIndex; // Step back for caller.
          break;
        }

        ops[i] = op.m_Operator;
      }

      if (!ops[0].HasValue) return null;

      AssignmentOpChain ret = null;

      if (ops[0].Value == NssOperators.Equals)
      {
        ret = new Equals();
      }
      else if (ops[0].Value == NssOperators.Addition)
      {
        if (ops[1].HasValue && ops[1].Value == NssOperators.Equals)
        {
          ret = new PlusEquals();
        }
      }
      else if (ops[0].Value == NssOperators.Subtraction)
      {
        if (ops[1].HasValue && ops[1].Value == NssOperators.Equals)
        {
          ret = new PlusEquals();
        }
      }
      else if (ops[0].Value == NssOperators.Multiplication)
      {
        if (ops[1].HasValue && ops[1].Value == NssOperators.Equals)
        {
          ret = new MultiplyEquals();
        }
      }
      else if (ops[0].Value == NssOperators.Division)
      {
        if (ops[1].HasValue && ops[1].Value == NssOperators.Equals)
        {
          ret = new DivideEquals();
        }
      }

      if (ret == null) return null;

      baseIndexRef = baseIndex;
      return ret;
    }

    public ArithmeticExpression ConstructArithmeticExpression(ref int baseIndexRef, NssSeparators boundingSep = NssSeparators.Semicolon)
    {
      int baseIndex = baseIndexRef;
      string expression = "";

      while (true)
      {
        int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
        if (err != 0) return null;
        if (token.GetType() == typeof(SeparatorToken) && ((SeparatorToken) token).m_Separator == boundingSep) break;

        expression += token.ToString();

        if (token.GetType() == typeof(KeywordToken) || token.GetType() == typeof(SeparatorToken))
        {
          expression += " ";
        }
      }

      ArithmeticExpression ret = new ArithmeticExpression {m_Expression = expression.TrimEnd()};
      baseIndexRef = baseIndex;
      return ret;
    }

    public LogicalExpression ConstructLogicalExpression(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.OpenParen) return null;

      string expression = "";
      int parenDepth = 1;

      while (true)
      {
        err = TraverseNextToken(out token, ref baseIndex);
        if (err != 0) return null;

        if (token is SeparatorToken sep)
        {
          if (sep.m_Separator == NssSeparators.OpenParen)
          {
            ++parenDepth;
          }
          else if (sep.m_Separator == NssSeparators.CloseParen)
          {
            --parenDepth;
          }
          else if (sep.m_Separator != NssSeparators.Comma)
          {
            return null;
          }
        }

        if (parenDepth == 0)
        {
          break;
        }

        expression += token.ToString();

        if (token.GetType() == typeof(KeywordToken) || token.GetType() == typeof(SeparatorToken))
        {
          expression += " ";
        }
      }

      if (parenDepth != 0) return null;

      LogicalExpression ret = new LogicalExpression {m_Expression = expression.TrimEnd()};
      baseIndexRef = baseIndex;
      return ret;
    }

    private FunctionCall ConstructFunctionCall(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      Lvalue functionName = ConstructLvalue(ref baseIndex);
      if (functionName == null) return null;

      LogicalExpression args = ConstructLogicalExpression(ref baseIndex);
      if (args == null) return null;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.Semicolon) return null;

      MatchCollection matches = FunctionCallRegex.Matches($"{functionName.Identifier}({args.m_Expression})");
      FunctionCall ret = new FunctionCall {m_Name = functionName, m_Arguments = matches[0].Groups["param"].Captures.Select(capture =>
      {
        // String literal in the argument, don't mess with it.
        if (capture.Value.Contains("\""))
        {
          return capture.Value;
        }

        return capture.Value.Replace(" ", "");
      }).ToArray()};
      baseIndexRef = baseIndex;
      return ret;
    }

    private LvalueAssignment ConstructLvalueAssignment(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      Lvalue lvalue = ConstructLvalue(ref baseIndex);
      if (lvalue == null) return null;

      AssignmentOpChain opChain = ConstructAssignmentOpChain(ref baseIndex);
      if (opChain == null) return null;

      ArithmeticExpression expr = ConstructArithmeticExpression(ref baseIndex);
      if (expr == null) return null;

      LvalueAssignment ret = new LvalueAssignment {m_Lvalue = lvalue, m_OpChain = opChain, m_Expression = expr};
      baseIndexRef = baseIndex;
      return ret;
    }

    private WhileLoop ConstructWhileLoop(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;
      if (((KeywordToken) token).m_Keyword != NssKeywords.While) return null;

      LogicalExpression cond = ConstructLogicalExpression(ref baseIndex);
      if (cond == null) return null;

      NSSNode action = ConstructBlock_r(ref baseIndex);
      if (action == null)
      {
        action = ConstructValidInBlock(ref baseIndex);
        if (action == null) return null;
      }

      WhileLoop ret = new WhileLoop {m_Expression = cond, m_Action = action};
      baseIndexRef = baseIndex;
      return ret;
    }

    private ForLoop ConstructForLoop(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;
      if (((KeywordToken) token).m_Keyword != NssKeywords.For) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.OpenParen) return null;

      ArithmeticExpression pre = ConstructArithmeticExpression(ref baseIndex);
      if (pre == null) return null;

      ArithmeticExpression cond = ConstructArithmeticExpression(ref baseIndex);
      if (cond == null) return null;

      ArithmeticExpression post = ConstructArithmeticExpression(ref baseIndex, NssSeparators.CloseParen);
      if (post == null) return null;

      NSSNode action = ConstructBlock_r(ref baseIndex);
      if (action == null)
      {
        action = ConstructValidInBlock(ref baseIndex);
        if (action == null) return null;
      }

      ForLoop ret = new ForLoop
      {
        m_Pre = pre,
        m_Condition = cond,
        m_Post = post,
        m_Action = action
      };

      baseIndexRef = baseIndex;
      return ret;
    }

    private DoWhileLoop ConstructDoWhileLoop(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;
      if (((KeywordToken) token).m_Keyword != NssKeywords.Do) return null;

      NSSNode action = ConstructBlock_r(ref baseIndex);
      if (action == null)
      {
        action = ConstructValidInBlock(ref baseIndex);
        if (action == null) return null;
      }

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;
      if (((KeywordToken) token).m_Keyword != NssKeywords.While) return null;

      LogicalExpression cond = ConstructLogicalExpression(ref baseIndex);
      if (cond == null) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.Semicolon) return null;

      DoWhileLoop ret = new DoWhileLoop {m_Expression = cond, m_Action = action};
      baseIndexRef = baseIndex;
      return ret;
    }

    private IfStatement ConstructIfStatement(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;
      if (((KeywordToken) token).m_Keyword != NssKeywords.If) return null;

      LogicalExpression expression = ConstructLogicalExpression(ref baseIndex);
      if (expression == null) return null;

      NSSNode action = ConstructBlock_r(ref baseIndex);
      if (action == null)
      {
        action = ConstructValidInBlock(ref baseIndex);
        if (action == null) return null;
      }

      IfStatement ret = new IfStatement {m_Expression = expression, m_Action = action};
      baseIndexRef = baseIndex;
      return ret;
    }

    private ElseStatement ConstructElseStatement(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;
      if (((KeywordToken) token).m_Keyword != NssKeywords.Else) return null;

      NSSNode action = ConstructBlock_r(ref baseIndex);
      if (action == null)
      {
        action = ConstructValidInBlock(ref baseIndex);
        if (action == null) return null;
      }

      ElseStatement ret = new ElseStatement {m_Action = action};
      baseIndexRef = baseIndex;
      return ret;
    }

    private ReturnStatement ConstructReturnStatement(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;
      if (((KeywordToken) token).m_Keyword != NssKeywords.Return) return null;

      ArithmeticExpression expr = ConstructArithmeticExpression(ref baseIndex);
      if (expr == null) return null;

      ReturnStatement ret = new ReturnStatement {m_Expression = expr};
      baseIndexRef = baseIndex;
      return ret;
    }

    private SwitchStatement ConstructSwitchStatement(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;
      if (((KeywordToken) token).m_Keyword != NssKeywords.Switch) return null;

      LogicalExpression expr = ConstructLogicalExpression(ref baseIndex);
      if (expr == null) return null;

      Block block = ConstructBlock_r(ref baseIndex);
      if (block == null) return null;

      SwitchStatement ret = new SwitchStatement {m_Expression = expr, m_Block = block};
      baseIndexRef = baseIndex;
      return ret;
    }

    private CaseLabel ConstructCaseLabel(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;

      Value label = null;
      KeywordToken keyword = (KeywordToken) token;

      if (keyword.m_Keyword == NssKeywords.Case)
      {
        label = ConstructRvalue(ref baseIndex);
        if (label == null)
        {
          label = ConstructLvalue(ref baseIndex);
          if (label == null) return null;
        }
      }
      else if (keyword.m_Keyword != NssKeywords.Default)
      {
        return null;
      }

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(OperatorToken)) return null;
      if (((OperatorToken) token).m_Operator != NssOperators.TernaryColon) return null;

      CaseLabel ret = new CaseLabel {m_Label = label};
      baseIndexRef = baseIndex;
      return ret;
    }

    private BreakStatement ConstructBreakStatement(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(KeywordToken)) return null;
      if (((KeywordToken) token).m_Keyword != NssKeywords.Break) return null;

      err = TraverseNextToken(out token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.Semicolon) return null;

      baseIndexRef = baseIndex;
      return new BreakStatement();
    }

    private Block ConstructBlock_r(ref int baseIndexRef)
    {
      int baseIndex = baseIndexRef;

      int err = TraverseNextToken(out ILanguageToken token, ref baseIndex);
      if (err != 0 || token.GetType() != typeof(SeparatorToken)) return null;
      if (((SeparatorToken) token).m_Separator != NssSeparators.OpenCurlyBrace) return null;

      Block ret = new Block();

      while (true)
      {
        Block block = ConstructBlock_r(ref baseIndex);
        if (block != null)
        {
          ret.m_Nodes.Add(block);
          continue;
        }

        NSSNode validInBlock = ConstructValidInBlock(ref baseIndex);
        if (validInBlock != null)
        {
          ret.m_Nodes.Add(validInBlock);
          continue;
        }

        err = TraverseNextToken(out token, ref baseIndex);
        if (err != 0) return null;
        if (token.GetType() == typeof(SeparatorToken) && ((SeparatorToken) token).m_Separator == NssSeparators.CloseCurlyBrace) break;

        ReportTokenError(token, "Unrecognised token in block-level.");

        return null;
      }

      baseIndexRef = baseIndex;
      return ret;
    }

    private NSSNode ConstructValidInBlock(ref int baseIndexRef)
    {
      {
        // FUNCTION CALL
        NSSNode node = ConstructFunctionCall(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // VARIABLE DECLARATIONS
        NSSNode node = ConstructLvalueDecl(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // VARIABLE ASSIGNMENTS
        NSSNode node = ConstructLvalueAssignment(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // LVALUE PREINC
        NSSNode node = ConstructLvaluePreinc(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // LVALUE POSTINC
        NSSNode node = ConstructLValuePostinc(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // LVALUE PREDEC
        NSSNode node = ConstructLvaluePredec(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // LVALUE POSTDEC
        NSSNode node = ConstructLValuePostdec(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // WHILE LOOP
        NSSNode node = ConstructWhileLoop(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // FOR LOOP
        NSSNode node = ConstructForLoop(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // DO WHILE LOOP
        NSSNode node = ConstructDoWhileLoop(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // IF STATEMENT
        NSSNode node = ConstructIfStatement(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // ELSE STATEMENT
        NSSNode node = ConstructElseStatement(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // RETURN STATEMENT
        NSSNode node = ConstructReturnStatement(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // SWITCH STATEMENT
        NSSNode node = ConstructSwitchStatement(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // CASE LABEL
        NSSNode node = ConstructCaseLabel(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // BREAK STATEMENT
        NSSNode node = ConstructBreakStatement(ref baseIndexRef);
        if (node != null) return node;
      }

      {
        // REDUNDANT SEMI COLON
        NSSNode node = ConstructRedundantSemiColon(ref baseIndexRef);
        if (node != null) return node;
      }

      return null;
    }

    private void ReportTokenError(ILanguageToken token, string error)
    {
      Errors.Add(error);
      Errors.Add(string.Format("On Token type {0}", token.GetType().Name));

      if (token.UserData != null)
      {
        LexerDebugInfo debugInfo = token.UserData;
        Errors.Add(string.Format("At line {0}:{1} to line {2}:{3}.",
          debugInfo.LineStart, debugInfo.ColumnStart,
          debugInfo.LineEnd, debugInfo.ColumnEnd));
        Errors.Add(CompilationUnit.m_DebugInfo.m_SourceData[debugInfo.LineStart]);
        Errors.Add(string.Format(
          "{0," + debugInfo.ColumnStart + "}" +
          "{1," + (debugInfo.ColumnEnd - debugInfo.ColumnStart) + "}",
          "^", "^"));
      }
    }

    private int TraverseNextToken(out ILanguageToken token, ref int baseIndexRef,
      bool skipComments = true, bool skipWhitespace = true)
    {
      ILanguageToken ret = null;

      int baseIndex = baseIndexRef;

      while (ret == null)
      {
        if (baseIndex >= Tokens.Count)
        {
          token = null;
          return 1;
        }

        ret = Tokens[baseIndex];

        bool skip = false;

        if (skipWhitespace)
        {
          if (ret is SeparatorToken sep && (
            sep.m_Separator == NssSeparators.Tab ||
            sep.m_Separator == NssSeparators.Space ||
            sep.m_Separator == NssSeparators.NewLine))
          {
            skip = true;
          }
        }

        if (skipComments && ret.GetType() == typeof(CommentToken))
        {
          skip = true;
        }

        if (skip)
        {
          ret = null;
          ++baseIndex;
          continue;
        }
      }

      baseIndexRef = ++baseIndex;
      token = ret;
      return 0;
    }
  }
}
