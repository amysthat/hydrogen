using Hydrogen.Tokenization;

namespace Hydrogen.Parsing;

public struct NodeTermInteger : NodeTerm
{
    public int LineNumber { get; set; }

    public Token Int_Lit;
}

public struct NodeTermIdentifier : NodeTerm
{
    public int LineNumber { get; set; }

    public Token Identifier;
}

public struct NodeTermPointerAddress : NodeTerm
{
    public int LineNumber { get; set; }

    public NodeTermIdentifier Identifier;
}

public struct NodeTermPointerValue : NodeTerm
{
    public int LineNumber { get; set; }

    public NodeTermIdentifier Identifier;
}

public struct NodeTermParen : NodeTerm
{
    public int LineNumber { get; set; }

    public NodeExpression Expression;
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

public interface NodeTerm : NodeExpression
{
}