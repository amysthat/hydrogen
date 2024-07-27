namespace Hydrogen.Generation.Variables;

public class Bool : VariableType
{
    public override string Keyword => "bool";
    public override long Size => 1;

    public override bool Cast(Generator generator, VariableType targetType, int lineNumber)
    {
        if (targetType is Integers.IntegerType)
            return true; // Sure, go ahead.

        return false;
    }

    public override void MoveIntoStack(Generator generator, long relativePosition)
    {
        var relativePositionAsm = Generator.CastRelativeVariablePositionToAssembly(relativePosition);

        generator.Pop("al");
        generator.output += $"    mov [{relativePositionAsm}], al\n";
    }

    public override void PushFromStack(Generator generator, string variableIdentifier)
    {
        var variablePosition = generator.GetRelativeVariablePosition(variableIdentifier);
        var assemblyString = Generator.CastRelativeVariablePositionToAssembly(variablePosition);

        generator.output += "    xor rax, rax\n";
        generator.output += $"    mov al, byte [{assemblyString}] ; bool {variableIdentifier} variable\n";
        generator.Push("rax");
    }
}