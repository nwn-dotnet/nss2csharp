using System.Collections.Generic;
using System.Linq;
using System.Text;
using NWScript.Parser;

namespace NWScript.Output
{
  public class Output_CSharp_NWNX
  {
    StringBuilder stringBuilder = new StringBuilder();
    private string pluginNameVar = null;

    public int GetFromCU(CompilationUnit cu, string className, out string data)
    {
      stringBuilder.Clear();
      pluginNameVar = null;

      stringBuilder.AppendLine("namespace NWN.Core.NWNX");
      stringBuilder.AppendLine("{");

      int attributePos = stringBuilder.Length;
      stringBuilder.AppendLine();
      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}public class {className}");
      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}{{");

      HashSet<FunctionImplementation> implementedMethods = new HashSet<FunctionImplementation>();
      for (int index = 0; index < cu.m_Nodes.Count; index++)
      {
        NSSNode node = cu.m_Nodes[index];
        if (node is LineComment lineComment)
        {
          if (lineComment.Comment.Contains("@param"))
          {
            string paramName = lineComment.Comment.Split(' ')[2];
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}//{lineComment.Comment.Replace($"@param {paramName} ", $"<param name=\"{paramName}\">")}</param>");
          }
          else if (lineComment.Comment.Contains("@return"))
          {
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}//{lineComment.Comment.Replace("@return ", $"<returns>")}</returns>");
          }
          else
          {
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}//" + lineComment.Comment.Replace("@brief ", ""));
          }
        }

        if (node is BlockComment blockComment)
        {
          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}/*");
          foreach (string line in blockComment.CommentLines)
          {
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}" + line);
          }

          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}*/");
        }

        if (node is LvalueDeclSingleWithAssignment lvalueDecl)
        {
          // First entry is always the plugin name.
          if (pluginNameVar == null)
          {
            if (lvalueDecl.m_Type.GetType() != typeof(StringType))
            {
              data = null;
              return -1;
            }

            pluginNameVar = lvalueDecl.m_Lvalue.Identifier;
          }

          string type = lvalueDecl.m_Type.Declaration;
          string name = lvalueDecl.m_Lvalue.Identifier;
          string value = lvalueDecl.m_Expression.m_Expression;
          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}public const {type} {name} = {value}{(lvalueDecl.m_Type.GetType() == typeof(FloatType) && !value.EndsWith("f") ? "f" : "")};");
          if (cu.m_Nodes.Count > index + 2 && !(cu.m_Nodes[index + 1] is LvalueDeclSingleWithAssignment))
          {
            stringBuilder.AppendLine();
          }
        }

        if (pluginNameVar == null)
        {
          continue;
        }

        if (node is FunctionDeclaration funcDecl)
        {
          FunctionImplementation funcImpl = GetFunction(cu, funcDecl);
          if (implementedMethods.Contains(funcImpl))
          {
            continue;
          }

          BuildMethod(funcImpl);
          implementedMethods.Add(funcImpl);
        }

        if (node is FunctionImplementation func)
        {
          if (implementedMethods.Contains(func))
          {
            continue;
          }
          
          BuildMethod(func);
          implementedMethods.Add(func);
        }
      }

      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}}}");

      // Structures
      foreach (StructDeclaration structDeclaration in cu.m_Nodes.OfType<StructDeclaration>())
      {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}public struct {RemovePrefixes(structDeclaration.m_Name.Identifier)}");
        stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}{{");

        foreach (LvalueDeclSingle dec in structDeclaration.m_Members.OfType<LvalueDeclSingle>())
        {
          string type = dec.m_Type.Declaration;
          string name = dec.m_Lvalue.Identifier;
          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}public {type} {name};");
        }

        stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}}}");
      }

      stringBuilder.Append("}");

      if (pluginNameVar == null)
      {
        data = null;
        return -1;
      }

      stringBuilder.Insert(attributePos, $"{Output_CSharp.GetIndent(1)}[NWNXPlugin({pluginNameVar})]");
      data = stringBuilder.ToString();
      return 0;
    }

    private void BuildMethod(FunctionImplementation implementation)
    {
      string name = implementation.Name.Identifier.Replace(pluginNameVar + "_", "");
      string retType = RemovePrefixes(implementation.ReturnType.Declaration);

      List<string> funcParams = new List<string>();
      foreach (FunctionParameter param in implementation.Parameters)
      {
        string paramType = RemovePrefixes(param.m_Type.Declaration);
        string paramName = Output_CSharp.GetSafeVariableName(param.m_Lvalue.Identifier);

        string paramStr = paramType + " " + paramName;

        if (param is FunctionParameterWithDefault def)
        {
          string defaultAsStr = Output_CSharp.GetValueAsString(def.m_Default, true);
          paramStr += " = " + defaultAsStr;
        }

        funcParams.Add(paramStr);
      }

      string parameters = funcParams.Count == 0 ? "" : funcParams.Aggregate((a, b) => a + ", " + b);

      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}public static {retType} {name}({parameters})");
      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}{{");

      BuildImplementation(implementation);

      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}}}");
      stringBuilder.AppendLine();
    }

    private string RemovePrefixes(string fullName)
    {
      return fullName.Replace($"{pluginNameVar}_", "").Replace("NWNX_", "");
    }

    private void BuildImplementation(FunctionImplementation implementation)
    {
      foreach (NSSNode node in implementation.m_Block.m_Nodes)
      {
        ProcessImplementationNode(implementation, node, 3);
      }
    }

    private void ProcessImplementationNode(FunctionImplementation implementation, NSSNode node, int depth)
    {
      string expression;

      switch (node)
      {
        case LvalueDeclSingleWithAssignment declAssignment:
          if (declAssignment.m_Lvalue.Identifier == "sFunc")
          {
            string methodName = declAssignment.m_Expression.m_Expression;
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}VM.NWNX.SetFunction({pluginNameVar}, {methodName});");
            break;
          }

          expression = VMTranslations.TryTranslate(pluginNameVar, declAssignment.m_Expression.m_Expression);
          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}{declAssignment.m_Type.Declaration} {declAssignment.m_Lvalue.Identifier} = {expression};");
          break;
        case LvalueAssignment assignment:
          expression = VMTranslations.TryTranslate(pluginNameVar, assignment.m_Expression.m_Expression);

          if (VMTranslations.IsNssConstant(expression))
          {
            expression = "NWScript." + expression;
          }

          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}{assignment.m_Lvalue.Identifier} = {expression};");
          break;
        case LvalueDeclSingle declaration:
          if (declaration.m_Type.GetType() == typeof(StringType))
          {
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}{RemovePrefixes(declaration.m_Type.Declaration)} {declaration.m_Lvalue.Identifier} = \"\";");
          }
          else if (declaration.m_Type.GetType() == typeof(StructType))
          {
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}{RemovePrefixes(declaration.m_Type.Declaration)} {declaration.m_Lvalue.Identifier} = default;");
          }
          else
          {
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}{RemovePrefixes(declaration.m_Type.Declaration)} {declaration.m_Lvalue.Identifier};");
          }
          break;
        case FunctionCall functionCall:
          expression = VMTranslations.TranslateCall(pluginNameVar, functionCall.m_Name.Identifier, functionCall.m_Arguments);
          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}{expression};");
          break;
        case ReturnStatement returnStatement:
          if (implementation.ReturnType is StructType structType)
          {
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}return {returnStatement.m_Expression.m_Expression};");
            break;
          }

          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}return {implementation.ReturnType.NWNXPopFormat};");
          break;
        case IfStatement ifStatement:
          expression = VMTranslations.TryTranslate(pluginNameVar, ifStatement.m_Expression.m_Expression);

          if (expression != ifStatement.m_Expression.m_Expression)
          {
            expression = $"{expression} == NWScript.TRUE";
          }

          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}if ({expression})");
          ProcessImplementationNode(implementation, ifStatement.m_Action, depth);
          break;
        case ElseStatement elseStatement:
          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}else");
          ProcessImplementationNode(implementation, elseStatement.m_Action, depth);
          break;
        case ForLoop forLoop:
          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}for ({forLoop.m_Pre.m_Expression}; {forLoop.m_Condition.m_Expression}; {forLoop.m_Post.m_Expression})");
          ProcessImplementationNode(implementation, forLoop.m_Action, depth);
          break;
        case Block block:
          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}{{");
          foreach (NSSNode childNode in block.m_Nodes)
          {
            ProcessImplementationNode(implementation, childNode, depth + 1);
          }

          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(depth)}}}");
          break;
        default:
          break;
      }
    }

    private FunctionImplementation GetFunction(CompilationUnit cu, FunctionDeclaration decl)
    {
      foreach (NSSNode node in cu.m_Nodes)
      {
        if (node is FunctionImplementation impl && impl.Name.Identifier == decl.Name.Identifier)
        {
          if (impl.Parameters.Select(parameter => parameter.m_Type.GetType()).SequenceEqual(decl.Parameters.Select(parameter => parameter.m_Type.GetType())))
          {
            return impl;
          }
        }
      }

      return null;
    }
  }
}