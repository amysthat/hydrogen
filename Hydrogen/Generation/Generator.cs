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
        {
            IntegerType integerType = suggestionType is IntegerType _integerType ? _integerType : VariableTypes.SignedInteger64;

            if (Variable.IsUnsignedInteger(integerType) && termInteger.Int_Lit.Value!.StartsWith('-'))
            {
                Console.Error.WriteLine("Negative value given for unsigned integer.");
                Environment.Exit(1);
            }

            Push(Variable.MoveIntegerToRegister(this, termInteger, integerType));
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

            if (variable.Value.Type is not IntegerType integerType)
            {
                throw new InvalidOperationException();
            }

            var variablePosition = GetRelativeVariablePosition(identifier);
            var assemblyString = CastRelativeVariablePositionToAssembly(variablePosition);

            var aRegister = integerType.AsmARegister;
            var asmPointerSize = integerType.AsmPointerSize;

            output += $"    mov {aRegister}, {asmPointerSize} [{assemblyString}] ; {variable!.Value.Type.Keyword} {identifier} variable\n";
            Push("rax");

            return variable!.Value.Type;
        }

        if (term is NodeTermParen termParenthesis)
            return GenerateExpression(termParenthesis.Expression, suggestionType);

        throw new InvalidProgramException("Reached unreachable state on GenerateTerm().");
    }

    public VariableType GenerateExpression(NodeExpression expression, VariableType suggestionType)
    {
        if (expression is NodeTerm term)
            return GenerateTerm(term!, suggestionType);

        if (expression is NodeBinExpr binaryExpression)
            return GenerateBinaryExpression(binaryExpression, suggestionType);

        if (expression is NodeExprCast castExpression)
        {
            var targetType = castExpression.CastType;

            var expressionType = GenerateExpression(castExpression, targetType);

            if (targetType == expressionType)
            {
                Console.WriteLine($"Warning: Redundant cast of {targetType.Keyword}.");
            }

            Variable.Cast(this, expressionType, targetType);
            return targetType!;
        }

        throw new InvalidProgramException("Reached unreachable state on GenerateExpression().");
    }

    public VariableType GenerateBinaryExpression(NodeBinExpr binaryExpression, VariableType suggestionType)
    {
        var type = binaryExpression.Type;

        var leftExprType = GenerateExpression(binaryExpression.Left, suggestionType);
        var rightExprType = GenerateExpression(binaryExpression.Right, leftExprType);

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

        Pop($"{bRegister} ; Binary expression"); // Pop the second expression
        Pop(aRegister); // Pop the first expression
        if (type == NodeBinExprType.Add) output += $"    add {aRegister}, {bRegister}\n";
        if (type == NodeBinExprType.Subtract) output += $"    sub {aRegister}, {bRegister}\n";
        if ((leftExprType as IntegerType)!.Signedness == IntegerSignedness.SignedInteger)
        {
            if (type == NodeBinExprType.Multiply) output += $"    imul {bRegister}\n";
            if (type == NodeBinExprType.Divide) output += $"    idiv {bRegister}\n";
        }
        else
        {
            if (type == NodeBinExprType.Multiply) output += $"    mul {bRegister}\n";
            if (type == NodeBinExprType.Divide) output += $"    div {bRegister}\n";
        }
        Push(aRegister);

        return leftExprType;
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

    private static long GetStatementSize(NodeStatement nodeStatement)
    {
        if (nodeStatement is NodeStmtVariable variableStatement)
        {
            return Variable.GetSize(variableStatement.Type);
        }

        return 0;
    }

    public string GenerateProgram()
    {
        output = "section .text\n    global _start\n\n_start:\n";

        long scopeSize = 0;
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

    #region Scopes
    public void GenerateScope(NodeScope scope)
    {
        long scopeSize = 0;
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
