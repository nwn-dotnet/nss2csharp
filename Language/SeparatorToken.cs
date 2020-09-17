using System.Collections.Generic;
using NWScript.Lexer;

namespace NWScript.Language
{
  public class SeparatorToken : ILanguageToken
  {
    public LexerDebugInfo UserData { get; set; }

    public override string ToString()
    {
      foreach (KeyValuePair<char, NssSeparators> kvp in Map)
      {
        if (kvp.Value == m_Separator)
        {
          return kvp.Key.ToString();
        }
      }

      return null;
    }

    public static Dictionary<char, NssSeparators> Map = new Dictionary<char, NssSeparators>
    {
      {' ', NssSeparators.Space},
      {'\n', NssSeparators.NewLine},
      {'(', NssSeparators.OpenParen},
      {')', NssSeparators.CloseParen},
      {'{', NssSeparators.OpenCurlyBrace},
      {'}', NssSeparators.CloseCurlyBrace},
      {';', NssSeparators.Semicolon},
      {'\t', NssSeparators.Tab},
      {',', NssSeparators.Comma},
      {'[', NssSeparators.OpenSquareBracket},
      {']', NssSeparators.CloseSquareBracket}
    };

    public NssSeparators m_Separator;
  }

  public enum NssSeparators
  {
    Space,
    NewLine,
    OpenParen,
    CloseParen,
    OpenCurlyBrace,
    CloseCurlyBrace,
    Semicolon,
    Tab,
    Comma,
    OpenSquareBracket,
    CloseSquareBracket
  }
}