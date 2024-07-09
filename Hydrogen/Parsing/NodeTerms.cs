using Hydrogen.Tokenization;

namespace Hydrogen.Parsing;

public struct NodeTermInteger : NodeTerm
{
    public Token Int_Lit;
}

public struct NodeTermIdentifier : NodeTerm
{
    public Token Identifier;
}

public struct NodeTermPointerAddress : NodeTerm
{
    public NodeTermIdentifier Identifier;
}

public struct NodeTermPointerValue : NodeTerm
{
    public NodeTermIdentifier Identifier;
}

public struct NodeTermParen : NodeTerm
{
    public NodeExpression Expression;
}

public struct NodeTermChar : NodeTerm
{
    public Token Char;
}

public interface NodeTerm : NodeExpression
{
}