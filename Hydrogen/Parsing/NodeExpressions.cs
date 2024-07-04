using Hydrogen.Generation;

namespace Hydrogen.Parsing;

public struct NodeExprCast : NodeExpression
{
    public VariableType CastType;
    public NodeExpression Expression;
}
public interface NodeExpression
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
    public NodeBinExprType Type;

    public NodeExpression Left;
    public NodeExpression Right;
}