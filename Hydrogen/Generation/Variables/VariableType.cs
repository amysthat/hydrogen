namespace Hydrogen.Generation.Variables;

public abstract class VariableType
{
    public abstract string Keyword { get; }
    public abstract long Size { get; }

    public abstract bool Cast(Generator generator, VariableType targetType, int lineNumber);

    public static bool operator ==(VariableType? x, VariableType? y) => x is not null && y is not null && x.Keyword == y.Keyword;
    public static bool operator !=(VariableType? x, VariableType? y) => x is not null && y is not null && x.Keyword != y.Keyword;

    public override bool Equals(object? obj)
    {
        if (obj is not VariableType variableType)
        {
            return false;
        }

        return variableType.Keyword == Keyword;
    }

    public override int GetHashCode() => Keyword.GetHashCode();

    public override string ToString() => Keyword;
}
