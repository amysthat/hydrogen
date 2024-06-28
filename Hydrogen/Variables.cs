using Hydrogen.Generation;

namespace Hydrogen;

public enum VariableType
{
    UnsignedInteger64,
    SignedInteger64,
    Byte,
}

public struct Variable
{
    public ulong StackLocation;
    public VariableType Type;
}

public static class Variables
{
    public static void Cast(Generator generator, VariableType castType, VariableType targetType)
    {
        if (castType == targetType)
            return;

        switch (castType)
        {
            case VariableType.SignedInteger64:
                switch (targetType)
                {
                    case VariableType.UnsignedInteger64:
                        return;
                    case VariableType.Byte:
                        return;
                }
                break;
            case VariableType.UnsignedInteger64:
                switch (targetType)
                {
                    case VariableType.SignedInteger64:
                        return;
                    case VariableType.Byte:
                        return;
                }
                break;
            case VariableType.Byte:
                switch (targetType)
                {
                    case VariableType.SignedInteger64:
                        return;
                    case VariableType.UnsignedInteger64:
                        return;
                }
                break;
        }

        throw new NotImplementedException($"Casting {castType} to {targetType} is not supported yet.");
    }

    public static ulong GetSize(VariableType type)
    {
        switch (type)
        {
            case VariableType.SignedInteger64:
            case VariableType.UnsignedInteger64:
                return 8;
            case VariableType.Byte:
                return 1;
        }

        throw new InvalidOperationException($"Unknown variable type: {type}");
    }
}