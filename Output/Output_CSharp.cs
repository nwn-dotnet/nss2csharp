using System.CodeDom.Compiler;
using nss2csharp.Language;
using nss2csharp.Parser;
using System.Collections.Generic;
using System.IO;

namespace nss2csharp.Output
{
    class Output_CSharp : IOutput
    {
        private static CodeDomProvider CodeDomProvider = CodeDomProvider.CreateProvider("C#");
        public const string Indent = "    ";

        public static string GetIndent(int depth)
        {
            string retVal = "";
            for (int i = 0; i < depth; i++)
            {
                retVal += Indent;
            }

            return retVal;
        }

        public int GetFromTokens(IEnumerable<IToken> tokens, out string data)
        {
            data = null;
            return 1;
        }

        public int GetFromCU(CompilationUnit cu, out string data, out string className)
        {
            if (cu.m_Metadata.m_Name == "nwscript.nss")
            {
                Output_CSharp_NwscriptNss output = new Output_CSharp_NwscriptNss();
                className = "NWScript";
                return output.GetFromCU(cu, className, out data);
            }
            else if (cu.m_Metadata.m_Name.StartsWith("nwnx") && cu.m_Metadata.m_Name.EndsWith(".nss"))
            {
                Output_CSharp_NWNX output = new Output_CSharp_NWNX();

                className = Path.GetFileNameWithoutExtension(cu.m_Metadata.m_Name);
                className = className.Substring(className.LastIndexOf("_") + 1);
                className = char.ToUpper(className[0]) + className.Substring(1) + "Plugin";

                return output.GetFromCU(cu, className, out data);
            }

            data = null;
            className = null;
            return 1;
        }

        public static string GetTypeAsString(Type type, string pluginName = null)
        {
            if (type.GetType() == typeof(VoidType))              return "void";
            else if (type.GetType() == typeof(IntType))          return "int";
            else if (type.GetType() == typeof(FloatType))        return "float";
            else if (type.GetType() == typeof(StringType))       return "string";
            else if (type.GetType() == typeof(StructType))       return pluginName == null ? ((StructType) type).m_TypeName : ((StructType) type).m_TypeName.Replace($"{pluginName}_", "").Replace("NWNX_", "");
            else if (type.GetType() == typeof(ObjectType))       return "uint";
            else if (type.GetType() == typeof(LocationType))     return "System.IntPtr";
            else if (type.GetType() == typeof(VectorType))       return "System.Numerics.Vector3";
            else if (type.GetType() == typeof(ItemPropertyType)) return "System.IntPtr";
            else if (type.GetType() == typeof(EffectType))       return "System.IntPtr";
            else if (type.GetType() == typeof(TalentType))       return "System.IntPtr";
            else if (type.GetType() == typeof(EventType))        return "System.IntPtr";
            else if (type.GetType() == typeof(ActionType))       return "System.Action";

            return null;
        }

        public static string GetValueAsString(Value value, bool isPlugin)
        {
            const string floatFormatStr = "0.0#######";

            if (value is Lvalue lv)
            {
                if (lv.m_Identifier == "OBJECT_SELF")
                {
                    return isPlugin ? "NWScript.OBJECT_INVALID" : "OBJECT_INVALID";
                }

                if (isPlugin && !lv.m_Identifier.StartsWith("NWNX_"))
                {
                    return $"NWScript.{lv.m_Identifier}";
                }

                return GetSafeVariableName(lv.m_Identifier);
            }
            else if (value is IntLiteral intLit)
            {
                return intLit.m_Value.ToString();
            }
            else if (value is FloatLiteral floatLit)
            {
                return floatLit.m_Value.ToString(floatFormatStr) + "f";
            }
            else if (value is StringLiteral stringLit)
            {
                return stringLit.m_Value;
            }
            else if (value is VectorLiteral vectorLiteral)
            {
                return "null";
            }

            return null;
        }

        public static string GetNWNXStackPushFormat(System.Type type)
        {
            if (type == typeof(IntType)) return "VM.NWNX.StackPush({0})";
            else if (type == typeof(FloatType)) return "VM.NWNX.StackPush({0})";
            else if (type == typeof(StringType)) return "VM.NWNX.StackPush({0})";
            else if (type == typeof(ObjectType)) return "VM.NWNX.StackPush({0})";
            else if (type == typeof(VectorType)) return "VM.NWNX.StackPush({0})";
            else if (type == typeof(EffectType)) return "VM.NWNX.StackPush({0}, NWScript.ENGINE_STRUCTURE_EFFECT)";
            else if (type == typeof(ItemPropertyType)) return "VM.NWNX.StackPush({0}, NWScript.ENGINE_STRUCTURE_ITEM_PROPERTY)";
            else
            {
                return null;
            }
        }

        public static string GetStackPushFormat(Type type)
        {
            if (type.GetType() == typeof(IntType)) return "VM.StackPush({0})";
            else if (type.GetType() == typeof(FloatType)) return "VM.StackPush({0})";
            else if (type.GetType() == typeof(StringType)) return "VM.StackPush({0})";
            else if (type.GetType() == typeof(ObjectType)) return "VM.StackPush({0})";
            else if (type.GetType() == typeof(VectorType)) return "VM.StackPush({0})";
            else if (type.GetType() == typeof(EffectType)) return "VM.StackPush({0}, ENGINE_STRUCTURE_EFFECT)";
            else if (type.GetType() == typeof(EventType)) return "VM.StackPush({0}, ENGINE_STRUCTURE_EVENT)";
            else if (type.GetType() == typeof(LocationType)) return "VM.StackPush({0}, ENGINE_STRUCTURE_LOCATION)";
            else if (type.GetType() == typeof(TalentType)) return "VM.StackPush({0}, ENGINE_STRUCTURE_TALENT)";
            else if (type.GetType() == typeof(ItemPropertyType)) return "VM.StackPush({0}, ENGINE_STRUCTURE_ITEM_PROPERTY)";
            else
            {
                return null;
            }
        }

        public static string GetStackPush(Type type, Value val, bool isNWNX)
        {
            string format = isNWNX ? GetNWNXStackPushFormat(type.GetType()) : GetStackPushFormat(type);
            return format != null ? string.Format(format, GetValueAsString(val, false)) : null;
        }

        public static string GetStackPop(Type type)
        {
            if (type.GetType() == typeof(IntType)) return "VM.StackPopInt()";
            else if (type.GetType() == typeof(FloatType)) return "VM.StackPopFloat()";
            else if (type.GetType() == typeof(StringType)) return "VM.StackPopString()";
            else if (type.GetType() == typeof(ObjectType)) return "VM.StackPopObject()";
            else if (type.GetType() == typeof(VectorType)) return "VM.StackPopVector()";
            else if (type.GetType() == typeof(EffectType)) return "VM.StackPopStruct(ENGINE_STRUCTURE_EFFECT)";
            else if (type.GetType() == typeof(EventType)) return "VM.StackPopStruct(ENGINE_STRUCTURE_EVENT)";
            else if (type.GetType() == typeof(LocationType)) return "VM.StackPopStruct(ENGINE_STRUCTURE_LOCATION)";
            else if (type.GetType() == typeof(TalentType)) return "VM.StackPopStruct(ENGINE_STRUCTURE_TALENT)";
            else if (type.GetType() == typeof(ItemPropertyType)) return "VM.StackPopStruct(ENGINE_STRUCTURE_ITEM_PROPERTY)";

            return null;
        }

        public static string GetInternalCall(int id)
        {
            return string.Format("Call({0})", id);
        }

        public static string GetNWNXStackPop(Type type)
        {
            if (type.GetType() == typeof(IntType)) return "VM.NWNX.StackPopInt()";
            else if (type.GetType() == typeof(FloatType)) return "VM.NWNX.StackPopFloat()";
            else if (type.GetType() == typeof(StringType)) return "VM.NWNX.StackPopString()";
            else if (type.GetType() == typeof(ObjectType)) return "VM.NWNX.StackPopObject()";
            else if (type.GetType() == typeof(VectorType)) return "VM.NWNX.StackPopVector()";
            else if (type.GetType() == typeof(EffectType)) return "VM.NWNX.StackPopStruct(NWScript.ENGINE_STRUCTURE_EFFECT)";
            else if (type.GetType() == typeof(EventType)) return "VM.NWNX.StackPopStruct(NWScript.ENGINE_STRUCTURE_EVENT)";
            else if (type.GetType() == typeof(LocationType)) return "VM.NWNX.StackPopStruct(NWScript.ENGINE_STRUCTURE_LOCATION)";
            else if (type.GetType() == typeof(TalentType)) return "VM.NWNX.StackPopStruct(NWScript.ENGINE_STRUCTURE_TALENT)";
            else if (type.GetType() == typeof(ItemPropertyType)) return "VM.NWNX.StackPopStruct(NWScript.ENGINE_STRUCTURE_ITEM_PROPERTY)";

            return null;
        }

        public static string GetNWNXSetFunction(string pluginNameVar, string methodName)
        {
            return $"VM.NWNX.SetFunction({pluginNameVar}, \"{methodName}\")";
        }

        public static string GetSafeVariableName(string variable)
        {
            return !CodeDomProvider.IsValidIdentifier(variable) ? $"@{variable}" : variable;
        }
    }
}
