using Hydrogen.Generation;

namespace Hydrogen;

public enum VariableType
{
    UnsignedInteger64,
    SignedInteger64,
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
                }
                break;
            case VariableType.UnsignedInteger64:
                switch (targetType)
                {
                    case VariableType.SignedInteger64:
                        return;
                }
                break;
        }

        throw new NotImplementedException($"Casting {castType} to {targetType} is not supported yet.");
    }
}