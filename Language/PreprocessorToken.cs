using NWScript.Lexer;

namespace NWScript.Language
{
  public class PreprocessorToken : ILanguageToken
  {
    public NssPreprocessorType PreprocessorType;
    public string Data;

    public LexerDebugInfo UserData { get; set; }

    public override string ToString()
    {
      return Data;
    }
  }

  public enum NssPreprocessorType
  {
    Unknown
  }
}