using System.Collections.Generic;
using System.Linq;
using System.Text;
using NWScript.Parser;

namespace NWScript.Output
{
  public class Output_CSharp_NwscriptNss
  {
    // This is a whitelist of functions we've implemented ourselves.
    private static readonly Dictionary<string, string> customImplementations = new Dictionary<string, string>
    {
      {
        "AssignCommand",
        $"{Output_CSharp.GetIndent(2)}public static void AssignCommand(uint oActionSubject, ActionDelegate aActionToAssign)\n" +
        $"{Output_CSharp.GetIndent(2)}{{\n" +
        $"{Output_CSharp.GetIndent(3)}NWNCore.GameManager.ClosureAssignCommand(oActionSubject, aActionToAssign);\n" +
        $"{Output_CSharp.GetIndent(3)}// Function ID 6\n" +
        $"{Output_CSharp.GetIndent(2)}}}"
      },
      {
        "DelayCommand",
        $"{Output_CSharp.GetIndent(2)}public static void DelayCommand(float fSeconds, ActionDelegate aActionToDelay)\n" +
        $"{Output_CSharp.GetIndent(2)}{{\n" +
        $"{Output_CSharp.GetIndent(3)}NWNCore.GameManager.ClosureDelayCommand(OBJECT_SELF, fSeconds, aActionToDelay);\n" +
        $"{Output_CSharp.GetIndent(3)}// Function ID 7\n" +
        $"{Output_CSharp.GetIndent(2)}}}"
      },
      {
        "ActionDoCommand",
        $"{Output_CSharp.GetIndent(2)}public static void ActionDoCommand(ActionDelegate aActionToDo)\n" +
        $"{Output_CSharp.GetIndent(2)}{{\n" +
        $"{Output_CSharp.GetIndent(3)}NWNCore.GameManager.ClosureActionDoCommand(OBJECT_SELF, aActionToDo);\n" +
        $"{Output_CSharp.GetIndent(3)}// Function ID 294\n" +
        $"{Output_CSharp.GetIndent(2)}}}"
      }
    };

    // This is a list of custom constants we define.
    private static readonly List<string> customConstants = new List<string>
    {
      "public const uint OBJECT_INVALID = 0x7F000000;",
      "public static uint OBJECT_SELF => NWNCore.GameManager.ObjectSelf;"
    };

    private readonly StringBuilder stringBuilder = new StringBuilder();
    private int internalCallId = 0;

    public int GetFromCU(CompilationUnit cu, string className, out string data)
    {
      stringBuilder.Clear();
      internalCallId = 0;

      stringBuilder.AppendLine("using System;");
      stringBuilder.AppendLine("using System.Numerics;");
      stringBuilder.AppendLine();

      stringBuilder.AppendLine("namespace NWN.Core");
      stringBuilder.AppendLine("{");
      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}public static class NWScript");
      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}{{");

      ProcessCustomConstants();

      for (int index = 0; index < cu.m_Nodes.Count; index++)
      {
        NSSNode node = cu.m_Nodes[index];
        NSSNode nextNode = index < cu.m_Nodes.Count - 1 ? cu.m_Nodes[index + 1] : null;

        if (node is LineComment lineComment)
        {
          string xmlEscapedComment = lineComment.Comment
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("&", "&amp;");

          stringBuilder.Append($"{Output_CSharp.GetIndent(2)}/// " + xmlEscapedComment);
          if (nextNode is Comment)
          {
            stringBuilder.Append("<br/>");
          }

          stringBuilder.AppendLine();
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
          BuildConstant(lvalueDecl);
          if (cu.m_Nodes.Count > index + 2 && !(cu.m_Nodes[index + 1] is LvalueDeclSingleWithAssignment))
          {
            stringBuilder.AppendLine();
          }
        }

        if (node is UnknownPreprocessor unknPreprocessor)
        {
          BuildDefine(unknPreprocessor);
        }

        if (node is FunctionDeclaration funcDecl)
        {
          BuildMethod(funcDecl);
        }
      }

      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}}}");
      stringBuilder.AppendLine("}");

      data = stringBuilder.ToString();
      return 0;
    }

    private void ProcessCustomConstants()
    {
      foreach (string constant in customConstants)
      {
        stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}{constant}");
      }
    }

    private void BuildDefine(UnknownPreprocessor unknPreprocessor)
    {
      string ppValue = unknPreprocessor.Value;

      string[] elements = ppValue.Split().Where(element => !string.IsNullOrEmpty(element)).ToArray();
      if (elements.Length == 3)
      {
        string instruction = elements[0];
        string macro = elements[1];
        string value = elements[2];

        if (instruction == "#define")
        {
          if (macro == "ENGINE_NUM_STRUCTURES")
          {
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}public const int {macro} = {value};");
            return;
          }

          const string structurePrefix = "ENGINE_STRUCTURE_";
          if (macro.StartsWith(structurePrefix))
          {
            string structureName = value.ToUpperInvariant();
            int structureValue = int.Parse(macro.Substring(structurePrefix.Length));
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}public const int {structurePrefix}{structureName} = {structureValue};");
            return;
          }
        }
      }

      stringBuilder.AppendLine($"// {ppValue}");
    }

    private void BuildConstant(LvalueDeclSingleWithAssignment lvalueDecl)
    {
      string type = lvalueDecl.m_Type.Declaration;
      string name = lvalueDecl.m_Lvalue.Identifier;
      string value = lvalueDecl.m_Expression.m_Expression;
      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}public const {type} {name} = {value}{(lvalueDecl.m_Type.GetType() == typeof(FloatType) && !value.EndsWith("f") ? "f" : "")};");
    }

    private void BuildMethod(FunctionDeclaration funcDecl)
    {
      string name = funcDecl.Name.Identifier;

      if (customImplementations.ContainsKey(name))
      {
        stringBuilder.AppendLine(customImplementations[name]);
        internalCallId++;
        stringBuilder.AppendLine();
        return;
      }

      string retType = funcDecl.ReturnType.Declaration;

      List<string> funcParams = new List<string>();
      foreach (FunctionParameter param in funcDecl.Parameters)
      {
        string paramType = param.m_Type.Declaration;
        string paramName = param.m_Lvalue.Identifier;
        string paramStr = paramType + " " + paramName;

        if (param is FunctionParameterWithDefault def)
        {
          string defaultAsStr = Output_CSharp.GetValueAsString(def.m_Default, false);
          paramStr += " = " + defaultAsStr;
        }

        funcParams.Add(paramStr);
      }

      string parameters = funcParams.Count == 0 ? "" : funcParams.Aggregate((a, b) => a + ", " + b);

      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}public static {retType} {name}({parameters})");
      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}{{");

      for (int i = funcDecl.Parameters.Count - 1; i >= 0; --i)
      {
        FunctionParameter param = funcDecl.Parameters[i];
        stringBuilder.AppendLine(Output_CSharp.GetIndent(3) + Output_CSharp.GetStackPush(param.m_Type, param.m_Lvalue, false) + ";");
      }

      stringBuilder.AppendLine(Output_CSharp.GetIndent(3) + Output_CSharp.GetInternalCall(internalCallId++) + ";");

      if (funcDecl.ReturnType.GetType() != typeof(VoidType))
      {
        stringBuilder.AppendLine($"{Output_CSharp.GetIndent(3)}return " + Output_CSharp.GetStackPop(funcDecl.ReturnType) + ";");
      }

      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}}}");
      stringBuilder.AppendLine();
    }
  }
}
