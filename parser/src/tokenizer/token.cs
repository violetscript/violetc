namespace VioletScript.Parser.Tokenizer;

public enum Token {
    Eof,
    Identifier,
    StringLiteral,
    NumericLiteral,
    RegExpLiteral,
    Keyword,
    Operator,
    CompoundAssignment,
    LCurly,
    RCurly,
    LParen,
    RParen,
    LSquare,
    RSquare,
    Dot,
    QuestionDot,
    Ellipsis,
    Semicolon,
    Comma,
    QuestionMark,
    ExclamationMark,
    Colon,
    Assign,
    FatArrow,
    LtSlash,
}