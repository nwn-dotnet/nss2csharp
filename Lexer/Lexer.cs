using System;
using System.Collections.Generic;
using System.Linq;
using NWScript.Language;

namespace NWScript.Lexer
{
  public class Lexer
  {
    public readonly List<ILanguageToken> Tokens = new List<ILanguageToken>();
    public readonly List<LexerDebugRange> DebugRanges = new List<LexerDebugRange>();

    private readonly string data;
    private int chBaseIndex;

    // This is an optimization.
    // We're progressing through all tokens in a linear fashion with no look-backs.
    // Therefore, we don't need to find the range each time (which is very slow) - we can just
    // start from our last index, because that's guaranteed to be the earliest one that matters.
    private int lastDebugDataIndex = 0;

    public Lexer(string data)
    {
      this.data = data;
    }

    public int Analyse()
    {
      {
        // Set up the debug data per line.
        int lineNum = 0;
        int cumulativeLen = 0;
        foreach (string line in data.Split('\n'))
        {
          LexerDebugRange range = new LexerDebugRange();
          range.Line = lineNum;
          range.IndexStart = cumulativeLen;
          range.IndexEnd = cumulativeLen + line.Length;
          DebugRanges.Add(range);

          lineNum = range.Line + 1;
          cumulativeLen = range.IndexEnd + 1;
        }
      }

      int streamPosition = 0;
      while (streamPosition < data.Length)
      {
        int lastPosition = streamPosition;

        {
          // PREPROCESSOR
          streamPosition = Preprocessor();
          if (streamPosition != lastPosition) continue;
        }

        {
          // COMMENTS
          streamPosition = Comment();
          if (streamPosition != lastPosition) continue;
        }

        {
          // SEPARATORS
          streamPosition = Separator();
          if (streamPosition != lastPosition) continue;
        }

        {
          // OPERATORS
          streamPosition = Operator();
          if (streamPosition != lastPosition) continue;
        }

        {
          // LITERALS
          streamPosition = Literal();
          if (streamPosition != lastPosition) continue;
        }

        {
          // KEYWORDS
          streamPosition = Keyword();
          if (streamPosition != lastPosition) continue;
        }

        {
          // IDENTIFIERS
          streamPosition = Identifier();
          if (streamPosition != lastPosition) continue;
        }

        return 1;
      }

      return 0;
    }

    private int Preprocessor()
    {
      char ch = data[chBaseIndex];
      if (ch == '#')
      {
        // Just scan for a new line or eof, then add this in.
        int chScanningIndex = chBaseIndex;

        while (++chScanningIndex <= data.Length)
        {
          bool eof = chScanningIndex >= data.Length - 1;

          bool proceed = eof;
          if (!proceed)
          {
            char chScanning = data[chScanningIndex];
            proceed = SeparatorToken.Map.ContainsKey(chScanning) &&
              SeparatorToken.Map[chScanning] == NssSeparators.NewLine;
          }

          if (proceed)
          {
            PreprocessorToken preprocessor = new PreprocessorToken();
            preprocessor.PreprocessorType = NssPreprocessorType.Unknown;

            int chStartIndex = chBaseIndex;
            int chEndIndex = eof ? data.Length : chScanningIndex;

            if (chStartIndex == chEndIndex)
            {
              preprocessor.Data = "";
            }
            else
            {
              preprocessor.Data = data.Substring(chStartIndex, chEndIndex - chStartIndex);
            }

            int chNewBaseIndex = chEndIndex;
            AttachDebugData(preprocessor, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

            Tokens.Add(preprocessor);
            chBaseIndex = chNewBaseIndex;
            break;
          }
        }
      }

      return chBaseIndex;
    }

    private int Comment()
    {
      char ch = data[chBaseIndex];
      if (ch == '/')
      {
        int chNextIndex = chBaseIndex + 1;
        if (chNextIndex < data.Length)
        {
          char nextCh = data[chNextIndex];
          if (nextCh == '/')
          {
            // Line comment - scan for end of line, and collect.
            int chScanningIndex = chNextIndex;

            while (++chScanningIndex <= data.Length)
            {
              bool eof = chScanningIndex >= data.Length - 1;

              bool proceed = eof;
              if (!proceed)
              {
                char chScanning = data[chScanningIndex];
                proceed = SeparatorToken.Map.ContainsKey(chScanning) &&
                  SeparatorToken.Map[chScanning] == NssSeparators.NewLine;
              }

              if (proceed)
              {
                CommentToken comment = new CommentToken();
                comment.CommentType = CommentType.LineComment;

                int chStartIndex = chNextIndex + 1;
                int chEndIndex = eof ? data.Length : chScanningIndex;

                if (chStartIndex == chEndIndex)
                {
                  comment.Comment = "";
                }
                else
                {
                  comment.Comment = data.Substring(chStartIndex, chEndIndex - chStartIndex);
                }

                int chNewBaseIndex = chEndIndex;
                AttachDebugData(comment, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

                Tokens.Add(comment);
                chBaseIndex = chNewBaseIndex;
                break;
              }
            }
          }
          else if (nextCh == '*')
          {
            // Block comment - scan for the closing */, ignoring everything else.
            bool terminated = false;
            int chScanningIndex = chNextIndex + 1;
            while (++chScanningIndex < data.Length)
            {
              char chScanning = data[chScanningIndex];
              if (chScanning == '/')
              {
                char chScanningLast = data[chScanningIndex - 1];
                if (chScanningLast == '*')
                {
                  terminated = true;
                  break;
                }
              }
            }

            bool eof = chScanningIndex >= data.Length - 1;

            CommentToken comment = new CommentToken();
            comment.CommentType = CommentType.BlockComment;
            comment.Terminated = terminated;

            int chStartIndex = chBaseIndex + 2;
            int chEndIndex = !terminated && eof ? data.Length : chScanningIndex + (terminated ? -1 : 0);
            comment.Comment = data.Substring(chStartIndex, chEndIndex - chStartIndex);

            int chNewBaseIndex = eof ? data.Length : chScanningIndex + 1;
            AttachDebugData(comment, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

            Tokens.Add(comment);
            chBaseIndex = chNewBaseIndex;
          }
        }
      }

      return chBaseIndex;
    }

    private int Separator()
    {
      char ch = data[chBaseIndex];

      if (SeparatorToken.Map.ContainsKey(ch))
      {
        SeparatorToken separator = new SeparatorToken();
        separator.m_Separator = SeparatorToken.Map[ch];

        int chNewBaseIndex = chBaseIndex + 1;
        AttachDebugData(separator, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

        Tokens.Add(separator);
        chBaseIndex = chNewBaseIndex;
      }

      return chBaseIndex;
    }

    private int Operator()
    {
      char ch = data[chBaseIndex];

      if (OperatorToken.Map.ContainsKey(ch))
      {
        OperatorToken op = new OperatorToken();
        op.m_Operator = OperatorToken.Map[ch];

        int chNewBaseIndex = chBaseIndex + 1;
        AttachDebugData(op, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

        Tokens.Add(op);
        chBaseIndex = chNewBaseIndex;
      }

      return chBaseIndex;
    }

    private int Literal()
    {
      char ch = data[chBaseIndex];
      bool isString = ch == '"';
      bool isNumber = char.IsNumber(ch);
      if (isString || isNumber)
      {
        LiteralToken literal = null;

        bool seenDecimalPlace = false;
        bool isHex = false;
        int chScanningIndex = chBaseIndex;
        while (++chScanningIndex < data.Length)
        {
          char chScanning = data[chScanningIndex];

          if (isString)
          {
            // If we're a string, we just scan to the next ", except for escaped ones.
            // There might be some weirdness with new lines here - but we'll just ignore them.
            char chScanningLast = data[chScanningIndex - 1];
            if (chScanning == '"' && chScanningLast != '\\')
            {
              literal = new LiteralToken();
              literal.LiteralType = NssLiteralType.String;

              int chStartIndex = chBaseIndex;
              int chEndIndex = chScanningIndex + 1;

              if (chStartIndex == chEndIndex)
              {
                literal.Literal = "";
              }
              else
              {
                literal.Literal = data.Substring(chStartIndex, chEndIndex - chStartIndex);
              }

              Tokens.Add(literal);
              int chNewBaseIndex = chEndIndex;
              AttachDebugData(literal, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

              chBaseIndex = chNewBaseIndex;
              break;
            }
          }
          else
          {
            // If we're a number, we need to keep track of whether we've seen a decimal place,
            // and scan until we're no longer a number or a decimal place.
            if (chScanning == '.')
            {
              seenDecimalPlace = true;
            }
            else if (chScanning == 'x' || chScanning == 'X' && chScanningIndex - chBaseIndex == 1)
            {
              isHex = true;
            }
            else if ((!isHex || !Uri.IsHexDigit(chScanning)) && !char.IsNumber(chScanning) && (!seenDecimalPlace || (seenDecimalPlace && chScanning != 'f')))
            {
              literal = new LiteralToken();
              literal.LiteralType = seenDecimalPlace ? NssLiteralType.Float : NssLiteralType.Int;
              literal.IsHex = isHex;

              int chStartIndex = chBaseIndex;
              int chEndIndex = chScanningIndex;
              literal.Literal = data.Substring(chStartIndex, chEndIndex - chStartIndex);

              int chNewBaseIndex = chScanningIndex;
              AttachDebugData(literal, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

              Tokens.Add(literal);
              chBaseIndex = chNewBaseIndex;
              break;
            }
          }
        }
      }

      return chBaseIndex;
    }

    private int Keyword()
    {
      char ch = data[chBaseIndex];

      if (Tokens.Count == 0 ||
        Tokens.Last().GetType() == typeof(SeparatorToken) ||
        Tokens.Last().GetType() == typeof(OperatorToken))
      {
        foreach (KeyValuePair<string, NssKeywords> kvp in KeywordToken.Map)
        {
          if (chBaseIndex + kvp.Key.Length >= data.Length)
          {
            continue; // This would overrun us.
          }

          string strFromData = data.Substring(chBaseIndex, kvp.Key.Length);
          if (strFromData == kvp.Key)
          {
            // We're matched a keyword, e.g. 'int ', but we might have, e.g. 'int integral', and the
            // 'integral' is an identifier. So let's only accept a keyword if the character proceeding it
            // is a separator or an operator.

            int chNextAlongIndex = chBaseIndex + kvp.Key.Length;
            bool accept = false;

            if (!accept)
            {
              char chNextAlong = data[chNextAlongIndex];
              accept = SeparatorToken.Map.ContainsKey(chNextAlong) || OperatorToken.Map.ContainsKey(chNextAlong);
            }

            if (accept)
            {
              KeywordToken keyword = new KeywordToken();
              keyword.m_Keyword = kvp.Value;

              int chNewBaseIndex = chNextAlongIndex;
              AttachDebugData(keyword, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

              Tokens.Add(keyword);
              chBaseIndex = chNewBaseIndex;
              break;
            }
          }
        }
      }

      return chBaseIndex;
    }

    private int Identifier()
    {
      char ch = data[chBaseIndex];

      int chScanningIndex = chBaseIndex;
      bool eof;

      do
      {
        eof = chScanningIndex >= data.Length;
        char chScanning = data[chScanningIndex];

        bool hasOperator = OperatorToken.Map.ContainsKey(chScanning); // An identifier ends at the first sight of an operator.
        bool hasSeparator = SeparatorToken.Map.ContainsKey(chScanning);
        if (eof || hasSeparator || hasOperator)
        {
          IdentifierToken identifier = new IdentifierToken();

          int chStartIndex = chBaseIndex;
          int chEndIndex = chScanningIndex + (eof ? 1 : 0);
          identifier.Identifier = data.Substring(chStartIndex, chEndIndex - chStartIndex);

          int chNewBaseIndex = chScanningIndex + (eof ? 1 : 0);
          AttachDebugData(identifier, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

          Tokens.Add(identifier);
          chBaseIndex = chNewBaseIndex;
          break;
        }

        ++chScanningIndex;
      }
      while (chScanningIndex < data.Length);

      return chBaseIndex;
    }

    private void AttachDebugData(ILanguageToken token, List<LexerDebugRange> debugRanges, int indexStart, int indexEnd)
    {
      LexerDebugInfo debugInfo = new LexerDebugInfo();

      bool foundStart = false;
      bool foundEnd = false;

      for (int i = lastDebugDataIndex; i < debugRanges.Count; ++i)
      {
        int startIndex = i;
        int endIndex = i;

        if (indexStart >= debugRanges[startIndex].IndexStart && indexStart <= debugRanges[startIndex].IndexEnd)
        {
          foundStart = true;

          for (int j = i; j < debugRanges.Count; ++j)
          {
            if (indexStart >= debugRanges[endIndex].IndexStart && indexStart <= debugRanges[endIndex].IndexEnd)
            {
              foundEnd = true;
              endIndex = j;
              break;
            }
          }

          if (!foundEnd)
          {
            break;
          }

          debugInfo.LineStart = i;
          debugInfo.LineEnd = endIndex;
          debugInfo.ColumnStart = indexStart - debugRanges[startIndex].IndexStart;
          debugInfo.ColumnEnd = indexEnd - debugRanges[endIndex].IndexStart;
          lastDebugDataIndex = i;
          break;
        }
      }

      if (!foundStart || !foundEnd)
      {
        Console.Error.WriteLine("Warning: No start or end debug range found for range {0} to {1}", indexStart, indexEnd);
      }

      token.UserData = debugInfo;
    }
  }
}