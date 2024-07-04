namespace Hydrogen.Tokenization;

public enum TokenType
{
    Exit,

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
}