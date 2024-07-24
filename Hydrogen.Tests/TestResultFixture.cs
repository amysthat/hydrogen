namespace Hydrogen.Tests;

public class TestResultFixture : IDisposable
{
    public int Passed { get; private set; } = 0;
    public int Failed { get; private set; } = 0;

    public void AddResult(bool isSuccess)
    {
        if (isSuccess)
        {
            Passed++;
        }
        else
        {
            Failed++;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Console.WriteLine($"Summary: Passed: {Passed}, Failed: {Failed}");
    }
}
