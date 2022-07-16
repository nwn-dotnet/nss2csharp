using System.Text;
using NWScript.Parser;

namespace NWScript.Output
{
    public static class OutputTokens
    {
        public static void BuildConstant(StringBuilder stringBuilder, LvalueDeclSingleWithAssignment lvalueDecl)
        {
            string type = lvalueDecl.m_Type.Declaration;
            string name = lvalueDecl.m_Lvalue.Identifier;
            string value = lvalueDecl.m_Expression.m_Expression;

            switch (lvalueDecl.m_Type)
            {
                case IntType:
                    if (long.TryParse(value, out long intValue) && intValue is > int.MaxValue or < int.MinValue)
                    {
                        stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}public const {type} {name} = unchecked((int){value});");
                    }
                    else
                    {
                        stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}public const {type} {name} = {value};");
                    }

                    break;
                default:
                    stringBuilder.AppendLine($"{Output_CSharp.GetIndent(2)}public const {type} {name} = {value}{(lvalueDecl.m_Type.GetType() == typeof(FloatType) && !value.EndsWith("f") ? "f" : "")};");
                    break;
            }
        }
    }
}
