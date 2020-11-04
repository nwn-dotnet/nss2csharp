using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NWScript.Output
{
  public static class VMTranslations
  {
    private static readonly List<Translation> translations = new List<Translation>
    {
      new Translation("NWNX_CallFunction", "VM.NWNX.Call()"),
      new Translation("NWNX_PushArgumentInt", "VM.NWNX.StackPush({0})"),
      new Translation("NWNX_PushArgumentFloat", "VM.NWNX.StackPush({0})"),
      new Translation("NWNX_PushArgumentObject", "VM.NWNX.StackPush({0})"),
      new Translation("NWNX_PushArgumentString", "VM.NWNX.StackPush({0})"),
      new Translation("NWNX_PushArgumentEffect", "VM.NWNX.StackPush({0}, ENGINE_STRUCTURE_EFFECT)"),
      new Translation("NWNX_PushArgumentItemProperty", "VM.NWNX.StackPush({0}, ENGINE_STRUCTURE_ITEM_PROPERTY)"),
      new Translation("NWNX_GetReturnValueInt", "VM.NWNX.StackPopInt()"),
      new Translation("NWNX_GetReturnValueFloat", "VM.NWNX.StackPopFloat()"),
      new Translation("NWNX_GetReturnValueObject", "VM.NWNX.StackPopObject()"),
      new Translation("NWNX_GetReturnValueString", "VM.NWNX.StackPopString()"),
      new Translation("NWNX_GetReturnValueEffect", "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_EFFECT)"),
      new Translation("NWNX_GetReturnValueItemProperty", "VM.NWNX.StackPopStruct(ENGINE_STRUCTURE_ITEM_PROPERTY)"),
      new Translation("DelayCommand", "DelayCommand({0}, () => {1})"),
      new Translation("Vector", "new System.Numerics.Vector3({0}, {1}, {2})"),
      new Translation("NWNX_WebHook_SendWebHookHTTPS", "SendWebHookHTTPS({0}, {1}, {2})"),
      new Translation("GetStringLength", "{0}.Length"),
    };

    public static string DefaultFormat => "{0}({1})";

    public static string TranslateCall(string pluginName, string nssFuncName, string[] args)
    {
      foreach (Translation translation in translations)
      {
        if (translation.NssFunction == nssFuncName)
        {
          return translation.Translate(pluginName, args);
        }
      }

      List<string> cleanedArgs = args.ToList();
      cleanedArgs.RemoveAll(arg => IsBadArg(pluginName, arg));

      for (int i = 0; i < cleanedArgs.Count; i++)
      {
        cleanedArgs[i] = ProcessArg(pluginName, cleanedArgs[i]);
      }

      if (nssFuncName.StartsWith(pluginName))
      {
        nssFuncName = nssFuncName.Replace($"{pluginName}_", "");
      }

      return string.Format(DefaultFormat, nssFuncName, string.Join(", ", cleanedArgs));
    }

    public static string TryTranslate(string pluginName, string statement)
    {
      MatchCollection matches = Parser.Parser.FunctionCallRegex.Matches(statement);
      if (matches.Count == 0)
      {
        return statement;
      }

      // Function call
      string funcName = matches[0].Groups["function_name"].Captures[0].Value;
      string[] funcArgs = matches[0].Groups["param"].Captures.Select(capture =>
      {
        // String literal in the argument, don't mess with it.
        if (capture.Value.Contains("\""))
        {
          return capture.Value;
        }

        return capture.Value.Replace(" ", "");
      }).ToArray();

      return TranslateCall(pluginName, funcName, funcArgs);
    }

    public static string ProcessArg(string pluginName, string arg)
    {
      string retVal = arg;

      // Vectors
      if (retVal.EndsWith(".x") || retVal.EndsWith(".y") || retVal.EndsWith(".z"))
      {
        retVal = retVal.Substring(0, retVal.Length - 1) + char.ToUpper(retVal[^1]);
        retVal = retVal.TrimEnd('x');
      }

      // Functions
      retVal = TryTranslate(pluginName, retVal);

      retVal = retVal.Replace($"{pluginName}_", "");
      retVal = Output_CSharp.GetSafeVariableName(retVal);

      return retVal;
    }

    public static bool IsBadArg(string pluginName, string arg)
    {
      return arg == pluginName ||
        arg == "sFunc";
    }
  }

  public class Translation
  {
    public readonly string NssFunction;
    public readonly string CsharpFunction;

    public Translation(string nssFunction, string csharpFormat)
    {
      this.NssFunction = nssFunction;
      this.CsharpFunction = csharpFormat;
    }

    public string Translate(string pluginName, string[] args)
    {
      List<string> cleanedArgs = args.ToList();
      cleanedArgs.RemoveAll(arg => VMTranslations.IsBadArg(pluginName, arg));

      for (int i = 0; i < cleanedArgs.Count; i++)
      {
        cleanedArgs[i] = VMTranslations.ProcessArg(pluginName, cleanedArgs[i]);
      }

      return string.Format(CsharpFunction, cleanedArgs.Cast<object>().ToArray());
    }
  }
}
