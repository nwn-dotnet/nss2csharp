using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NWScript.Language;
using NWScript.Output;

namespace NWScript
{
  public static class Nss2CSharp
  {
    private static Stopwatch timer = new Stopwatch();

    private static string destDir;

    public static void Main(string[] args)
    {
      if (args.Length <= 0 || args.Length % 2 != 0)
      {
        Console.WriteLine("Usage: Nss2CSharp <source_dir_1> <dest_dir_1> <source_dir_2...> <dest_dir2...>");
        return;
      }

      for (int i = 0; i < args.Length; i+=2)
      {
        string sourceDir = args[i];
        destDir = args[i + 1];

        string[] scripts = Directory.GetFiles(sourceDir, "*.nss", SearchOption.AllDirectories);

        // Process each file
        foreach (string script in scripts)
        {
          if (ProcessFile(script))
          {
            Console.WriteLine("Processed in {0}ms\n", timer.ElapsedMilliseconds);
          }
        }
      }
    }

    private static bool ProcessFile(string script)
    {
      if (!File.Exists(script))
      {
        Console.Error.WriteLine("Failed to read file {0}", script);
        return false;
      }

      timer.Restart();

      FileInfo fileInfo = new FileInfo(script);
      if (fileInfo.Length == 0)
      {
        Console.WriteLine("Source file empty, skipping.");
        return false;
      }

      string[] scriptLines = File.ReadAllLines(script);
      string scriptData = scriptLines.Aggregate((a, b) => a + "\n" + b);

      Lexer.Lexer lexer = new Lexer.Lexer(scriptData);

      Console.WriteLine("Running lexical analysis. [+{0}ms]", timer.ElapsedMilliseconds);

      int error = lexer.Analyse();
      if (error != 0)
      {
        Console.Error.WriteLine("Failed due to error {0}", error);
        return false;
      }

#if DEBUG
      {
        int preprocessors = lexer.Tokens.Count(token => token.GetType() == typeof(PreprocessorToken));
        int comments = lexer.Tokens.Count(token => token.GetType() == typeof(CommentToken));
        int separators = lexer.Tokens.Count(token => token.GetType() == typeof(SeparatorToken));
        int operators = lexer.Tokens.Count(token => token.GetType() == typeof(OperatorToken));
        int literals = lexer.Tokens.Count(token => token.GetType() == typeof(LiteralToken));
        int keywords = lexer.Tokens.Count(token => token.GetType() == typeof(KeywordToken));
        int identifiers = lexer.Tokens.Count(token => token.GetType() == typeof(IdentifierToken));

        Console.WriteLine("DEBUG: Preprocessor: {0} Comments: {1} Separators: {2} " +
          "Operators: {3} Literals: {4} Keywords: {5} Identifiers: {6}",
          preprocessors, comments, separators, operators, literals, keywords, identifiers);
      }

      {
        Console.WriteLine("DEBUG: Converting tokens back to source and comparing.");
        Output_Nss debugOutput = new Output_Nss();

        error = debugOutput.GetFromTokens(lexer.Tokens, out string data);
        if (error != 0)
        {
          Console.Error.WriteLine("DEBUG: Failed due to error {0}", error);
          return false;
        }

        string[] reformattedData = data.Split('\n');

        int sourceLines = scriptLines.Count();
        int dataLines = reformattedData.Count();

        if (sourceLines != dataLines)
        {
          Console.Error.WriteLine("DEBUG: Failed due to mismatch in line count. " +
            "Source: {0}, Data: {1}", sourceLines, dataLines);

          return false;
        }

        for (int i = 0; i < scriptLines.Length; ++i)
        {
          string sourceLine = scriptLines[i];
          string dataLine = reformattedData[i];

          if (sourceLine != dataLine)
          {
            Console.Error.WriteLine("DEBUG: Failed due to mismatch in line contents. " +
              "Line {0}.\n" +
              "Source line len: {1}\nData line len:   {2}\n" +
              "Source line: {3}\nData line:   {4}",
              i, sourceLine.Length, dataLine.Length, sourceLine, dataLine);

            break;
          }
        }
      }
#endif

      Console.WriteLine("Running parser. [+{0}ms]", timer.ElapsedMilliseconds);
      Parser.Parser parser = new Parser.Parser();
      error = parser.Parse(Path.GetFileName(script), scriptLines, lexer.Tokens);
      if (error != 0)
      {
        Console.Error.WriteLine("Failed due to error {0}", error);
        foreach (string errStr in parser.Errors)
        {
          Console.Error.WriteLine("  {0}", errStr);
        }

        return false;
      }

      Console.WriteLine("Running output. [+{0}ms]", timer.ElapsedMilliseconds);
      Output_CSharp output = new Output_CSharp();
      error = output.GetFromCU(parser.CompilationUnit, out string outputStr, out string className);
      if (error != 0)
      {
        Console.Error.WriteLine("Failed due to error {0}", error);
        return false;
      }

      string outputPath = Path.Combine(destDir, Path.ChangeExtension(className, ".cs"));

      File.WriteAllText(outputPath, outputStr);
      return true;
    }
  }
}
