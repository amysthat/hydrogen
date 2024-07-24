using System.Linq;

namespace Hydrogen.Tests;

public class CompilerTests : IClassFixture<TestResultFixture>
{
    private readonly TestResultFixture fixture;

    private static string AssemblyDirectory =>
        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()!.Location)!;
    private static string TestDirectory =>
        Path.GetFullPath(Path.Combine(AssemblyDirectory, "../../../..", "tests"));

    public CompilerTests(TestResultFixture fixture)
    {
        this.fixture = fixture;
    }

    public static IEnumerable<object[]> ConfigFiles =>
        Directory.GetFiles(TestDirectory, "*.hy")
        .Select(x => new object[] { x });

    [Theory]
    [MemberData(nameof(ConfigFiles))]
    public void TestFile(string configFile)
    {
    }
}