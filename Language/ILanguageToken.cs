using NWScript.Lexer;

namespace NWScript.Language
{
  public interface ILanguageToken
  {
    LexerDebugInfo UserData { get; set; }
    string ToString();
  }
}