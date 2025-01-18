using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NWScript.Output
{
  public static class VMTranslations
  {
    private static readonly List<Translation> translations = new List<Translation>
    {
      new Translation("DelayCommand", "DelayCommand({0}, () => {1})", true),
      new Translation("Vector", "new System.Numerics.Vector3({0}, {1}, {2})", true),
      new Translation("NWNX_WebHook_SendWebHookHTTPS", "SendWebHookHTTPS({0}, {1}, {2})", true),
      new Translation("GetStringLength", "{0}.Length", true),
    };

    public static string DefaultFormat => "{0}({1})";

    public static string RemovePrefixes(string pluginName, string fullName, bool includeNWNX = true)
    {
      if (!fullName.StartsWith($"{pluginName}_"))
      {
        return fullName;
      }

      string retVal = fullName.Replace($"{pluginName}_", "");
      return includeNWNX ? retVal.Replace("NWNX_", "") : retVal;
    }

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
      for (int i = 0; i < cleanedArgs.Count; i++)
      {
        cleanedArgs[i] = ProcessArg(pluginName, cleanedArgs[i]);
      }

      if (nssFuncName.StartsWith(pluginName) || nssFuncName.StartsWith($"__{pluginName}"))
      {
        nssFuncName = RemovePrefixes(pluginName, nssFuncName, false);
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
      string[] funcArgs = matches[0].Groups["param"].Captures.Select(capture => capture.Value.TrimStart(' ').TrimEnd(' ')).ToArray();

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

      retVal = RemovePrefixes(pluginName, retVal, false);
      retVal = Output_CSharp.GetSafeVariableName(retVal);

      return retVal;
    }
  }

  public class Translation
  {
    public readonly string NssFunction;
    public readonly string CsharpFunction;
    public readonly bool HasArgs;

    public Translation(string nssFunction, string csharpFormat, bool hasArgs)
    {
      this.NssFunction = nssFunction;
      this.CsharpFunction = csharpFormat;
      this.HasArgs = hasArgs;
    }

    public string Translate(string pluginName, string[] args)
    {
      if (!HasArgs)
      {
        return CsharpFunction;
      }

      List<string> cleanedArgs = args.ToList();
      for (int i = 0; i < cleanedArgs.Count; i++)
      {
        cleanedArgs[i] = VMTranslations.ProcessArg(pluginName, cleanedArgs[i]);
      }

      return string.Format(CsharpFunction, cleanedArgs.Cast<object>().ToArray());
    }
  }
}
