using System.Collections.Generic;

namespace NWScript.Parser
{
  public class NSSNode {}

  public abstract class Preprocessor : NSSNode {}

  public class UnknownPreprocessor : Preprocessor
  {
    public string Value;
  }

  public abstract class Comment : NSSNode {}

  public class LineComment : Comment
  {
    public string Comment;
  }

  public class BlockComment : Comment
  {
    public List<string> CommentLines;
  }

  public abstract class NSSType : NSSNode
  {
    public abstract string Declaration { get; }
    public virtual string NativePushFormat => "VM.StackPush({0})";
    public virtual string NWNXPushFormat => "VM.NWNX.StackPush({0})";
    public abstract string NativePopFormat { get; }
    public abstract string NWNXPopFormat { get; }
  }

  public class VoidType : NSSType
  {
    public override string Declaration => "void";
    public override string NativePushFormat => null;
    public override string NWNXPushFormat => null;
    public override string NativePopFormat => null;
    public override string NWNXPopFormat => null;
  }

  public class IntType : NSSType
  {
    public override string Declaration => "int";
    public override string NativePopFormat => "VM.StackPopInt()";
    public override string NWNXPopFormat => "VM.NWNX.StackPopInt()";
  }

  public class FloatType : NSSType
  {
    public override string Declaration => "float";
    public override string NativePopFormat => "VM.StackPopFloat()";
    public override string NWNXPopFormat => "VM.NWNX.StackPopFloat()";
  }

  public class StringType : NSSType
  {
    public override string Declaration => "string";
    public override string NativePopFormat => "VM.StackPopString()";
    public override string NWNXPopFormat => "VM.NWNX.StackPopString()";
  }

  public class StructType : NSSType
  {
    public string TypeName;

    public override string Declaration => TypeName;
    public override string NativePushFormat => null;
    public override string NWNXPushFormat => null;
    public override string NativePopFormat => null;
    public override string NWNXPopFormat => null;
  }

  public class ObjectType : NSSType
  {
    public override string Declaration => "uint";
    public override string NativePopFormat => "VM.StackPopObject()";
    public override string NWNXPopFormat => "VM.NWNX.StackPopObject()";
  }

  public class VectorType : NSSType
  {
    public override string Declaration => "System.Numerics.Vector3";
    public override string NativePopFormat => "VM.StackPopVector()";
    public override string NWNXPopFormat => "VM.NWNX.StackPopVector()";
  }

  public class EffectType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "VM.StackPush({0}, ENGINE_STRUCTURE_EFFECT)";
    public override string NWNXPushFormat => "VM.NWNX.StackPush({0}, ENGINE_STRUCTURE_EFFECT)";

    public override string NativePopFormat => "VM.StackPopStruct(ENGINE_STRUCTURE_EFFECT)";
    public override string NWNXPopFormat => "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_EFFECT)";
  }

  public class EventType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "VM.StackPush({0}, ENGINE_STRUCTURE_EVENT)";
    public override string NWNXPushFormat => "VM.NWNX.StackPush({0}, ENGINE_STRUCTURE_EVENT)";

    public override string NativePopFormat => "VM.StackPopStruct(ENGINE_STRUCTURE_EVENT)";
    public override string NWNXPopFormat => "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_EVENT)";
  }

  public class LocationType : NSSType
  {
    public override string Declaration => "System.IntPtr";
    public override string NativePushFormat => "VM.StackPush({0}, ENGINE_STRUCTURE_LOCATION)";
    public override string NWNXPushFormat => "VM.NWNX.StackPush({0}, ENGINE_STRUCTURE_LOCATION)";

    public override string NativePopFormat => "VM.StackPopStruct(ENGINE_STRUCTURE_LOCATION)";
    public override string NWNXPopFormat => "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_LOCATION)";
  }

  public class TalentType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "VM.StackPush({0}, ENGINE_STRUCTURE_TALENT)";
    public override string NWNXPushFormat => "VM.NWNX.StackPush({0}, ENGINE_STRUCTURE_TALENT)";

    public override string NativePopFormat => "VM.StackPopStruct(ENGINE_STRUCTURE_TALENT)";
    public override string NWNXPopFormat => "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_TALENT)";
  }

  public class ItemPropertyType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "VM.StackPush({0}, ENGINE_STRUCTURE_ITEMPROPERTY)";
    public override string NWNXPushFormat => "VM.NWNX.StackPush({0}, ENGINE_STRUCTURE_ITEMPROPERTY)";

    public override string NativePopFormat => "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_ITEMPROPERTY)";
    public override string NWNXPopFormat => "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_ITEMPROPERTY)";
  }

  public class SQLQueryType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "VM.StackPush({0}, ENGINE_STRUCTURE_SQLQUERY)";
    public override string NWNXPushFormat => "VM.NWNX.StackPush({0}, ENGINE_STRUCTURE_SQLQUERY)";

    public override string NativePopFormat => "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_SQLQUERY)";
    public override string NWNXPopFormat => "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_SQLQUERY)";
  }

  public class CassowaryType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "VM.StackPush({0}, ENGINE_STRUCTURE_CASSOWARY)";
    public override string NWNXPushFormat => "VM.NWNX.StackPush({0}, ENGINE_STRUCTURE_CASSOWARY)";

    public override string NativePopFormat => "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_CASSOWARY)";
    public override string NWNXPopFormat => "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_CASSOWARY)";
  }

  public class ActionType : NSSType
  {
    public override string Declaration => "System.Action";
    public override string NativePushFormat => null;
    public override string NWNXPushFormat => null;
    public override string NativePopFormat => null;
    public override string NWNXPopFormat => null;
  }

  public abstract class Value : NSSNode {}

  public class Lvalue : Value
  {
    public string Identifier;
  }

  public abstract class Rvalue : Value {}

  public abstract class Literal : Rvalue {}

  public class IntLiteral : Literal
  {
    public int Value;
  }

  public class FloatLiteral : Literal
  {
    public float Value;
  }

  public class StringLiteral : Literal
  {
    public string Value;
  }

  public abstract class Function : NSSNode
  {
    public Lvalue Name;
    public NSSType ReturnType;
    public List<FunctionParameter> Parameters = new List<FunctionParameter>();
  }

  public abstract class AssignmentOpChain : NSSNode {}

  public class Equals : AssignmentOpChain {}

  public class PlusEquals : AssignmentOpChain {}

  public class MinusEquals : AssignmentOpChain {}

  public class DivideEquals : AssignmentOpChain {}

  public class MultiplyEquals : AssignmentOpChain {}

  public class RedundantSemiColon : NSSNode {}
}
