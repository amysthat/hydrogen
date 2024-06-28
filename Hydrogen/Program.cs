namespace Hydrogen;

using System.Diagnostics;
using Hydrogen.Generation;
using Hydrogen.Parsing;
using Hydrogen.Tokenization;

internal class Program
{ // Add this later: nodeStmtIf.{ Expression = expression, Scope = scope!.Value };
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            args = ["../user/test.hy"];
            // Console.ForegroundColor = ConsoleColor.Red;
            // Console.WriteLine("Incorrect usage. Correct usage:\n -> hydrogen <input.hy>");
            // return 1;
        }

        string content = File.ReadAllText(args[0]);

        DateTime startTime = DateTime.Now;

        Console.WriteLine("Tokenizing...");
        var tokenizer = new Tokenizer(content);
        var tokens = tokenizer.Tokenize();

        ExportTokens(tokens);

        Console.WriteLine("Parsing...");
        var parser = new Parser(tokens);
        var tree = parser.ParseProgram();

        Console.WriteLine("Generating...");
        var generator = new Generator(tree!);
        string asm = generator.GenerateProgram();

        File.WriteAllText("out.asm", asm);

        Console.WriteLine("Generated assembly.");

        RunCommand("nasm -felf64 out.asm");
        RunCommand("ld -o out out.o");

        Console.WriteLine($"Compilation finished. Took {(DateTime.Now - startTime).TotalMilliseconds} ms.");

        return;
    }

    private static void ExportTokens(List<Token> tokens)
    {
        if (File.Exists("tokens.txt"))
            File.Delete("tokens.txt");

        string output = string.Empty;

        foreach (var token in tokens)
        {
            output += $"{token.Type} {token.Value}\n";

            if (token.Type == TokenType.Semicolon)
                output += "\n";
        }

        File.WriteAllText("tokens.txt", output);
    }

    private static void RunCommand(string command)
    {
        Process process = new Process();
        process.StartInfo.FileName = "/bin/bash";
        process.StartInfo.Arguments = "-c \"" + command + "\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.Out.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.Error.WriteLine(e.Data);
            }
        };

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();
    }
}