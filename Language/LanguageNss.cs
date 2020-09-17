using NWScript.Language.Tokens;

namespace NWScript.Language
{
  public class LanguageNss
  {
    public string StringFromToken(ILanguageToken token)
    {
      return token.ToString();
    }
  }
}