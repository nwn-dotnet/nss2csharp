using NWScript.Lexer;

namespace NWScript.Language.Tokens
{
  public interface ILanguageToken
  {
    LexerDebugInfo UserData { get; set; }
    string ToString();
  }
}