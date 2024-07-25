using Hydrogen.Generation.Variables;

namespace Hydrogen.Parsing;

public struct NodeExprCast : NodeExpression
{
    public int LineNumber { get; set; }

    public VariableType CastType;
    public NodeExpression Expression;
}
public interface NodeExpression : Node
{
}

public enum NodeBinExprType
{
    Add,
    Subtract,
    Multiply,
    Divide,
}
public struct NodeBinExpr : NodeExpression
{
    public int LineNumber { get; set; }

    public NodeBinExprType Type;

    public NodeExpression Left;
    public NodeExpression Right;
}