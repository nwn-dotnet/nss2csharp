using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using NWScript.Language;
using NWScript.Parser;

namespace NWScript.Output
{
  public class Output_CSharp
  {
    public const string Indent = "  ";

    public static string GetIndent(int depth)
    {
      string retVal = "";
      for (int i = 0; i < depth; i++)
      {
        retVal += Indent;
      }

      return retVal;
    }

    public int GetFromTokens(IEnumerable<ILanguageToken> tokens, out string data)
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

    public static string GetValueAsString(Value value, bool isPlugin)
    {
      const string floatFormatStr = "0.0#######";

      if (value is Lvalue lv)
      {
        if (lv.Identifier == "OBJECT_SELF")
        {
          return "OBJECT_INVALID";
        }

        if (lv.Identifier == "LOCATION_INVALID")
        {
          return "default";
        }

        return GetSafeVariableName(lv.Identifier);
      }
      else if (value is IntLiteral intLit)
      {
        return intLit.Value.ToString();
      }
      else if (value is FloatLiteral floatLit)
      {
        return floatLit.Value.ToString(floatFormatStr) + "f";
      }
      else if (value is StringLiteral stringLit)
      {
        return stringLit.Value;
      }
      else if (value is VectorLiteral vectorLiteral)
      {
        if (vectorLiteral.m_X.Value == 0 && vectorLiteral.m_Y.Value == 0 && vectorLiteral.m_Z.Value == 0)
        {
          return "default";
        }

        return "null";
      }

      return null;
    }

    public static string GetStackPush(NSSType type, Value val, bool isNWNX)
    {
      string format = isNWNX ? type.NWNXPushFormat : type.NativePushFormat;
      return format != null ? string.Format(format, GetValueAsString(val, false)) : null;
    }

    public static string GetStackPop(NSSType type)
    {
      return type.NativePopFormat;
    }

    public static string GetInternalCall(int id)
    {
      return string.Format("VM.Call({0})", id);
    }

    public static string GetSafeVariableName(string variable)
    {
      SyntaxKind kind = SyntaxFacts.GetKeywordKind(variable);
      return SyntaxFacts.IsReservedKeyword(kind) ? $"@{variable}" : variable;
    }
  }
}
