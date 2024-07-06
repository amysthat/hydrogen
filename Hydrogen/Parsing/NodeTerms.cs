using Hydrogen.Tokenization;

namespace Hydrogen.Parsing;

public struct NodeTermInteger : NodeTerm
{
    public Token Int_Lit;
}
public struct NodeTermIdentifier : NodeTerm
{
    public bool VarToPtr;
    public Token Identifier;
}
public struct NodeTermParen : NodeTerm
{
    public NodeExpression Expression;
}
public interface NodeTerm : NodeExpression
{
}