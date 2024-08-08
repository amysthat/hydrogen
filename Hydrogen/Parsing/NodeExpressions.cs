using Hydrogen.Generation.Variables;

namespace Hydrogen.Parsing;

public struct NodeExprCast : NodeExpression
{
    public int LineNumber { get; set; }

    public VariableType CastType;
    public NodeTerm Term;
}

public interface NodeExpression : Node
{
}

// Binary Expression

public enum NodeBinExprType
{
    Add,
    Subtract,
    Multiply,
    Divide,
}
public struct NodeBinExpr : NodeExpression, BinaryExprSupporter
{
    public int LineNumber { get; set; }

    public NodeBinExprType Type;

    public BinaryExprSupporter Left;
    public BinaryExprSupporter Right;
}

public interface BinaryExprSupporter
{
}

// Logical Expression

public interface NodeLogicalExpr : NodeExpression, LogicalExprSupporter
{
}

public interface LogicalExprSupporter
{
}

public struct NodeLogicNotExpr : NodeLogicalExpr
{
    public int LineNumber { get; set; }

    public LogicalExprSupporter InnerExpression;
}
