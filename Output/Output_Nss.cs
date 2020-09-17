using System.Collections.Generic;
using System.Text;
using NWScript.Language;
using NWScript.Language.Tokens;
using NWScript.Parser;

namespace NWScript.Output
{
    class Output_Nss : IOutput
    {
        public int GetFromTokens(IEnumerable<ILanguageToken> tokens, out string data)
        {
            StringBuilder builder = new StringBuilder();
            LanguageNss nss = new LanguageNss();

            foreach (ILanguageToken token in tokens)
            {
                string tokenAsStr = nss.StringFromToken(token);

                if (tokenAsStr == null)
                {
                    data = null;
                    return 1;
                }

                builder.Append(tokenAsStr);
            }

            data = builder.ToString();
            return 0;
        }

        public int GetFromCU(CompilationUnit cu, out string data, out string className)
        {
            data = null;
            className = null;
            return 1;
        }
    }
}
