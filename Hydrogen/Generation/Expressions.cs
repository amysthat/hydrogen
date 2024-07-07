using Hydrogen.Generation.Variables;
using Hydrogen.Parsing;

namespace Hydrogen.Generation;

public static class Expressions
{
    public static VariableType BinaryExpression(Generator generator, NodeBinExpr binaryExpression, VariableType suggestionType)
    {
        var type = binaryExpression.Type;

        var leftExprType = generator.GenerateExpression(binaryExpression.Left, suggestionType);
        var rightExprType = generator.GenerateExpression(binaryExpression.Right, leftExprType);

        if (leftExprType is not IntegerType || rightExprType is not IntegerType)
        {
            Console.Error.WriteLine("Expected integer types for binary expression.");
            Environment.Exit(1);
        }

        if (leftExprType != rightExprType)
        {
            Console.Error.WriteLine($"Expression type mismatch on binary expression. {leftExprType} != {rightExprType}");
            Environment.Exit(1);
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

        var expressionType = generator.GenerateExpression(castExpression.Expression, targetType);

        if (targetType == expressionType)
        {
            Console.WriteLine($"Warning: Redundant cast of {targetType.Keyword}.");
        }

        Variable.Cast(generator, expressionType, targetType);
        return targetType!;
    }
}