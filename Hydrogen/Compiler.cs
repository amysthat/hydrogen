using System.Diagnostics;
using Hydrogen.Generation;
using Hydrogen.Parsing;
using Hydrogen.Tokenization;

namespace Hydrogen;

public class Compiler(string compilationFile, bool printStage = false)
{
    public string compilationFile = compilationFile;
    public bool printStage = printStage;

    public void Compile(CompilerArgs args)
    {
        args.finalExecutablePath ??= Path.GetDirectoryName(compilationFile)!;

        if (args.objPath is null)
        {
            var supposedObjPath = Path.Combine(Path.GetDirectoryName(compilationFile)!, "obj");

            if (!Directory.Exists(supposedObjPath))
                Directory.CreateDirectory(supposedObjPath);

            args.objPath = supposedObjPath;
        }

        string fileContents = File.ReadAllText(compilationFile);

        Print("Tokenizing...");
        var tokenizer = new Tokenizer(fileContents);
        var tokens = tokenizer.Tokenize();
        ExportTokens(tokens, args.objPath);

        Print("Parsing...");
        var parser = new Parser(tokens);
        var parseTree = parser.ParseProgram();

        Print("Compiling...");
        var generator = new Generator(parseTree)
        {
            performPushPullOptimization = args.pushPullOptimization,
        };
        string asm = generator.GenerateProgram();
        File.WriteAllText(Path.Combine(args.objPath, "out.asm"), asm);


        if (args.dontAssemble)
        {
            Print("\"Don't Assemble\" flag is set. Exiting...");
            return;
        }

        Print("Assembling...");
        if (CommandLineUtility.RunCommand("nasm -f elf64 out.asm", args.objPath) != 0)
            throw new AssemblationException();

        Print("Linking...");
        if (CommandLineUtility.RunCommand($"ld -o \"{Path.Combine(args.finalExecutablePath, "out")}\" out.o", args.objPath) != 0)
            throw new LinkingException();
    }

    private void Print(string message)
    {
        if (!printStage)
            return;

        Console.WriteLine(message);
    }

    private static void ExportTokens(List<Token> tokens, string objPath)
    {
        string output = string.Empty;

        foreach (var token in tokens)
        {
            output += $"{token.Type} {token.Value}\n";

            if (token.Type == TokenType.Semicolon)
                output += "\n";
        }

        File.WriteAllText(Path.Combine(objPath, "tokens.txt"), output);
    }
}

public struct CompilerArgs
{
    public string finalExecutablePath;
    public string objPath;
    public bool dontAssemble;
    public bool pushPullOptimization;
}