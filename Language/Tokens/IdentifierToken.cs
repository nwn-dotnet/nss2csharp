using NWScript.Lexer;

namespace NWScript.Language.Tokens
{
  public class IdentifierToken : ILanguageToken
  {
    public LexerDebugInfo UserData { get; set; }

    public override string ToString()
    {
      return Identifier;
    }

    public string Identifier;
  }
}