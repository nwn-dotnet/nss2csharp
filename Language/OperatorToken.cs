using System.Collections.Generic;
using NWScript.Lexer;

namespace NWScript.Language
{
  public class OperatorToken : ILanguageToken
  {
    public LexerDebugInfo UserData { get; set; }

    public override string ToString()
    {
      foreach (KeyValuePair<char, NssOperators> kvp in Map)
      {
        if (kvp.Value == m_Operator)
        {
          return kvp.Key.ToString();
        }
      }

      return null;
    }

    public static Dictionary<char, NssOperators> Map = new Dictionary<char, NssOperators>
    {
      {'+', NssOperators.Addition},
      {'-', NssOperators.Subtraction},
      {'/', NssOperators.Division},
      {'*', NssOperators.Multiplication},
      {'%', NssOperators.Modulo},
      {'&', NssOperators.And},
      {'|', NssOperators.Or},
      {'!', NssOperators.Not},
      {'~', NssOperators.Inversion},
      {'>', NssOperators.GreaterThan},
      {'<', NssOperators.LessThan},
      {'=', NssOperators.Equals},
      {'?', NssOperators.TernaryQuestionMark},
      {':', NssOperators.TernaryColon},
    };

    public NssOperators m_Operator;
  }

  public enum NssOperators
  {
    Addition,
    Subtraction,
    Division,
    Multiplication,
    Modulo,
    And,
    Or,
    Not,
    Inversion,
    GreaterThan,
    LessThan,
    Equals,
    TernaryQuestionMark,
    TernaryColon,
  }
}