using NWScript.Lexer;

namespace NWScript.Language.Tokens
{
  public class LiteralToken : ILanguageToken
  {
    public LexerDebugInfo UserData { get; set; }

    public override string ToString()
    {
      return m_Literal;
    }

    public NssLiteralType m_LiteralType;
    public string m_Literal;
  }

  public enum NssLiteralType
  {
    Int,
    Float,
    String,
  }
}