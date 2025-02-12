﻿using Hydrogen.Parsing;
using Hydrogen.Generation.Variables;

namespace Hydrogen.Generation;

public class Generator(NodeProgram program)
{
    public NodeProgram program = program;
    public string output = string.Empty;
    public int labelCount;
    public int finalIfLabelCount;
    public Scope workingScope = null!;
    public bool performPushPullOptimization;

    public int dataCount;
    public List<string> dataSection = [];

    public VariableType GenerateTerm(NodeTerm term, VariableType suggestionType)
    {
        if (term is NodeTermInteger termInteger)
            return Terms.Integer(this, termInteger, suggestionType);

        if (term is NodeTermIdentifier termIdentifier)
            return Terms.Identifier(this, termIdentifier);

        if (term is NodeTermPointerAddress termPointerAdress)
            return Terms.PointerAddress(this, termPointerAdress);

        if (term is NodeTermPointerValue termPointerValue)
            return Terms.PointerValue(this, termPointerValue);

        // if (term is NodeTermParen termParenthesis)
        //     return GenerateExpression(termParenthesis.Expression, suggestionType);

        if (term is NodeTermChar termChar)
            return Terms.Char(this, termChar);

        if (term is NodeTermString termString)
            return Terms.String(this, termString);

        if (term is NodeTermBool termBool)
            return Terms.Bool(this, termBool);

        throw new InvalidProgramException($"Reached unreachable state on {nameof(GenerateTerm)}().");
    }

    public VariableType GenerateExpression(NodeExpression expression, VariableType suggestionType)
    {
        if (expression is NodeExprCast castExpression)
            return Expressions.Cast(this, castExpression);

        if (expression is NodeTerm term)
            return GenerateTerm(term!, suggestionType);

        if (expression is NodeBinExpr binaryExpression)
            return Expressions.BinaryExpression(this, binaryExpression, suggestionType);

        if (expression is NodeLogicalExpr logicalExpression)
            return GenerateLogicalExpression(logicalExpression);

        throw new InvalidProgramException("Reached unreachable state on GenerateExpression().");
    }

    public VariableType GenerateBinaryExpression(BinaryExprSupporter binExprSupporter, VariableType suggestionType)
    {
        if (binExprSupporter is NodeBinExpr binaryExpression)
            return Expressions.BinaryExpression(this, binaryExpression, suggestionType);

        if (binExprSupporter is NodeTermInteger termInteger)
            return Terms.Integer(this, termInteger, suggestionType);

        if (binExprSupporter is NodeTermIdentifier termIdentifier)
            return Terms.Identifier(this, termIdentifier);

        if (binExprSupporter is NodeTermPointerAddress termPointerAdress)
            return Terms.PointerAddress(this, termPointerAdress);

        if (binExprSupporter is NodeTermPointerValue termPointerValue)
            return Terms.PointerValue(this, termPointerValue);

        throw new InvalidProgramException($"Reached unreachable state on {nameof(GenerateBinaryExpression)}().");
    }

    public VariableType GenerateLogicalExpression(LogicalExprSupporter logicalExprSupporter)
    {
        if (logicalExprSupporter is NodeTermBool termBool)
            return Terms.Bool(this, termBool);

        if (logicalExprSupporter is NodeTermIdentifier termIdentifier)
        {
            var variableType = Terms.Identifier(this, termIdentifier);

            if (variableType != VariableTypes.Bool)
                throw new CompilationException(termIdentifier.LineNumber, $"{termIdentifier.Identifier.Value} ({variableType}) is not bool for logical expression.");

            return VariableTypes.Bool;
        }

        if (logicalExprSupporter is NodeTermPointerValue termPtrValue)
        {
            var variableType = Terms.PointerValue(this, termPtrValue);

            if (variableType != VariableTypes.Bool)
                throw new CompilationException(termPtrValue.LineNumber, $"*{termPtrValue.Identifier.Identifier.Value} is not bool for logical expression.");

            return VariableTypes.Bool;
        }

        if (logicalExprSupporter is NodeLogicNotExpr notExpr)
            return Expressions.Logic.Not(this, notExpr);

        throw new InvalidProgramException($"Reached unreachable state on {nameof(GenerateLogicalExpression)}().");
    }

    private void GenerateStatement(NodeStatement statement)
    {
        if (statement is NodeStmtExit exitStatement)
        {
            Statements.Exit(this, exitStatement);
            output += "\n";
            return;
        }

        if (statement is NodeStmtWrite writeStatement)
        {
            Statements.Write(this, writeStatement);
            output += "\n";
            return;
        }

        if (statement is NodeStmtVariable variableStatement)
        {
            Statements.VariableStatement(this, variableStatement);
            output += "\n";
            return;
        }

        if (statement is NodeStmtAssign assignStatement)
        {
            Statements.VariableAssignment(this, assignStatement);
            output += "\n";
            return;
        }

        if (statement is NodeScope statementScope)
        {
            GenerateScope(statementScope.Statements);
            output += "\n";
            return;
        }

        if (statement is NodeStmtIf ifStatement)
        {
            Statements.If(this, ifStatement);
            output += "\n";
            return;
        }

        throw new InvalidProgramException("Reached unreachable state on GenerateStatement().");
    }

    public string GenerateProgram()
    {
        output = "section .text\n    global _start\n\n_start:\n";

        GenerateScope(program.Statements);

        output += "    mov rax, 60 ; End of program\n";
        output += "    xor rdi, rdi\n";
        output += "    syscall\n";

        if (dataSection.Count > 0)
        {
            var dataSectionOutput = "section .data\n";

            foreach (var data in dataSection)
            {
                dataSectionOutput += data + "\n";
            }

            dataSectionOutput += "\n";

            output = dataSectionOutput + output;
        }

        if (performPushPullOptimization)
            return OptimizeHorribly(output);
        else
            return output;
    }

    public void Asm(string instruction)
    {
        output += "    ";
        output += instruction;
        output += "\n";
    }

    #region Scopes
    public void GenerateScope(List<NodeStatement> statements)
    {
        long scopeSize = 0;

        foreach (var statement in statements)
        {
            scopeSize += Statements.GetSize(statement);
        }

        BeginNewWorkingScope(scopeSize);
        foreach (var statement in statements)
        {
            GenerateStatement(statement);
        }
        EndWorkingScope();
    }

    private void BeginNewWorkingScope(long scopeSize)
    {
        output += "    ; Begin new scope\n";
        output += "    push rbp ; Set up stack pointers\n";
        output += "    mov rbp, rsp\n";
        output += $"    sub rsp, {scopeSize}\n";
        output += "\n";

        var scope = new Scope
        {
            Parent = workingScope
        };

        workingScope = scope;
    }

    private void EndWorkingScope()
    {
        output += $"    add rsp, {workingScope.CurrentStackSize} ; Revert stack dedication\n";
        output += "    leave ; Revert stack pointers\n";
        output += "    ; End of scope\n";
        output += "\n";

        workingScope = workingScope.Parent;
    }
    #endregion

    #region Variable Positioning
    public static string CastRelativeVariablePositionToAssembly(long position)
    {
        if (position < 0)
            return $"rbp + {-position}";
        else if (position > 0)
            return $"rbp - {position}";
        else
            return "rbp";
    }

    public long GetRelativeVariablePosition(string variableName)
    {
        if (workingScope.variables.ContainsKey(variableName))
        {
            var variable = workingScope.variables.GetValueByKey(variableName);

            return variable.RelativePosition + variable.Size;
        }

        long stackDifference = 0;

        Scope currentScope = workingScope;

        while (currentScope != null)
        {
            if (currentScope.variables.ContainsKey(variableName))
            {
                var variable = currentScope.variables.GetValueByKey(variableName);

                return stackDifference + variable.RelativePosition + variable.Size;
            }

            stackDifference -= 8; // rbp is pushed
            stackDifference -= currentScope.Parent.CurrentStackSize;
            currentScope = currentScope.Parent;
        }

        throw new VariableNotFoundException(variableName);
    }

    public Variable? GetVariable(string variableName)
    {
        Scope currentScope = workingScope;

        while (currentScope != null)
        {
            if (currentScope.variables.ContainsKey(variableName))
            {
                return currentScope.variables.GetValueByKey(variableName)!;
            }

            currentScope = currentScope.Parent;
        }

        return null;
    }

    public bool DoesVariableExist(string variableName)
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
    private static string Force64BitRegister(string register)
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

    private static string Force64BitRegisterLone(string register)
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

    private static string OptimizeHorribly(string output) => output.Replace("    push rax\n    pop rax\n", ""); // lol
}
