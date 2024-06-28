using Hydrogen.Parsing;

namespace Hydrogen.Generation;

public class Generator
{
    private NodeProgram program;
    private string output;
    private ulong StackSize;
    private int labelCount;
    private readonly Map<string, Variable> variables;
    private readonly List<int> scopes;

    public struct Variable
    {
        public ulong StackLocation;
    }

    public Generator(NodeProgram program)
    {
        this.program = program;
        output = string.Empty;
        StackSize = 0;
        variables = new Map<string, Variable>();
        scopes = [];
    }

    public void GenerateTerm(NodeTerm term)
    {
        switch (term.Type)
        {
            case NodeTermType.IntLit:
                output += "    mov rax, " + term.IntLit.Int_Lit.Value + " ; IntLit expression\n";
                Push("rax"); // Push the literal to the top of the stack
                break;

            case NodeTermType.Identifier:
                string identifier = term.Identifier.Identifier.Value!;

                if (!variables.ContainsKey(identifier))
                {
                    Console.Error.WriteLine($"Variable '{identifier}' has not been declared.");
                    Environment.Exit(1);
                }

                var variable = variables.GetValueByKey(identifier);

                var lastStackPosition = StackSize - 1;

                Push($"QWORD [rsp + {(lastStackPosition - variable.StackLocation) * 8}] ; {identifier} variable");
                break;

            case NodeTermType.Parenthesis:
                GenerateExpression(term.Parenthesis.Expression);
                break;

            default:
                throw new InvalidProgramException("Reached unreachable state on GenerateTerm().");
        }
    }

    public void GenerateExpression(NodeExpression expression)
    {
        switch (expression.Type)
        {
            case NodeExpressionType.Term:
                var term = expression.Term;

                GenerateTerm(term);
                break;

            case NodeExpressionType.BinaryExpression:
                var binaryExpression = expression.BinaryExpression;
                var type = binaryExpression.Type;

                GenerateExpression(binaryExpression.Left);
                GenerateExpression(binaryExpression.Right);

                Pop("rbx ; Binary expression"); // Pop the second expression
                Pop("rax"); // Pop the first expression
                if (type == NodeBinaryExpressionType.Add) output += "    add rax, rbx\n";
                if (type == NodeBinaryExpressionType.Subtract) output += "    sub rax, rbx\n";
                if (type == NodeBinaryExpressionType.Multiply) output += "    mul rbx\n";
                if (type == NodeBinaryExpressionType.Divide) output += "    div rbx\n";
                Push("rax"); // Push the output
                break;

            default:
                throw new InvalidProgramException("Reached unreachable state on GenerateExpression().");
        }
    }

    public void GenerateStatement(NodeStatement statement)
    {
        switch (statement.Type)
        {
            case NodeStatementType.Exit:
                GenerateExpression(statement.Exit.ReturnCodeExpression);
                output += "    mov rax, 60 ; exit\n";
                Pop("rdi"); // Retrieve literal from the top of the stack
                output += "    syscall\n";
                break;

            case NodeStatementType.Variable:
                string identifier = statement.Variable.Identifier.Value!;

                if (variables.ContainsKey(identifier))
                {
                    Console.Error.WriteLine($"Variable '{identifier}' is already in use.");
                    Environment.Exit(1);
                }

                output += $"    ; Define {identifier} variable\n";

                GenerateExpression(statement.Variable.ValueExpression);

                variables.Add(identifier, new Variable
                {
                    StackLocation = StackSize - 1 // Already pushed it with GenerateExpression and therefore -1
                });
                break;

            case NodeStatementType.Assign:
                string assignIdentifier = statement.Assign.Identifier.Value!;

                if (!variables.ContainsKey(assignIdentifier))
                {
                    Console.Error.WriteLine($"Variable '{assignIdentifier}' has not been declared yet.");
                    Environment.Exit(1);
                }

                var variable = variables.GetValueByKey(assignIdentifier);

                var lastStackPosition = StackSize - 1;

                output += $"    ; Assign {assignIdentifier}\n";

                GenerateExpression(statement.Assign.ValueExpression);
                Pop("rax");
                output += $"    mov QWORD [rsp + {(lastStackPosition - variable.StackLocation) * 8}], rax\n";
                break;

            case NodeStatementType.Scope:
                GenerateScope(statement.Scope);
                break;

            case NodeStatementType.If:
                var ifStatement = statement.If;

                var finalLabelIndex = labelCount + ifStatement.Elifs.Count + (ifStatement.Else.HasValue ? 1 : 0);

                GenerateExpression(ifStatement.This.Expression);
                Pop("rax");
                output += $"    cmp rax, 0\n";
                output += $"    je label{labelCount}\n";
                GenerateScope(ifStatement.This.Scope);
                output += $"    jmp label{finalLabelIndex}\n";

                for (int i = 0; i < ifStatement.Elifs.Count; i++)
                {
                    var elifStatement = ifStatement.Elifs[i];

                    output += $"label{labelCount}:\n"; labelCount++;
                    GenerateExpression(elifStatement.Expression);
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

    public void GenerateScope(NodeScope scope)
    {
        BeginScope();
        foreach (var statement in scope.Statements)
        {
            GenerateStatement(statement);
        }
        EndScope();
    }

    public string GenerateProgram()
    {
        output = "global _start\n_start:\n";

        foreach (var statement in program.Statements)
        {
            GenerateStatement(statement);
        }

        output += "    mov rax, 60 ; End of program\n";
        output += "    mov rdi, 0\n";
        output += "    syscall\n";

        return output;
    }

    private void BeginScope()
    {
        scopes.Add(variables.Count);

        output += "    ; Start scope\n";
    }

    private void EndScope()
    {
        int pop_count = variables.Count - scopes[^1];

        output += $"    add rsp, {pop_count * 8} ; End scope with {pop_count} variable(s)\n";
        StackSize -= (ulong)pop_count;

        variables.PopBack(pop_count);
        scopes.RemoveAt(scopes.Count - 1);
    }

    private void Push(string register)
    {
        output += $"    push {register}\n";
        StackSize++;
    }

    private void Pop(string register)
    {
        output += $"    pop {register}\n";
        StackSize--;
    }
}
