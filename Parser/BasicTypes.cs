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
    public abstract string NativePushFormat { get; }
    public abstract string NWNXPushFormat { get; }
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
    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushInteger({0})";
    public override string NWNXPushFormat => "NWNXPushInt({0})";
    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopInteger()";
    public override string NWNXPopFormat => "NWNXPopInt()";
  }

  public class FloatType : NSSType
  {
    public override string Declaration => "float";
    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushFloat({0})";
    public override string NWNXPushFormat => "NWNXPushFloat({0})";
    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopFloat()";
    public override string NWNXPopFormat => "NWNXPopFloat()";
  }

  public class StringType : NSSType
  {
    public override string Declaration => "string";
    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushString({0})";
    public override string NWNXPushFormat => "NWNXPushString({0})";
    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopString()";
    public override string NWNXPopFormat => "NWNXPopString()";
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
    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushObject({0})";
    public override string NWNXPushFormat => "NWNXPushObject({0})";
    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopObject()";
    public override string NWNXPopFormat => "NWNXPopObject()";
  }

  public class VectorType : NSSType
  {
    public override string Declaration => "System.Numerics.Vector3";
    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushVector({0})";
    public override string NWNXPushFormat => "NWNXPushVector({0})";
    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopVector()";
    public override string NWNXPopFormat => "NWNXPopVector()";
  }

  public class EffectType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushGameDefinedStructure(ENGINE_STRUCTURE_EFFECT, {0})";
    public override string NWNXPushFormat => "NWNXPushEffect({0})";

    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopGameDefinedStructure(ENGINE_STRUCTURE_EFFECT)";
    public override string NWNXPopFormat => "NWNXPopEffect()";
  }

  public class EventType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushGameDefinedStructure(ENGINE_STRUCTURE_EVENT, {0})";
    public override string NWNXPushFormat => "NWNXPushEvent({0})";

    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopGameDefinedStructure(ENGINE_STRUCTURE_EVENT)";
    public override string NWNXPopFormat => "NWNXPopEvent()";
  }

  public class LocationType : NSSType
  {
    public override string Declaration => "System.IntPtr";
    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushGameDefinedStructure(ENGINE_STRUCTURE_LOCATION, {0})";
    public override string NWNXPushFormat => "NWNXPushLocation({0})";

    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopGameDefinedStructure(ENGINE_STRUCTURE_LOCATION)";
    public override string NWNXPopFormat => "NWNXPopLocation()";
  }

  public class TalentType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushGameDefinedStructure(ENGINE_STRUCTURE_TALENT, {0})";
    public override string NWNXPushFormat => "NWNXPushTalent({0})";

    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopGameDefinedStructure(ENGINE_STRUCTURE_TALENT)";
    public override string NWNXPopFormat => "NWNXPopTalent()";
  }

  public class ItemPropertyType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushGameDefinedStructure(ENGINE_STRUCTURE_ITEMPROPERTY, {0})";
    public override string NWNXPushFormat => "NWNXPushItemProperty({0})";

    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopGameDefinedStructure(ENGINE_STRUCTURE_ITEMPROPERTY)";
    public override string NWNXPopFormat => "NWNXPopItemProperty()";
  }

  public class SQLQueryType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushGameDefinedStructure(ENGINE_STRUCTURE_SQLQUERY, {0})";
    public override string NWNXPushFormat => "NWNXPushSqlquery({0})";

    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopGameDefinedStructure(ENGINE_STRUCTURE_SQLQUERY)";
    public override string NWNXPopFormat => "NWNXPopSqlquery()";
  }

  public class CassowaryType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushGameDefinedStructure(ENGINE_STRUCTURE_CASSOWARY, {0})";
    public override string NWNXPushFormat => "NWNXPushCassowary({0})";

    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopGameDefinedStructure(ENGINE_STRUCTURE_CASSOWARY)";
    public override string NWNXPopFormat => "NWNXPopCassowary()";
  }

  public class JsonType : NSSType
  {
    public override string Declaration => "System.IntPtr";

    public override string NativePushFormat => "global::NWNX.NET.NWNXAPI.StackPushGameDefinedStructure(ENGINE_STRUCTURE_JSON, {0})";
    public override string NWNXPushFormat => "NWNXPushJson({0})";

    public override string NativePopFormat => "global::NWNX.NET.NWNXAPI.StackPopGameDefinedStructure(ENGINE_STRUCTURE_JSON)";
    public override string NWNXPopFormat => "NWNXPopJson()";
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
