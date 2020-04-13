using nss2csharp.Parser;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nss2csharp.Output
{
    class Output_CSharp_NwscriptNss
    {
        // This is a whitelist of functions we've implementedo ourselves.
        private static List<string> s_BuiltIns = new List<string>
        {
            // ACTION FUNCTIONS
            "AssignCommand",
            "DelayCommand",
            "ActionDoCommand"
        };

        public int GetFromCU(CompilationUnit cu, string className, out string data)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("namespace NWN");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("  partial class NWScript");
            stringBuilder.AppendLine("  {");

            int internalCallId = 0;

            foreach (Node node in cu.m_Nodes)
            {
                if (node is LineComment lineComment)
                {
                    stringBuilder.AppendLine("    /// " + lineComment.m_Comment);
                }

                if (node is BlockComment blockComment)
                {
                    stringBuilder.AppendLine("    /*");
                    foreach (string line in blockComment.m_CommentLines)
                    {
                        stringBuilder.AppendLine("    " + line);
                    }

                    stringBuilder.AppendLine("    */");
                }

                if (node is LvalueDeclSingleWithAssignment lvalueDecl)
                {
                    string type = Output_CSharp.GetTypeAsString(lvalueDecl.m_Type);
                    string name = lvalueDecl.m_Lvalue.m_Identifier;
                    string value = lvalueDecl.m_Expression.m_Expression;
                    stringBuilder.AppendLine($"    public const {type} {name} = {value}{(lvalueDecl.m_Type.GetType() == typeof(FloatType) && !value.EndsWith("f") ? "f" : "")};");
                }

                if (node is FunctionDeclaration funcDecl)
                {
                    string name = funcDecl.m_Name.m_Identifier;

                    if (s_BuiltIns.Contains(name))
                    {
                        ++internalCallId;
                        continue;
                    }

                    string retType = Output_CSharp.GetTypeAsString(funcDecl.m_ReturnType);

                    List<string> funcParams = new List<string>();
                    foreach (FunctionParameter param in funcDecl.m_Parameters)
                    {
                        string paramType = Output_CSharp.GetTypeAsString(param.m_Type);
                        string paramName = param.m_Lvalue.m_Identifier;
                        string paramStr = paramType + " " + paramName;

                        if (param is FunctionParameterWithDefault def)
                        {
                            string defaultAsStr = Output_CSharp.GetValueAsString(def.m_Default, false);
                            paramStr += " = " + defaultAsStr;
                        }

                        funcParams.Add(paramStr);
                    }

                    string parameters = funcParams.Count == 0 ? "" : funcParams.Aggregate((a, b) => a + ", " + b);

                    stringBuilder.AppendLine($"    public static {retType} {name}({parameters})");
                    stringBuilder.AppendLine("    {");

                    for (int i = funcDecl.m_Parameters.Count - 1; i >= 0; --i)
                    {
                        FunctionParameter param = funcDecl.m_Parameters[i];
                        stringBuilder.AppendLine("      " + Output_CSharp.GetStackPush(param.m_Type, param.m_Lvalue, false) + ";");
                    }

                    stringBuilder.AppendLine("      " + Output_CSharp.GetInternalCall(internalCallId++) + ";");

                    if (funcDecl.m_ReturnType.GetType() != typeof(VoidType))
                    {
                        stringBuilder.AppendLine("      return " + Output_CSharp.GetStackPop(funcDecl.m_ReturnType) + ";");
                    }

                    stringBuilder.AppendLine("    }");
                    stringBuilder.AppendLine("");
                }
            }

            stringBuilder.AppendLine("    }");
            stringBuilder.AppendLine("}");

            data = stringBuilder.ToString();
            return 0;
        }
    }
}
