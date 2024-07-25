namespace Hydrogen.Tests;

public class CompilerTests : IClassFixture<TestResultFixture>
{
    private readonly TestResultFixture _fixture;

    public CompilerTests(TestResultFixture fixture)
    {
        _fixture = fixture;
    }

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
        if (CompileFile(file, out var error))
            _fixture.AddSuccess();
        else
            _fixture.AddFailed(error!.Value.title, file, error!.Value.error);

        Directory.Delete(GenerationDirectory, true);
    }

    private static bool CompileFile(string file, out (string title, string error)? error)
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
            error = (title, "Compilation failed: " + ex);
            return false;
        }

        int returnCode = CommandLineUtility.RunCommand(Path.Combine(GenerationDirectory, "out"));

        if (returnCode == 139)
        {
            error = (title, "Segfault.");
            return false;
        }

        if (returnCode != expectedReturnCode)
        {
            error = (title, $"Exit code mismatch: {expectedReturnCode} (expected) != {returnCode} (received)");
            return false;
        }

        error = null;
        return true;
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