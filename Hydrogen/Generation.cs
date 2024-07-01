using Hydrogen.Parsing;

namespace Hydrogen.Generation;

public class Generator
{
    private NodeProgram program;
    public string output;
    private int labelCount;
    private Scope workingScope;

    public class Scope
    {
        public ulong CurrentStackSize;
        public readonly Map<string, Variable> variables = new();

        public required Scope Parent;

        public ulong DefineVariable(string variableName, VariableType type)
        {
            var variablePosition = CurrentStackSize;

            var variable = new Variable
            {
                Type = type,
                BaseStackDifference = variablePosition,
                Size = Variables.GetSize(type),
                Owner = this,
            };

            variables.Add(variableName, variable);

            CurrentStackSize += variable.Size;

            if (CurrentStackSize > 128)
            {
                throw new OutOfMemoryException("Scope size reached more than 128.");
            }

            return variablePosition;
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Generator(NodeProgram program)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        this.program = program;
        output = string.Empty;
    }

    private VariableType GenerateTerm(NodeTerm term, VariableType suggestionType)
    {
        switch (term.Type)
        {
            case NodeTermType.Integer:
                var integerType = Variables.IsInteger(suggestionType) ? suggestionType : VariableType.SignedInteger64;

                if (!Variables.IsSignedInteger(integerType) && term.Integer.Int_Lit.Value!.StartsWith('-'))
                {
                    Console.Error.WriteLine("Negative value given for unsigned integer.");
                    Environment.Exit(1);
                }

                Push(Variables.MoveIntegerToRegister(this, term.Integer, integerType));
                return integerType;

            case NodeTermType.Identifier:
                string identifier = term.Identifier.Identifier.Value!;

                var variable = GetVariable(identifier);

                if (!variable.HasValue)
                {
                    Console.Error.WriteLine($"Variable '{identifier}' has not been declared.");
                    Environment.Exit(1);
                }

                var variablePosition = GetVariablePositionOnStackRelativeToWorkingScope(identifier);
                var assemblyString = CastVariablePositionRelativeToWorkingSpaceToAssemblyString(variablePosition);

                var aRegister = Variables.GetARegisterForIntegerType(variable!.Value.Type);
                var asmPointerSize = Variables.GetAsmPointerSizeForIntegerType(variable!.Value.Type);

                output += $"    mov {aRegister}, {asmPointerSize} [{assemblyString}] ; {variable!.Value.Type} {identifier} variable\n";
                Push("rax");

                return variable!.Value.Type;

            case NodeTermType.Parenthesis:
                return GenerateExpression(term.Parenthesis.Expression, suggestionType);

            default:
                throw new InvalidProgramException("Reached unreachable state on GenerateTerm().");
        }
    }

    private VariableType GenerateExpression(NodeExpression expression, VariableType suggestionType)
    {
        switch (expression.Type)
        {
            case NodeExpressionType.Term:
                var term = expression.Term;

                return GenerateTerm(term, suggestionType);

            case NodeExpressionType.BinaryExpression:
                return GenerateBinaryExpression(expression.BinaryExpression, suggestionType);

            case NodeExpressionType.Cast:
                var castExpression = expression.Cast.Expression;
                var targetType = expression.Cast.CastType;

                var expressionType = GenerateExpression(castExpression, targetType);

                if (targetType == expressionType)
                {
                    Console.WriteLine($"Warning: Redundant cast of {targetType}.");
                }

                Variables.Cast(this, expressionType, targetType);
                return targetType;

            default:
                throw new InvalidProgramException("Reached unreachable state on GenerateExpression().");
        }
    }

    private VariableType GenerateBinaryExpression(NodeBinaryExpression binaryExpression, VariableType suggestionType)
    {
        var type = binaryExpression.Type;

        var leftExprType = GenerateExpression(binaryExpression.Left, suggestionType);
        var rightExprType = GenerateExpression(binaryExpression.Right, leftExprType);

        if (!Variables.IsInteger(leftExprType) || !Variables.IsInteger(rightExprType))
        {
            Console.Error.WriteLine("Expected integer types for binary expression.");
            Environment.Exit(1);
        }

        if (leftExprType != rightExprType)
        {
            Console.Error.WriteLine("Expression type mismatch on binary expression.");
            Environment.Exit(1);
        }

        var aRegister = Variables.GetARegisterForIntegerType(leftExprType);
        var bRegister = Variables.GetBRegisterForIntegerType(leftExprType);

        Pop($"{bRegister} ; Binary expression"); // Pop the second expression
        Pop(aRegister); // Pop the first expression
        if (type == NodeBinaryExpressionType.Add) output += $"    add {aRegister}, {bRegister}\n";
        if (type == NodeBinaryExpressionType.Subtract) output += $"    sub {aRegister}, {bRegister}\n";
        if (Variables.IsSignedInteger(leftExprType))
        {
            if (type == NodeBinaryExpressionType.Multiply) output += $"    imul {bRegister}\n";
            if (type == NodeBinaryExpressionType.Divide) output += $"    idiv {bRegister}\n";
        }
        else
        {
            if (type == NodeBinaryExpressionType.Multiply) output += $"    mul {bRegister}\n";
            if (type == NodeBinaryExpressionType.Divide) output += $"    div {bRegister}\n";
        }
        Push(aRegister);

        return leftExprType;
    }

    private void GenerateStatement(NodeStatement statement)
    {
        switch (statement.Type)
        {
            case NodeStatementType.Exit:
                var exitExprType = GenerateExpression(statement.Exit.ReturnCodeExpression, VariableType.Byte);

                if (exitExprType != VariableType.Byte)
                {
                    Console.Error.WriteLine($"Invalid expression type on exit. Expected Byte and got {exitExprType}.");
                    Environment.Exit(1);
                }

                output += "    mov rax, 60 ; exit\n";
                Pop("rdi"); // Retrieve literal from the top of the stack
                output += "    syscall\n";
                break;

            case NodeStatementType.Variable:
                string identifier = statement.Variable.Identifier.Value!;

                if (DoesVariableExist(identifier))
                {
                    Console.Error.WriteLine($"Variable '{identifier}' is already in use.");
                    Environment.Exit(1);
                }

                var variableType = statement.Variable.Type;

                output += $"    ; Define {identifier} variable of type {variableType}\n";

                var expressionType = GenerateExpression(statement.Variable.ValueExpression, variableType);
                var variableARegister = Variables.GetARegisterForIntegerType(expressionType);

                if (variableType != expressionType)
                {
                    Console.Error.WriteLine($"Type mismatch on variable statement. {variableType} != {expressionType}");
                    Environment.Exit(1);
                }

                ulong relativePosition = workingScope.DefineVariable(identifier, variableType);

                if (variableARegister[0] == 'e') // I hate assembly
                    variableARegister = "r" + variableARegister[1..];

                Pop(variableARegister);
                output += $"    mov [rbp - {relativePosition}], {variableARegister}\n";
                break;

            case NodeStatementType.Assign:
                string assignIdentifier = statement.Assign.Identifier.Value!;

                var variable = GetVariable(assignIdentifier);

                if (!variable.HasValue)
                {
                    Console.Error.WriteLine($"Variable '{assignIdentifier}' has not been declared yet.");
                    Environment.Exit(1);
                }

                output += $"    ; Assign {assignIdentifier}";
                var assignExprType = GenerateExpression(statement.Assign.ValueExpression, variable!.Value.Type);

                if (variable!.Value.Type != assignExprType)
                {
                    Console.Error.WriteLine($"Type mismatch on variable assignment. {assignIdentifier} ({variable!.Value.Type}) != {assignExprType}");
                    Environment.Exit(1);
                }

                long variablePosition = GetVariablePositionOnStackRelativeToWorkingScope(assignIdentifier);
                var assemblyAssignString = CastVariablePositionRelativeToWorkingSpaceToAssemblyString(variablePosition);

                Pop($"{assemblyAssignString}");
                break;

            case NodeStatementType.Scope:
                GenerateScope(statement.Scope);
                break;

            case NodeStatementType.If: // TODO: Fix register usage later
                var ifStatement = statement.If;

                var finalLabelIndex = labelCount + ifStatement.Elifs.Count + (ifStatement.Else.HasValue ? 1 : 0);

                GenerateExpression(ifStatement.This.Expression, VariableType.SignedInteger64);
                output += "    xor rax, rax ; Clear out rax for if statement";
                Pop("rax");
                output += $"    cmp rax, 0\n";
                output += $"    je label{labelCount}\n";
                GenerateScope(ifStatement.This.Scope);
                output += $"    jmp label{finalLabelIndex}\n";

                for (int i = 0; i < ifStatement.Elifs.Count; i++)
                {
                    var elifStatement = ifStatement.Elifs[i];

                    output += $"label{labelCount}:\n"; labelCount++;
                    GenerateExpression(elifStatement.Expression, VariableType.SignedInteger64);
                    Pop("rax");
                    output += $"    cmp rax, 0\n";
                    output += $"    je label{labelCount}\n";
                    GenerateScope(elifStatement.Scope);
                    output += $"    jmp label{finalLabelIndex}\n";
                }

                if (ifStatement.Else.HasValue)
                {
                    output += $"label{labelCount}:\n"; labelCount++;
                    GenerateScope(ifStatement.Else.Value);
                }

                output += $"label{finalLabelIndex}:\n";

                break;

            default:
                throw new InvalidProgramException("Reached unreachable state on GenerateStatement().");
        }
    }

    private void GenerateScope(NodeScope scope)
    {
        BeginNewWorkingScope();
        foreach (var statement in scope.Statements)
        {
            GenerateStatement(statement);
        }
        EndWorkingScope();
    }

    public string GenerateProgram()
    {
        output = "section .text\n    global _start\n\n_start:\n";

        BeginNewWorkingScope();

        foreach (var statement in program.Statements)
        {
            GenerateStatement(statement);
        }

        EndWorkingScope();

        output += "    mov rax, 60 ; End of program\n";
        output += "    xor rdi, rdi\n";
        output += "    syscall\n";

        return output;
    }

    private void BeginNewWorkingScope()
    {
        output += "    ; Begin new scope\n";
        output += "    push rbp ; Previous base stack pointer\n";
        output += "    mov rbp, rsp\n";
        output += "    sub rsp, 128 ; Allocate 128 bytes for scope\n";

        var scope = new Scope
        {
            Parent = workingScope
        };

        workingScope = scope;
    }

    private void EndWorkingScope()
    {
        output += "    leave ; Revert stack pointers\n";
        output += "    ; End of scope\n";

        workingScope = workingScope.Parent;
    }

    private string CastVariablePositionRelativeToWorkingSpaceToAssemblyString(long position)
    {
        if (position < 0)
            return $"rbp + {-position}";
        else if (position > 0)
            return $"rbp - {position}";
        else
            return "rbp";
    }

    private long GetVariablePositionOnStackRelativeToWorkingScope(string variableName)
    {
        long stackDifference = 0;

        Scope currentScope = workingScope;

        while (currentScope != null)
        {
            if (currentScope.variables.ContainsKey(variableName))
            {
                stackDifference += (long)currentScope.variables.GetValueByKey(variableName).BaseStackDifference;
                return stackDifference;
            }

            stackDifference -= (long)currentScope.CurrentStackSize;
            currentScope = currentScope.Parent;
        }

        throw new VariableNotFoundException(variableName);
    }

    private Variable? GetVariable(string variableName)
    {
        Scope currentScope = workingScope;

        while (currentScope != null)
        {
            if (currentScope.variables.ContainsKey(variableName))
            {
                return currentScope.variables.GetValueByKey(variableName);
            }

            currentScope = currentScope.Parent;
        }

        return null;
    }

    private bool DoesVariableExist(string variableName)
    {
        Scope currentScope = workingScope;

        while (currentScope != null)
        {
            if (currentScope.variables.ContainsKey(variableName))
            {
                return true;
            }

            currentScope = currentScope.Parent;
        }

        return false;
    }

    public void Push(string register)
    {
        output += $"    push {Force64BitRegister(register)}\n";
    }

    public void Pop(string register)
    {
        output += $"    pop {Force64BitRegister(register)}\n";
    }

    private string Force64BitRegister(string register) // I hate assembly with a passion
    {
        if (!register.Contains(' '))
            return Force64BitRegisterLone(register);

        var firstHalfIndex = register.IndexOf(' ');
        var workingRegister = register[..firstHalfIndex];

        if (workingRegister.Length > 3)
            return register; // Don't touch the special ones

        workingRegister = Force64BitRegisterLone(workingRegister);

        return workingRegister + register[firstHalfIndex..];
    }

    private string Force64BitRegisterLone(string register) // I hate assembly with a passion
    {
        if (register.Contains('a'))
            register = "rax";
        else if (register.Contains('b'))
            register = "rbx";

        return register;
    }
}
