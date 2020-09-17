using System.Collections.Generic;
using NWScript.Lexer;

namespace NWScript.Language.Tokens
{
  public class KeywordToken : ILanguageToken
  {
    public LexerDebugInfo UserData { get; set; }

    public override string ToString()
    {
      foreach (KeyValuePair<string, NssKeywords> kvp in Map)
      {
        if (kvp.Value == m_Keyword)
        {
          return kvp.Key;
        }
      }

      return null;
    }

    public static Dictionary<string, NssKeywords> Map = new Dictionary<string, NssKeywords>
    {
      { "if",             NssKeywords.If },
      { "else",           NssKeywords.Else },
      { "for",            NssKeywords.For },
      { "while",          NssKeywords.While },
      { "do",             NssKeywords.Do },
      { "switch",         NssKeywords.Switch },
      { "break",          NssKeywords.Break },
      { "return",         NssKeywords.Return },
      { "case",           NssKeywords.Case },
      { "const",          NssKeywords.Const },
      { "void",           NssKeywords.Void },
      { "int",            NssKeywords.Int },
      { "float",          NssKeywords.Float },
      { "string",         NssKeywords.String },
      { "struct",         NssKeywords.Struct },
      { "object",         NssKeywords.Object },
      { "location",       NssKeywords.Location },
      { "vector",         NssKeywords.Vector },
      { "itemproperty",   NssKeywords.ItemProperty },
      { "sqlquery",       NssKeywords.SqlQuery },
      { "effect",         NssKeywords.Effect },
      { "talent",         NssKeywords.Talent },
      { "action",         NssKeywords.Action },
      { "event",          NssKeywords.Event },
      { "default",        NssKeywords.Default },
    };

    public NssKeywords m_Keyword;
  }

  public enum NssKeywords
  {
    If,
    Else,
    For,
    While,
    Do,
    Switch,
    Break,
    Return,
    Case,
    Const,
    Void,
    Int,
    Float,
    String,
    Struct,
    Object,
    Location,
    Vector,
    ItemProperty,
    Effect,
    Talent,
    Action,
    Event,
    SqlQuery,
    Default
  }
}