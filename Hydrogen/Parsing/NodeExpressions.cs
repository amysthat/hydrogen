namespace Hydrogen.Parsing;

public struct NodeExprCast : NodeExpression
{
    public VariableType CastType;
    public NodeExpression Expression;
}
public interface NodeExpression
{
}

public enum NodeBinaryExpressionType
{
    Add,
    Subtract,
    Multiply,
    Divide,
}
public struct NodeBinaryExpression : NodeExpression
{
    public NodeBinaryExpressionType Type;

    public NodeExpression Left;
    public NodeExpression Right;
}