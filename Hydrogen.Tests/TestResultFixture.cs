namespace Hydrogen.Tests;

public class TestResultFixture : IDisposable
{
    public int Passed { get; private set; } = 0;
    public int Failed { get; private set; } = 0;
    public List<(string title, string file, string message)> Errors { get; private set; } = [];

    public void AddSuccess()
    {
        Passed++;
    }

    public void AddFailed(string title, string file, string message)
    {
        Failed++;
        Errors.Add((title, Path.GetFileName(file), message));
    }

    public void Dispose()
    {
        var color = Console.ForegroundColor;

        foreach (var error in Errors)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fail: {error.title} ({error.file}): {error.message}");
        }

        Console.ForegroundColor = color;
        Console.WriteLine($"Summary: Passed: {Passed}, Failed: {Failed}");

        if (Errors.Count > 0)
        {
            Environment.Exit(1);
        }
    }
}