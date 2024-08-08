using Hydrogen.Tokenization;

namespace Hydrogen.Parsing;

public interface NodeTerm : NodeExpression
{
}

public struct NodeTermInteger : NodeTerm, BinaryExprSupporter
{
    public int LineNumber { get; set; }

    public Token IntegerLiteral;
}

public struct NodeTermIdentifier : NodeTerm, BinaryExprSupporter, LogicalExprSupporter
{
    public int LineNumber { get; set; }

    public Token Identifier;
}

public struct NodeTermPointerAddress : NodeTerm, BinaryExprSupporter
{
    public int LineNumber { get; set; }

    public NodeTermIdentifier Identifier;
}

public struct NodeTermPointerValue : NodeTerm, BinaryExprSupporter, LogicalExprSupporter
{
    public int LineNumber { get; set; }

    public NodeTermIdentifier Identifier;
}

public struct NodeTermChar : NodeTerm
{
    public int LineNumber { get; set; }

    public Token Char;
}
public struct NodeTermString : NodeTerm
{
    public int LineNumber { get; set; }

    public Token String;
}

public struct NodeTermBool : NodeTerm, LogicalExprSupporter
{
    public int LineNumber { get; set; }

    public bool Value;
}
