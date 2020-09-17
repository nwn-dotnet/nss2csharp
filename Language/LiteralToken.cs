using NWScript.Lexer;

namespace NWScript.Language
{
  public class LiteralToken : ILanguageToken
  {
    public LexerDebugInfo UserData { get; set; }

    public override string ToString()
    {
      return Literal;
    }

    public NssLiteralType LiteralType;
    public string Literal;
  }

  public enum NssLiteralType
  {
    Int,
    Float,
    String,
  }
}