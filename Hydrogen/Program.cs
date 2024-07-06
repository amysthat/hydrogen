﻿namespace Hydrogen;

using System.Diagnostics;
using Hydrogen.Generation;
using Hydrogen.Parsing;
using Hydrogen.Tokenization;

internal class Program
{ // Add this later: nodeStmtIf.{ Expression = expression, Scope = scope!.Value };
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
#if DEBUG
            args = ["user/test.hy"];
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "../../../../.."));
#elif RELEASE
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Incorrect usage. Correct usage:\n -> hydrogen <input.hy>");
            return 1;
#endif
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("hydrogen Compiler - working beta 0.1");
        Console.ForegroundColor = ConsoleColor.Gray;

        if (!File.Exists(args[0]))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("File does not exist.");
            return 1;
        }

        string content;
        try
        {
            content = File.ReadAllText(args[0]);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error opening file: " + ex.Message);
            return 1;
        }

        DateTime startTime = DateTime.Now;

        Console.WriteLine("Tokenizing...");
        var tokenizer = new Tokenizer(content);
        var tokens = tokenizer.Tokenize();

        if (args.Contains("/exporttokens"))
            ExportTokens(tokens);

        Console.WriteLine("Parsing...");
        var parser = new Parser(tokens);
        var tree = parser.ParseProgram();

        Console.WriteLine("Compiling...");
        var generator = new Generator(tree!);
        string asm = generator.GenerateProgram();

        File.WriteAllText("out.asm", asm);

        if (args.Contains("/noasmcompile"))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\"/noasmcompile\" flag is set. Will not compile.");

            if (args.Contains("/nocleanup"))
            {
                Console.WriteLine("\"/nocleanup\" flag is set. This is redundant.");
            }

            return 0;
        }

        Console.WriteLine("Assembling...");
        Compile();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Compilation finished. Took {(DateTime.Now - startTime).TotalMilliseconds} ms.");

#if RELEASE
        if (!args.Contains("/nocleanup"))
            Cleanup();
#endif

        Console.WriteLine();
        int exitCode = RunCommand("./out");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Program exited with exit code {exitCode}.");

        if (exitCode == 139) // Segmentation fault
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Program failed with segmentation fault.\nThis is usually a fault of the compiler, not a user error.\nFor more info, check out https://en.wikipedia.org/wiki/Segfault.");
            return 1;
        }

        return 0;
    }

    private static void Compile()
    {
        if (RunCommand("nasm -felf64 out.asm") != 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Assembling failed. This is a fault of the compiler, not a user error.");
            Console.WriteLine("If you don't have \"nasm\" installed on your system, please install it.");
            Environment.Exit(1);
        }

        if (RunCommand("ld -o out out.o") != 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Linking failed. This is a fault of the compiler, not a user error.");
            Console.WriteLine("If you don't have \"ld\" installed on your system, please install it.");
            Environment.Exit(1);
        }
    }

    private static void Cleanup()
    {
        try
        {
            File.Delete("out.asm");
            File.Delete("out.o");
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("File cleanup failed.");
            Console.ForegroundColor = ConsoleColor.Gray;
        }
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

    private static int RunCommand(string command)
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
        return process.ExitCode;
    }
}