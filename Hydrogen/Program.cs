namespace Hydrogen;

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
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), ".."));
#elif RELEASE
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine("Usage: hydrogen <input.hy>");
            Console.WriteLine("Arguments:");
            Console.WriteLine(" /exporttokens - Export tokens to tokens.txt");
            Console.WriteLine(" /dontassemble - Don't assemble compiled assembly");
            Console.WriteLine();
            Console.WriteLine("Recommended arguments:");
            Console.WriteLine(" /optimizepushpull - Remove unnecessary push pull usage");
            return 0;
#endif
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("hydrogen Compiler - beta 0.1");
        Console.ForegroundColor = ConsoleColor.Gray;

        if (!File.Exists(args[0]))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("File does not exist.");
            return 1;
        }

        DateTime startTime = DateTime.Now;

        var objPath = Path.Combine(Directory.GetCurrentDirectory(), "obj");
        if (!Directory.Exists(objPath))
            Directory.CreateDirectory(objPath);

        var compiler = new Compiler(args[0], printStage: true);
        var compilerArgs = new CompilerArgs
        {
            pushPullOptimization = args.Contains("/optimizepushpull"),
            dontAssemble = args.Contains("/dontassemble"),
            finalExecutablePath = Directory.GetCurrentDirectory(),
            objPath = objPath,
        };

        try
        {
            compiler.Compile(compilerArgs);
        }
        catch (AssemblationException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Assembling failed. This is a fault of the compiler, not a user error.");
            Console.WriteLine("If you don't have \"nasm\" installed on your system, please install it.");
            Environment.Exit(1);
        }
        catch (LinkingException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Linking failed. This is a fault of the compiler, not a user error.");
            Console.WriteLine("If you don't have \"ld\" installed on your system, please install it.");
            Environment.Exit(1);
        }
        catch (CompilerDerivedException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Environment.Exit(1);
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Compilation finished. Took {(DateTime.Now - startTime).TotalMilliseconds} ms.");

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