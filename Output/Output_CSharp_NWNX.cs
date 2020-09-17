using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NWScript.Parser;

namespace NWScript.Output
{
  public class Output_CSharp_NWNX
  {
    public int GetFromCU(CompilationUnit cu, string className, out string data)
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.AppendLine("namespace NWN.Core.NWNX");
      stringBuilder.AppendLine("{");

      int attributePos = stringBuilder.Length;
      stringBuilder.AppendLine();
      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}public class {className}");
      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}{{");

      string pluginNameVar = null;

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

        if (node is FunctionDeclaration funcDecl)
        {
          string name = funcDecl.Name.Identifier.Replace(pluginNameVar + "_", "");
          string retType = RemovePrefixes(funcDecl.ReturnType.Declaration, pluginNameVar);

          List<string> funcParams = new List<string>();
          foreach (FunctionParameter param in funcDecl.Parameters)
          {
            string paramType = RemovePrefixes(param.m_Type.Declaration, pluginNameVar);
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

          string methodName = funcDecl.Name.Identifier.Substring(funcDecl.Name.Identifier.LastIndexOf("_", StringComparison.Ordinal) + 1);

          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(3)}" + Output_CSharp.GetNWNXSetFunction(pluginNameVar, methodName) + ";");

          for (int i = funcDecl.Parameters.Count - 1; i >= 0; --i)
          {
            FunctionParameter param = funcDecl.Parameters[i];

            if (param.m_Type is StructType paramStructType)
            {
              StructDeclaration paramStruct = GetStruct(cu, paramStructType);
              if (paramStruct == null)
              {
                data = null;
                return -1;
              }

              for (int j = 0; j < paramStruct.m_Members.Count; j++)
              {
                LvalueDeclSingle structDec = paramStruct.m_Members[j] as LvalueDeclSingle;
                if (structDec == null)
                {
                  continue;
                }

                stringBuilder.AppendLine($"{Output_CSharp.GetIndent(3)}{string.Format(structDec.m_Type.NWNXPushFormat, $"{param.m_Lvalue.Identifier}.{structDec.m_Lvalue.Identifier}")};");
              }
            }
            else
            {
              stringBuilder.AppendLine($"{Output_CSharp.GetIndent(3)}" + Output_CSharp.GetStackPush(param.m_Type, param.m_Lvalue, true) + ";");
            }
          }

          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(3)}VM.NWNX.Call();");

          if (funcDecl.ReturnType is StructType retStructType)
          {
            StructDeclaration retStruct = GetStruct(cu, retStructType);
            if (retStruct == null)
            {
              data = null;
              return -1;
            }

            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(3)}{RemovePrefixes(retStructType.Declaration, pluginNameVar)} retVal;");
            for (int i = retStruct.m_Members.Count - 1; i >= 0; i--)
            {
              LvalueDeclSingle dec = retStruct.m_Members[i] as LvalueDeclSingle;
              if (dec == null)
              {
                continue;
              }

              string structMemName = dec.m_Lvalue.Identifier;
              stringBuilder.AppendLine($"{Output_CSharp.GetIndent(3)}retVal.{structMemName} = {dec.m_Type.NWNXPopFormat};");
            }

            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(3)}return retVal;");
          }

          else if (funcDecl.ReturnType.GetType() != typeof(VoidType))
          {
            stringBuilder.AppendLine($"{Output_CSharp.GetIndent(3)}return " + funcDecl.ReturnType.NWNXPopFormat + ";");
          }

          stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}}}");
          stringBuilder.AppendLine();
        }
      }

      stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}}}");

      // Structures
      foreach (StructDeclaration structDeclaration in cu.m_Nodes.OfType<StructDeclaration>())
      {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"{Output_CSharp.GetIndent(1)}public struct {RemovePrefixes(structDeclaration.m_Name.Identifier, pluginNameVar)}");
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

    private string RemovePrefixes(string fullName, string pluginName)
    {
      return fullName.Replace($"{pluginName}_", "").Replace("NWNX_", "");
    }

    private StructDeclaration GetStruct(CompilationUnit cu, StructType structType)
    {
      foreach (NSSNode node in cu.m_Nodes)
      {
        if (node is StructDeclaration structDeclaration && structType.TypeName == structDeclaration.m_Name.Identifier)
        {
          return structDeclaration;
        }
      }

      return null;
    }
  }
}