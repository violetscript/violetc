namespace VioletScript.Parser.Tokenizer;

using VioletScript.Parser.Operator;
using VioletScript.Parser.Source;

public sealed class TokenData {
    public Script Script;
    public Token Type = Token.Eof;
    public int FirstLine = 1;
    public int LastLine = 1;
    public int Start = 0;
    public int End = 0;
    public string StringValue = "";
    public double NumberValue = 0;
    public string Flags = "";
    public Operator Operator = null;

    public TokenData(Script script) {
        this.Script = script;
    }

    public void CopyTo(TokenData other) {
        other.Type = this.Type;
        other.FirstLine = this.FirstLine;
        other.LastLine = this.LastLine;
        other.Start = this.Start;
        other.End = this.End;
        other.StringValue = this.StringValue;
        other.NumberValue = this.NumberValue;
        other.Flags = this.Flags;
        other.Operator = this.Operator;
    }

    public bool IsKeyword(string name) {
        return Type == Token.Keyword && StringValue == name;
    }

    public bool IsContextKeyword(string name) {
        return Type == Token.Identifier && StringValue == name && RawLength == name.Count();
    }

    public bool IsOperator(Operator opType) {
        return Type == Token.Operator && this.Operator == opType;
    }

    public bool IsCompoundAssignment(Operator opType) {
        return Type == Token.CompoundAssignment && this.Operator == opType;
    }

    public Span Span {
        get => Span.WithLinesAndIndexes(this.Script, FirstLine, LastLine, Start, End);
    }

    public int RawLength {
        get => End - Start;
    }
}