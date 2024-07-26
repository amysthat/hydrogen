namespace Hydrogen.Generation.Variables.Integers;

public abstract class IntegerType : VariableType
{
    public abstract IntegerSignedness Signedness { get; }
    public abstract string AsmARegister { get; }
    public abstract string AsmBRegister { get; }
    public abstract string AsmPointerSize { get; }

    public override bool Cast(Generator generator, VariableType targetType, int lineNumber)
    {
        if (targetType is not IntegerType integerType)
            return false;

        IntegerCast(generator, integerType, lineNumber);
        return true;
    }

    public abstract void IntegerCast(Generator generator, IntegerType integerType, int lineNumber);
}

public enum IntegerSignedness
{
    SignedInteger,
    UnsignedInteger,
}
