namespace Hydrogen.Tests;

public class CompilerTests
{
    private static string AssemblyDirectory =>
        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()!.Location)!;
    private static string TestDirectory =>
        Path.GetFullPath(Path.Combine(AssemblyDirectory, "../../../..", "tests"));

    private static string GenerationDirectory =>
        Path.GetFullPath(Path.Combine(TestDirectory, "generation"));

    public static IEnumerable<object[]> ConfigFiles =>
        Directory.GetFiles(TestDirectory, "*.hy")
        .Select(x => new object[] { x });

    [Theory]
    [MemberData(nameof(ConfigFiles))]
    public void TestFile(string file)
    {
        CompileFile(file);
    }

    private static void CompileFile(string file)
    {
        if (Directory.Exists(GenerationDirectory))
            Directory.Delete(GenerationDirectory, true);

        Directory.CreateDirectory(GenerationDirectory);

        var compiler = new Compiler(file);
        var args = new CompilerArgs
        {
            finalExecutablePath = GenerationDirectory,
            objPath = GenerationDirectory,
            pushPullOptimization = true,
        };

        GetFileInformation(file, out var title, out int expectedReturnCode);

        try
        {
            compiler.Compile(args);
        }
        catch (Exception ex)
        {
            Assert.Fail($"{title} ({Path.GetFileName(file)}) failed compilation: {ex.Message}");
        }

        int returnCode = CommandLineUtility.RunCommand(Path.Combine(GenerationDirectory, "out"));

        if (returnCode == 139)
        {
            Assert.Fail($"{title} ({Path.GetFileName(file)}) segfaulted.");
        }

        if (returnCode != expectedReturnCode)
        {
            Assert.Fail($"{title} ({Path.GetFileName(file)}) did not meet the return code requirements. {expectedReturnCode} (expected) != {returnCode} (received)");
        }

        Directory.Delete(GenerationDirectory, true);
    }

    private static void GetFileInformation(string file, out string fileTitle, out int expectedReturnCode)
    {
        fileTitle = Path.GetFileNameWithoutExtension(file);
        expectedReturnCode = -1;

        var lines = File.ReadAllText(file).Split('\n');

        foreach (var line in lines)
        {
            if (line.StartsWith("// expects "))
            {
                expectedReturnCode = int.Parse(line[11..].TrimEnd());
            }

            if (line.StartsWith("// title "))
            {
                fileTitle = line[9..].TrimEnd();
            }
        }

        if (expectedReturnCode == -1)
        {
            Assert.Fail($"{file} has no expected return code.");
        }
    }
}