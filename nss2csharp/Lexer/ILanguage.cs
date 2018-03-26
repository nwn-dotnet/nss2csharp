﻿namespace nss2csharp
{
    public interface IToken
    {
        object UserData { get; set; }
    }

    public interface ILanguage
    {
        // Returns the string representation of the token.
        string StringFromToken(IToken token);
    }
}
