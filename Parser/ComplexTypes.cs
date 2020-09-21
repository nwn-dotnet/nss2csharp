using System.Collections.Generic;

namespace NWScript.Parser
{
  public struct CompilationUnitMetadata
  {
    public string m_Name;
  }

  public struct CompilationUnitDebugInfo
  {
    public string[] m_SourceData;
  }

  public class CompilationUnit : NSSNode
  {
    public CompilationUnitMetadata m_Metadata;
    public CompilationUnitDebugInfo m_DebugInfo;
    public List<NSSNode> m_Nodes = new List<NSSNode>();
  }

  public class LvalueDecl : NSSNode
  {
    public NSSType m_Type;
  }

  public class LvalueDeclSingle : LvalueDecl
  {
    public Lvalue m_Lvalue;
  }

  public class LvalueDeclSingleWithAssignment : LvalueDeclSingle
  {
    public ArithmeticExpression m_Expression;
  }

  public class ConstLvalueDeclSingleWithAssignment : LvalueDeclSingleWithAssignment {}

  public class LvalueDeclMultiple : LvalueDecl
  {
    public List<Lvalue> m_Lvalues = new List<Lvalue>();
  }

  public class FunctionParameter : LvalueDeclSingle {}

  public class FunctionParameterWithDefault : FunctionParameter
  {
    public Value m_Default;
  }

  public class Block : NSSNode
  {
    public List<NSSNode> m_Nodes = new List<NSSNode>();
  }

  public class FunctionDeclaration : Function {}

  public class FunctionImplementation : Function
  {
    public Block m_Block;
  }

  public class FunctionCall : NSSNode
  {
    public Lvalue m_Name;
    public string[] m_Arguments;
  }

  public class Expression : NSSNode
  {
    public string m_Expression; // Just store it as a string - for this tool, we don't need to semantically understand it.
  }

  public class ArithmeticExpression : Expression {}

  public class LogicalExpression : Expression {}

  public class LvalueAssignment : NSSNode
  {
    public Lvalue m_Lvalue;
    public AssignmentOpChain m_OpChain;
    public Expression m_Expression;
  }

  public class LvaluePreinc : NSSNode
  {
    public string m_Identifier;
  }

  public class LvaluePostinc : NSSNode
  {
    public string m_Identifier;
  }

  public class LvaluePredec : NSSNode
  {
    public string m_Identifier;
  }

  public class LvaluePostdec : NSSNode
  {
    public string m_Identifier;
  }

  public class WhileLoop : NSSNode
  {
    public Expression m_Expression;
    public NSSNode m_Action;
  }

  public class ForLoop : NSSNode
  {
    public Expression m_Pre;
    public Expression m_Condition;
    public Expression m_Post;
    public NSSNode m_Action;
  }

  public class DoWhileLoop : NSSNode
  {
    public Expression m_Expression;
    public NSSNode m_Action;
  }

  public class IfStatement : NSSNode
  {
    public Expression m_Expression;
    public NSSNode m_Action;
  }

  public class ElseStatement : NSSNode
  {
    public NSSNode m_Action;
  }

  public class ReturnStatement : NSSNode
  {
    public ArithmeticExpression m_Expression;
  }

  public class StructDeclaration : NSSNode
  {
    public Lvalue m_Name;
    public List<LvalueDecl> m_Members;
  }

  public class SwitchStatement : NSSNode
  {
    public LogicalExpression m_Expression;
    public Block m_Block;
  }

  public class CaseLabel : NSSNode
  {
    // m_Label can be null. If it is, this is the default case.
    public Value m_Label;
  }

  public class BreakStatement : NSSNode {}

  public class VectorLiteral : Rvalue
  {
    public FloatLiteral m_X;
    public FloatLiteral m_Y;
    public FloatLiteral m_Z;
  }
}