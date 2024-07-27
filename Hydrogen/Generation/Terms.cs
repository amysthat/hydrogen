using System.Text;
using Hydrogen.Generation.Variables;
using Hydrogen.Generation.Variables.Integers;
using Hydrogen.Parsing;
using Hydrogen.Tokenization;

namespace Hydrogen.Generation;

public static class Terms
{
    public static VariableType Integer(Generator generator, NodeTermInteger termInteger, VariableType suggestionType)
    {
        IntegerType integerType = suggestionType is IntegerType _integerType ? _integerType : VariableTypes.SignedInteger64;

        if (Variable.IsUnsignedInteger(integerType) && termInteger.IntegerLiteral.Value!.StartsWith('-'))
        {
            throw new CompilationException(termInteger.LineNumber, "Negative value given for unsigned integer.");
        }

        generator.Push(Variable.MoveIntegerToRegister(generator, termInteger, integerType));
        return integerType;
    }

    public static VariableType Identifier(Generator generator, NodeTermIdentifier termIdentifier)
    {
        string identifier = termIdentifier.Identifier.Value!;

        var variable = generator.GetVariable(identifier);

        if (!variable.HasValue)
        {
            throw new CompilationException(termIdentifier.LineNumber, $"Variable '{identifier}' has not been declared.");
        }

        variable.Value.Type.PushFromStack(generator, identifier);

        return variable!.Value.Type;
    }

    public static VariableType PointerAddress(Generator generator, NodeTermPointerAddress termPointer)
    {
        string identifier = termPointer.Identifier.Identifier.Value!;

        var variable = generator.GetVariable(identifier);

        if (!variable.HasValue)
        {
            throw new CompilationException(termPointer.LineNumber, $"Variable '{identifier}' has not been declared.");
        }

        var variablePosition = generator.GetRelativeVariablePosition(identifier);
        var assemblyString = Generator.CastRelativeVariablePositionToAssembly(variablePosition);

        generator.output += $"    lea rax, [{assemblyString}] ; &{variable!.Value.Type.Keyword} {identifier} variable\n";
        generator.Push("rax");

        return new Pointer { RepresentingType = variable!.Value.Type };
    }

    public static VariableType PointerValue(Generator generator, NodeTermPointerValue termValue)
    {
        string identifier = termValue.Identifier.Identifier.Value!;

        var variable = generator.GetVariable(identifier);

        if (!variable.HasValue)
        {
            throw new CompilationException(termValue.LineNumber, $"Variable '{identifier}' has not been declared.");
        }

        if (variable.Value.Type is not Pointer)
        {
            throw new CompilationException(termValue.LineNumber, $"Variable '{identifier}' is not a pointer.");
        }

        var pointerType = (variable.Value.Type as Pointer)!;

        if (pointerType.RepresentingType is not IntegerType)
        {
            throw new InvalidOperationException();
        }

        var targetType = (pointerType.RepresentingType as IntegerType)!;

        var variablePosition = generator.GetRelativeVariablePosition(identifier);
        var assemblyString = Generator.CastRelativeVariablePositionToAssembly(variablePosition);
        var pointerSize = targetType.AsmPointerSize;

        var aRegister = targetType.AsmARegister;

        generator.output += $"    mov rbx, [{assemblyString}] ; {identifier} pointer\n";
        generator.output += $"    mov {aRegister}, {pointerSize} [rbx] ; Pointer value\n";
        generator.Push("rax");

        return pointerType.RepresentingType;
    }

    public static VariableType GenerateChar(Generator generator, NodeTermChar termChar)
    {
        byte @byte = Encoding.ASCII.GetBytes(@termChar.Char.Value!)[0];

        generator.output += $"    xor rax, rax ; '{termChar.Char.Value!}'\n";
        generator.output += $"    mov al, {@byte}\n";
        generator.Push("rax");

        return VariableTypes.Char;
    }

    public static VariableType GenerateString(Generator generator, NodeTermString termString)
    {
        string @string = termString.String.Value!;

        @string = @string.Replace("\'", "', 39, '");
        @string = @string.Replace("\n", "', 10, '");

        var dataName = $"data{generator.dataCount++}";

        generator.dataSection.Add($"    {dataName}: db '{@string}', 0");

        generator.output += $"    lea rax, [{dataName}]\n";
        generator.Push("rax");

        return VariableTypes.String;
    }

    public static VariableType GenerateBool(Generator generator, NodeTermBool termBool)
    {
        var integerValue = termBool.Value ? 1 : 0;

        generator.output += $"    xor rax, rax ; {termBool.Value}\n";
        generator.output += $"    mov al, {@integerValue}\n";
        generator.Push("rax");

        return VariableTypes.Bool;
    }
}