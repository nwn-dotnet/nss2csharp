using NWScript.Lexer;

namespace NWScript.Language
{
  public class CommentToken : ILanguageToken
  {
    public LexerDebugInfo UserData { get; set; }

    public override string ToString()
    {
      if (CommentType == CommentType.LineComment)
      {
        return "//" + Comment;
      }
      else if (CommentType == CommentType.BlockComment)
      {
        return "/*" + Comment + (Terminated ? "*/" : "");
      }

      return null;
    }

    public CommentType CommentType;
    public string Comment;
    public bool Terminated; // If a block style comment, whether it was actually terminated
  }

  public enum CommentType
  {
    LineComment,
    BlockComment
  }
}