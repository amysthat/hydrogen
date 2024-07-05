using Hydrogen.Parsing;
using Hydrogen.Generation.Variables;

namespace Hydrogen.Generation;

public class Generator(NodeProgram program)
{
    public NodeProgram program = program;
    public string output = string.Empty;
    public int labelCount;
    public Scope workingScope = null!;

    public VariableType GenerateTerm(NodeTerm term, VariableType suggestionType)
    {
        if (term is NodeTermInteger termInteger)
            return Terms.Integer(this, termInteger, suggestionType);

        if (term is NodeTermIdentifier termIdentifier)
            return Terms.Identifier(this, termIdentifier);

        if (term is NodeTermParen termParenthesis)
            return GenerateExpression(termParenthesis.Expression, suggestionType);

        throw new InvalidProgramException("Reached unreachable state on GenerateTerm().");
    }

    public VariableType GenerateExpression(NodeExpression expression, VariableType suggestionType)
    {
        if (expression is NodeTerm term)
            return GenerateTerm(term!, suggestionType);

        if (expression is NodeBinExpr binaryExpression)
            return Expressions.BinaryExpression(this, binaryExpression, suggestionType);

        if (expression is NodeExprCast castExpression)
            return Expressions.Cast(this, castExpression);

        throw new InvalidProgramException("Reached unreachable state on GenerateExpression().");
    }

    private void GenerateStatement(NodeStatement statement)
    {
        if (statement is NodeStmtExit exitStatement)
        {
            Statements.ExitStatement(this, exitStatement);
            return;
        }

        if (statement is NodeStmtVariable variableStatement)
        {
            Statements.VariableStatement(this, variableStatement);
            return;
        }

        if (statement is NodeStmtAssign assignStatement)
        {
            Statements.VariableAssignmentStatement(this, assignStatement);
            return;
        }

        if (statement is NodeScope statementScope)
        {
            GenerateScope(statementScope);
            return;
        }

        if (statement is NodeStmtIf ifStatement)
        {
            Statements.IfStatement(this, ifStatement);
            return;
        }

        throw new InvalidProgramException("Reached unreachable state on GenerateStatement().");
    }

    public string GenerateProgram()
    {
        output = "section .text\n    global _start\n\n_start:\n";

        long scopeSize = 0;
        foreach (var statement in program.Statements)
        {
            scopeSize += Statements.GetSize(statement);
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

    #region Scopes
    public void GenerateScope(NodeScope scope)
    {
        long scopeSize = 0;
        foreach (var statement in scope.Statements)
        {
            scopeSize += Statements.GetSize(statement);
        }

        BeginNewWorkingScope(scopeSize);
        foreach (var statement in scope.Statements)
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
        output += $"    sub rsp, {scopeSize} ; Allocate {scopeSize} bytes for scope\n";

        var scope = new Scope
        {
            Parent = workingScope
        };

        workingScope = scope;
    }

    private void EndWorkingScope()
    {
        output += "    mov rsp, rbp ; Revert stack pointers\n";
        output += "    pop rbp\n";
        output += "    ; End of scope\n";

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

    public long GetRelativeVariablePosition(string variableName) // TODO: Not working
    {
        long stackDifference = 0;

        Scope currentScope = workingScope;

        while (currentScope != null)
        {
            if (currentScope.variables.ContainsKey(variableName))
            {
                stackDifference += currentScope.variables.GetValueByKey(variableName).BaseStackDifference;
                return stackDifference;
            }

            stackDifference -= 8; // rbp is pushed
            stackDifference -= currentScope.CurrentStackSize;
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
