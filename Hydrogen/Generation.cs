﻿using Hydrogen.Parsing;

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
        if (term is NodeTermInteger termInteger)
        {
            var integerType = Variables.IsInteger(suggestionType) ? suggestionType : VariableType.SignedInteger64;

            if (!Variables.IsSignedInteger(integerType) && termInteger.Int_Lit.Value!.StartsWith('-'))
            {
                Console.Error.WriteLine("Negative value given for unsigned integer.");
                Environment.Exit(1);
            }

            Push(Variables.MoveIntegerToRegister(this, termInteger, integerType));
            return integerType;
        }

        if (term is NodeTermIdentifier termIdentifier)
        {
            string identifier = termIdentifier.Identifier.Value!;

            var variable = GetVariable(identifier);

            if (!variable.HasValue)
            {
                Console.Error.WriteLine($"Variable '{identifier}' has not been declared.");
                Environment.Exit(1);
            }

            var variablePosition = GetRelativeVariablePosition(identifier);
            var assemblyString = CastRelativeVariablePositionToAssembly(variablePosition);

            var aRegister = Variables.GetARegisterForIntegerType(variable!.Value.Type);
            var asmPointerSize = Variables.GetAsmPointerSizeForIntegerType(variable!.Value.Type);

            output += $"    mov {aRegister}, {asmPointerSize} [{assemblyString}] ; {variable!.Value.Type} {identifier} variable\n";
            Push("rax");

            return variable!.Value.Type;
        }

        if (term is NodeTermParen termParenthesis)
            return GenerateExpression(termParenthesis.Expression, suggestionType);

        throw new InvalidProgramException("Reached unreachable state on GenerateTerm().");
    }

    private VariableType GenerateExpression(NodeExpression expression, VariableType suggestionType)
    {
        if (expression is NodeTerm term)
            return GenerateTerm(term!, suggestionType);

        if (expression is NodeBinaryExpression binaryExpression)
            return GenerateBinaryExpression(binaryExpression, suggestionType);

        if (expression is NodeExprCast castExpression)
        {
            var targetType = castExpression.CastType;

            var expressionType = GenerateExpression(castExpression, targetType);

            if (targetType == expressionType)
            {
                Console.WriteLine($"Warning: Redundant cast of {targetType}.");
            }

            Variables.Cast(this, expressionType, targetType);
            return targetType;
        }

        throw new InvalidProgramException("Reached unreachable state on GenerateExpression().");
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
        if (statement is NodeStmtExit exitStatement)
        {
            var exitExprType = GenerateExpression(exitStatement.ReturnCodeExpression, VariableType.Byte);

            if (exitExprType != VariableType.Byte)
            {
                Console.Error.WriteLine($"Invalid expression type on exit. Expected Byte and got {exitExprType}.");
                Environment.Exit(1);
            }

            output += "    mov rax, 60 ; exit\n";
            Pop("rdi"); // Retrieve literal from the top of the stack
            output += "    syscall\n";
            return;
        } 

        if (statement is NodeStmtVar variableStatement)
        {
            string identifier = variableStatement.Identifier.Value!;

            if (DoesVariableExist(identifier))
            {
                Console.Error.WriteLine($"Variable '{identifier}' is already in use.");
                Environment.Exit(1);
            }

            var variableType = variableStatement.Type;

            output += $"    ; Define {identifier} variable of type {variableType}\n";

            var expressionType = GenerateExpression(variableStatement.ValueExpression, variableType);
            var variableARegister = Variables.GetARegisterForIntegerType(expressionType);

            if (variableType != expressionType)
            {
                Console.Error.WriteLine($"Type mismatch on variable statement. {variableType} != {expressionType}");
                Environment.Exit(1);
            }

            ulong relativePosition = workingScope.DefineVariable(identifier, variableType);

            Pop(variableARegister);
            output += $"    mov [rbp - {relativePosition}], {variableARegister}\n";
            return;
        }
        
        if (statement is NodeStmtAssign assignStatement)
        {
            string assignIdentifier = assignStatement.Identifier.Value!;

            var variable = GetVariable(assignIdentifier);

            if (!variable.HasValue)
            {
                Console.Error.WriteLine($"Variable '{assignIdentifier}' has not been declared yet.");
                Environment.Exit(1);
            }

            output += $"    ; Assign {assignIdentifier}\n";
            var assignExprType = GenerateExpression(assignStatement.ValueExpression, variable!.Value.Type);

            if (variable!.Value.Type != assignExprType)
            {
                Console.Error.WriteLine($"Type mismatch on variable assignment. {assignIdentifier} ({variable!.Value.Type}) != {assignExprType}");
                Environment.Exit(1);
            }

            var assignARegister = Variables.GetARegisterForIntegerType(assignExprType);
            long variablePosition = GetRelativeVariablePosition(assignIdentifier);
            var assemblyAssignString = CastRelativeVariablePositionToAssembly(variablePosition);

            Pop($"{assignARegister}");
            output += $"    mov [{assemblyAssignString}], {assignARegister}\n";
            return;
        }
        
        if (statement is NodeScope statementScope)
        {
            GenerateScope(statementScope);
            return;
        }

        if (statement is NodeStmtIf ifStatement) // TODO: Fix register usage later
        {
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
            return;
        }

        throw new InvalidProgramException("Reached unreachable state on GenerateStatement().");
    }

    private static ulong GetStatementSize(NodeStatement nodeStatement)
    {
        if (nodeStatement is NodeStmtVar variableStatement)
        {
            return Variables.GetSize(variableStatement.Type);
        }

        return 0;
    }

    private void GenerateScope(NodeScope scope)
    {
        ulong scopeSize = 0;
        foreach (var statement in scope.Statements)
        {
            scopeSize += GetStatementSize(statement);
        }

        BeginNewWorkingScope(scopeSize);
        foreach (var statement in scope.Statements)
        {
            GenerateStatement(statement);
        }
        EndWorkingScope();
    }

    public string GenerateProgram()
    {
        output = "section .text\n    global _start\n\n_start:\n";

        ulong scopeSize = 0;
        foreach (var statement in program.Statements)
        {
            scopeSize += GetStatementSize(statement);
        }

        BeginNewWorkingScope(scopeSize);

        foreach (var statement in program.Statements)
        {
            GenerateStatement(statement);
        }

        EndWorkingScope();

        output += "    mov rax, 60 ; End of program\n";
        output += "    xor rdi, rdi\n";
        output += "    syscall\n";

        return OptimizeHorribly(output);
    }

    private void BeginNewWorkingScope(ulong scopeSize)
    {
        output += "    ; Begin new scope\n";
        output += "    push rbp ; Previous base stack pointer\n";
        output += "    mov rbp, rsp\n";
        output += $"    sub rsp, {scopeSize} ; Allocate {scopeSize} bytes for scope\n";

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

    #region Variable Positioning
    private static string CastRelativeVariablePositionToAssembly(long position)
    {
        if (position < 0)
            return $"rbp + {-position}";
        else if (position > 0)
            return $"rbp - {position}";
        else
            return "rbp";
    }

    private long GetRelativeVariablePosition(string variableName) // TODO: Not working
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

            stackDifference -= 8; // rbp is pushed
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
    #endregion

    public void Push(string register)
    {
        output += $"    push {Force64BitRegister(register)}\n";
    }

    public void Pop(string register)
    {
        output += $"    pop {Force64BitRegister(register)}\n";
    }

    #region I hate assembly with a passion
    private string Force64BitRegister(string register)
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

    private string Force64BitRegisterLone(string register)
    {
        if (register == "rsp" || register == "rbp")
            return register;

        if (register.Contains('a'))
            register = "rax";
        else if (register.Contains('b'))
            register = "rbx";

        return register;
    }
    #endregion

    private string OptimizeHorribly(string output) => output.Replace("    push rax\n    pop rax\n", ""); // lol
}
