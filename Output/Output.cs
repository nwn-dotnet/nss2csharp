using System.Collections.Generic;
using NWScript.Language;
using NWScript.Language.Tokens;
using NWScript.Parser;

namespace NWScript.Output
{
    public interface IOutput
    {
        int GetFromTokens(IEnumerable<ILanguageToken> tokens, out string data);

        int GetFromCU(CompilationUnit cu, out string data, out string className);
    }
}
