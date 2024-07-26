namespace Hydrogen.Tokenization;

public enum TokenType
{
    Exit,
    Write, // TODO: Export to proper method

    If,
    Elif,
    Else,

    Int_Lit,

    Semicolon,

    OpenParenthesis,
    CloseParenthesis,
    OpenCurlyBraces,
    CloseCurlyBraces,

    Identifier,

    VariableHint,

    Equals,
    Plus,
    Star,
    Minus,
    Slash,

    Cast,
    VariableType,

    VarToPtr,

    Char,
    String,

    Bool,
}