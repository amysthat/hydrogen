using Hydrogen.Tokenization;

namespace Hydrogen.Parsing;

public struct NodeTermInteger : NodeTerm, BinaryExprSupporter
{
    public int LineNumber { get; set; }

    public Token IntegerLiteral;
}

public struct NodeTermIdentifier : NodeTerm, BinaryExprSupporter
{
    public int LineNumber { get; set; }

    public Token Identifier;
}

public struct NodeTermPointerAddress : NodeTerm, BinaryExprSupporter
{
    public int LineNumber { get; set; }

    public NodeTermIdentifier Identifier;
}

public struct NodeTermPointerValue : NodeTerm, BinaryExprSupporter
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

public struct NodeTermBool : NodeTerm
{
    public int LineNumber { get; set; }

    public bool Value;
}

public interface NodeTerm : NodeExpression
{
}