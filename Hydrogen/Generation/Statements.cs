using Hydrogen.Generation.Variables;
using Hydrogen.Parsing;
using String = Hydrogen.Generation.Variables.String;

namespace Hydrogen.Generation;

public static class Statements
{
    public static void Exit(Generator generator, NodeStmtExit exitStatement)
    {
        var exitExprType = generator.GenerateExpression(exitStatement.ReturnCodeExpression, VariableTypes.Byte);

        if (exitExprType is not Byte)
        {
            Console.Error.WriteLine($"Invalid expression type on exit. Expected byte and got {exitExprType.Keyword}.");
            Environment.Exit(1);
        }

        generator.output += "    mov rax, 60 ; exit\n";
        generator.Pop("rdi");
        generator.output += "    syscall\n";
    }

    public static void Write(Generator generator, NodeStmtWrite writeStatement)
    {
        generator.output += "    ; write\n";

        var writeExprType = generator.GenerateExpression(writeStatement.String, VariableTypes.String);

        if (writeExprType is not String)
        {
            Console.Error.WriteLine($"Invalid expression type on exit. Expected {VariableTypes.String} and got {writeExprType}.");
            Environment.Exit(1);
        }

        generator.Pop("rsi ; String pointer");
        generator.output += "    xor rdx, rdx\n";

        var loopLabel = "label" + generator.labelCount++;
        var finishLabel = "label" + generator.labelCount++;

        generator.output += $"    {loopLabel}: ; Find length of null-terminated string\n";
        generator.output += "    cmp byte [rsi + rdx], 0\n";
        generator.output += $"    je {finishLabel}\n";
        generator.output += "    inc rdx\n";
        generator.output += $"    jmp {loopLabel}\n";

        generator.output += $"    {finishLabel}:\n";
        generator.output += "    ; sys_write(stdout, char*, strlen)\n";
        generator.output += "    mov rax, 1\n";
        generator.output += "    mov rdi, 1 ; stdout\n";
        generator.output += "    ; Data (rsi is already set)\n";
        generator.output += "    ; Length (rdx is already set)\n";
        generator.output += "    syscall\n";
    }

    public static void VariableStatement(Generator generator, NodeStmtVariable variableStatement)
    {
        string identifier = variableStatement.Identifier.Value!;

        if (generator.DoesVariableExist(identifier))
        {
            Console.Error.WriteLine($"Variable '{identifier}' is already in use.");
            Environment.Exit(1);
        }

        var variableType = variableStatement.Type;

        generator.output += $"    ; Define {identifier} variable of type {variableType.Keyword}\n";

        var expressionType = generator.GenerateExpression(variableStatement.ValueExpression, variableType);

        if (variableType != expressionType)
        {
            Console.Error.WriteLine($"Type mismatch on variable statement. {variableType.Keyword} != {expressionType.Keyword}");
            Environment.Exit(1);
        }

        generator.workingScope.DefineVariable(identifier, variableType);

        string variableARegister = (variableType as IntegerType)!.AsmARegister;
        long relativePosition = generator.GetRelativeVariablePosition(identifier) + variableType.Size - 1;
        string relativePositionAsm = Generator.CastRelativeVariablePositionToAssembly(relativePosition);

        generator.Pop(variableARegister);
        generator.output += $"    mov [{relativePositionAsm}], {variableARegister}\n";
    }

    public static void VariableAssignment(Generator generator, NodeStmtAssign assignmentStatement)
    {
        string assignIdentifier = assignmentStatement.Identifier.Value!;

        var variable = generator.GetVariable(assignIdentifier);

        if (!variable.HasValue)
        {
            Console.Error.WriteLine($"Variable '{assignIdentifier}' has not been declared yet.");
            Environment.Exit(1);
        }

        generator.output += $"    ; Assign {assignIdentifier}\n";
        var assignExprType = generator.GenerateExpression(assignmentStatement.ValueExpression, variable!.Value.Type);

        if (variable!.Value.Type != assignExprType)
        {
            Console.Error.WriteLine($"Type mismatch on variable assignment. {assignIdentifier} ({variable!.Value.Type.Keyword}) != {assignExprType.Keyword}");
            Environment.Exit(1);
        }

        if (assignExprType is not IntegerType assignIntegerType)
        {
            Console.Error.WriteLine($"Expected integer for variable assignment. {assignIdentifier} {assignExprType.Keyword}");
            Environment.Exit(1);
            throw new Exception(); // To make C# be compliant on the use of assignIntegerType
        }

        string assignARegister = assignIntegerType.AsmARegister;
        long variablePosition = generator.GetRelativeVariablePosition(assignIdentifier);
        var assemblyAssignString = Generator.CastRelativeVariablePositionToAssembly(variablePosition);

        generator.Pop($"{assignARegister}");
        generator.output += $"    mov [{assemblyAssignString}], {assignARegister}\n";
    }

    public static void If(Generator generator, NodeStmtIf ifStatement) // TODO: Fix register usage later
    {
        var finalLabelIndex = generator.labelCount + ifStatement.Elifs.Count + (ifStatement.Else.HasValue ? 1 : 0);

        generator.GenerateExpression(ifStatement.This.Expression, VariableTypes.SignedInteger64);
        generator.output += "    xor rax, rax ; Clear out rax for if statement";
        generator.Pop("rax");
        generator.output += $"    cmp rax, 0\n";
        generator.output += $"    je label{generator.labelCount}\n";
        generator.GenerateScope(ifStatement.This.Scope);
        generator.output += $"    jmp label{finalLabelIndex}\n";

        for (int i = 0; i < ifStatement.Elifs.Count; i++)
        {
            var elifStatement = ifStatement.Elifs[i];

            generator.output += $"label{generator.labelCount}:\n"; generator.labelCount++;
            generator.GenerateExpression(elifStatement.Expression, VariableTypes.SignedInteger64);
            generator.Pop("rax");
            generator.output += $"    cmp rax, 0\n";
            generator.output += $"    je label{generator.labelCount}\n";
            generator.GenerateScope(elifStatement.Scope);
            generator.output += $"    jmp label{finalLabelIndex}\n";
        }

        if (ifStatement.Else.HasValue)
        {
            generator.output += $"label{generator.labelCount}:\n"; generator.labelCount++;
            generator.GenerateScope(ifStatement.Else.Value);
        }

        generator.output += $"label{finalLabelIndex}:\n";
    }

    public static long GetSize(NodeStatement nodeStatement)
    {
        if (nodeStatement is NodeStmtVariable variableStatement)
            return variableStatement.Type.Size;

        return 0;
    }
}