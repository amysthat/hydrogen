using Hydrogen.Generation.Variables;
using Hydrogen.Generation.Variables.Integers;
using Hydrogen.Parsing;

namespace Hydrogen.Generation;

public static class Expressions
{
    public static VariableType BinaryExpression(Generator generator, NodeBinExpr binaryExpression, VariableType suggestionType)
    {
        var type = binaryExpression.Type;

        var leftExprType = generator.GenerateBinaryExpression(binaryExpression.Left, suggestionType);
        var rightExprType = generator.GenerateBinaryExpression(binaryExpression.Right, leftExprType);

        if (leftExprType is not IntegerType || rightExprType is not IntegerType)
        {
            throw new CompilationException(binaryExpression.LineNumber, "Expected integer types for binary expression.");
        }

        if (leftExprType != rightExprType)
        {
            throw new CompilationException(binaryExpression.LineNumber, $"Expression type mismatch on binary expression. {leftExprType} != {rightExprType}");
        }

        var aRegister = (leftExprType as IntegerType)!.AsmARegister;
        var bRegister = (leftExprType as IntegerType)!.AsmBRegister;

        generator.Pop($"{bRegister} ; Binary expression"); // Pop the second expression
        generator.Pop(aRegister); // Pop the first expression

        if (type == NodeBinExprType.Add) generator.output += $"    add {aRegister}, {bRegister}\n";
        if (type == NodeBinExprType.Subtract) generator.output += $"    sub {aRegister}, {bRegister}\n";
        if ((leftExprType as IntegerType)!.Signedness == IntegerSignedness.SignedInteger)
        {
            if (type == NodeBinExprType.Multiply) generator.output += $"    imul {bRegister}\n";
            if (type == NodeBinExprType.Divide) generator.output += $"    idiv {bRegister}\n";
        }
        else
        {
            if (type == NodeBinExprType.Multiply) generator.output += $"    mul {bRegister}\n";
            if (type == NodeBinExprType.Divide) generator.output += $"    div {bRegister}\n";
        }

        generator.Push(aRegister);

        return leftExprType;
    }

    public static VariableType Cast(Generator generator, NodeExprCast castExpression)
    {
        var targetType = castExpression.CastType;

        var termType = generator.GenerateTerm(castExpression.Term, targetType);

        if (targetType == termType)
        {
            Console.WriteLine($"Warning: Redundant cast of {termType.Keyword} to {targetType.Keyword}.");
            return targetType!;
        }

        Variable.Cast(generator, termType, targetType, castExpression.LineNumber);
        return targetType!;
    }

    public static class Logic
    {
        public static VariableType Not(Generator generator, NodeLogicNotExpr notExpr)
        {
            generator.Asm("; Not logical expression");

            generator.GenerateLogicalExpression(notExpr.InnerExpression);

            generator.Asm("pop rdi");
            generator.Asm("xor rax, rax");
            generator.Asm("test rdi, rdi");
            generator.Asm("sete al");
            generator.Asm("push rax");

            return VariableTypes.Bool;
        }
    }
}