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

    public override void MoveIntoStack(Generator generator, long relativePosition)
    {
        var relativePositionAsm = Generator.CastRelativeVariablePositionToAssembly(relativePosition);

        generator.Pop(AsmARegister);
        generator.output += $"    mov [{relativePositionAsm}], {AsmARegister}\n";
    }

    public override void PushFromStack(Generator generator, string variableIdentifier)
    {
        var variablePosition = generator.GetRelativeVariablePosition(variableIdentifier);
        var assemblyString = Generator.CastRelativeVariablePositionToAssembly(variablePosition);

        generator.output += "    xor rax, rax\n";
        generator.output += $"    mov {AsmARegister}, {AsmPointerSize} [{assemblyString}] ; {Keyword} {variableIdentifier} variable\n";
        generator.Push("rax");
    }

    public override void PushFromPointer(Generator generator, Variable pointer)
    {
        var identifier = pointer.Name;

        var variablePosition = generator.GetRelativeVariablePosition(identifier);
        var assemblyString = Generator.CastRelativeVariablePositionToAssembly(variablePosition);

        generator.output += $"    mov rbx, [{assemblyString}] ; {identifier} pointer\n";
        generator.output += $"    mov {AsmARegister}, {AsmPointerSize} [rbx] ; Pointer value\n";
        generator.Push("rax");
    }

    public abstract void IntegerCast(Generator generator, IntegerType integerType, int lineNumber);
}

public enum IntegerSignedness
{
    SignedInteger,
    UnsignedInteger,
}
