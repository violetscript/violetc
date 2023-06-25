namespace VioletScript.Parser.Tokenizer;

using VioletScript.Parser.Operator;
using VioletScript.Parser.Parser;
using VioletScript.Parser.Diagnostic;
using VioletScript.Parser.Source;
using VioletScript.Util;

using DiagnosticArguments = Dictionary<string, object>;
using TToken = Token;

public sealed class Tokenizer {
    private Script Script;
    public TokenData Token = null;
    private string Content;
    private int ContentLength;
    private int Index = 0;
    public int Line = 1;
    private int SliceStart = 0;

    public Tokenizer(Script script) {
        this.Script = script;
        this.Content = script.Content;
        this.ContentLength = script.Content.Count();
        this.Token = new TokenData(script);
    }

    private bool AtEof {
        get => Index >= ContentLength;
    }

    private Span GetCharacterSpan() {
        return Span.Point(Script, Line, Index);
    }

    private void RaiseUnexpected() {
        throw new ParseException(Script.CollectDiagnostic(
            AtEof ? Diagnostic.SyntaxError(0, GetCharacterSpan(), new DiagnosticArguments { ["t"] = TToken.Eof })
                :   Diagnostic.SyntaxError(1, GetCharacterSpan(), new DiagnosticArguments {})
        ));
    }

    private void BeginToken() {
        Token.Start = Index;
        Token.FirstLine = Line;
    }

    private void EndToken(TToken type) {
        Token.Type = type;
        Token.End = Index;
        Token.LastLine = Line;
    }

    private void SkipAndEndToken(int length, TToken type) {
        Index += length;
        EndToken(type);
    }

    private void SkipAndEndOperator(int length, Operator op) {
        Index += length;
        EndToken(TToken.Operator);
        Token.Operator = op;
    }

    private void SkipAndEndCompoundAssignment(int length, Operator op) {
        Index += length;
        EndToken(TToken.CompoundAssignment);
        Token.Operator = op;
    }

    private void BeginSlice() {
        SliceStart = Index;
    }

    private string EndSlice() {
        var r = Content.Substring(SliceStart, Index - SliceStart);
        SliceStart = 0;
        return r;
    }

    private char Lookahead(int index = 0) {
        var j = Index + index;
        return j < ContentLength ? Content[j] : '\x00';
    }

    public void Tokenize() {
        char ch = '\x00';
        for (;;) {
            ch = Lookahead();
            if (SourceCharacter.IsWhitespace(ch)) {
                Index += 1;
            } else if (!ScanLineTerminator() && !ScanComment()) {
                break;
            }
        }
        BeginToken();
        if (SourceCharacter.IsIdentifierStart(ch)) {
            do {
                Index += 1;
            } while (SourceCharacter.IsIdentifierPart(Lookahead()));
            ScanIdentifier(Content.Substring(Token.Start, Index - Token.Start));
            return;
        }
        else if (SourceCharacter.IsDecDigit(ch)) {
            ScanNumericLiteral(false);
            return;
        }
        else {
            var slice = StringSubstr.Substr(Content, Index, 4);
            if (slice == ">>>=") {
                SkipAndEndCompoundAssignment(4, Operator.UnsignedRightShift);
                return;
            }
            switch (StringSubstr.Substr(slice, 0, 3)) {
                case "**=": this.SkipAndEndCompoundAssignment(3, Operator.Pow); return;
                case "&&=": this.SkipAndEndCompoundAssignment(3, Operator.LogicalAnd); return;
                case "^^=": this.SkipAndEndCompoundAssignment(3, Operator.LogicalXor); return;
                case "||=": this.SkipAndEndCompoundAssignment(3, Operator.LogicalOr); return;
                case "<<=": this.SkipAndEndCompoundAssignment(3, Operator.LeftShift); return;
                case ">>=": this.SkipAndEndCompoundAssignment(3, Operator.RightShift); return;
                case "...": this.SkipAndEndToken(3, TToken.Ellipsis); return;
                case ">>>": this.SkipAndEndOperator(3, Operator.UnsignedRightShift); return;
                case "===": this.SkipAndEndOperator(3, Operator.StrictEquals); return;
                case "!==": this.SkipAndEndOperator(3, Operator.StrictNotEquals); return;
            }
            switch (StringSubstr.Substr(slice, 0, 2)) {
                case "?.": this.SkipAndEndToken(2, TToken.QuestionDot); return;
                case "</": this.SkipAndEndToken(2, TToken.LtSlash); return;
                case "=>": this.SkipAndEndToken(2, TToken.FatArrow); return;
                case "+=": this.SkipAndEndCompoundAssignment(2, Operator.Add); return;
                case "-=": this.SkipAndEndCompoundAssignment(2, Operator.Subtract); return;
                case "*=": this.SkipAndEndCompoundAssignment(2, Operator.Multiply); return;
                case "/=": this.SkipAndEndCompoundAssignment(2, Operator.Divide); return;
                case "%=": this.SkipAndEndCompoundAssignment(2, Operator.Remainder); return;
                case "&=": this.SkipAndEndCompoundAssignment(2, Operator.BitwiseAnd); return;
                case "^=": this.SkipAndEndCompoundAssignment(2, Operator.BitwiseXor); return;
                case "|=": this.SkipAndEndCompoundAssignment(2, Operator.BitwiseOr); return;
                case "**": this.SkipAndEndOperator(2, Operator.Pow); return;
                case "&&": this.SkipAndEndOperator(2, Operator.LogicalAnd); return;
                case "^^": this.SkipAndEndOperator(2, Operator.LogicalXor); return;
                case "||": this.SkipAndEndOperator(2, Operator.LogicalOr); return;
                case "??": this.SkipAndEndOperator(2, Operator.NullCoalescing); return;
                case "<<": this.SkipAndEndOperator(2, Operator.LeftShift); return;
                case ">>": this.SkipAndEndOperator(2, Operator.RightShift); return;
                case "==": this.SkipAndEndOperator(2, Operator.Equals); return;
                case "!=": this.SkipAndEndOperator(2, Operator.NotEquals); return;
                case "<=": this.SkipAndEndOperator(2, Operator.Le); return;
                case ">=": this.SkipAndEndOperator(2, Operator.Ge); return;
                case "++": this.SkipAndEndOperator(2, Operator.PreIncrement); return;
                case "--": this.SkipAndEndOperator(2, Operator.PreDecrement); return;
            }
            switch (StringSubstr.CharAt(slice, 0)) {
                case '.':
                    if (SourceCharacter.IsDecDigit(Lookahead(1))) {
                        ScanNumericLiteral(true);
                        return;
                    }
                    SkipAndEndToken(1, TToken.Dot);
                    return;
                case '{': SkipAndEndToken(1, TToken.LCurly); return;
                case '}': SkipAndEndToken(1, TToken.RCurly); return;
                case '(': SkipAndEndToken(1, TToken.LParen); return;
                case ')': SkipAndEndToken(1, TToken.RParen); return;
                case '[': SkipAndEndToken(1, TToken.LSquare); return;
                case ']': SkipAndEndToken(1, TToken.RSquare); return;
                case ';': SkipAndEndToken(1, TToken.Semicolon); return;
                case ',': SkipAndEndToken(1, TToken.Comma); return;
                case '?': SkipAndEndToken(1, TToken.QuestionMark); return;
                case '!': SkipAndEndToken(1, TToken.ExclamationMark); return;
                case ':': SkipAndEndToken(1, TToken.Colon); return;
                case '=': SkipAndEndToken(1, TToken.Assign); return;
                case '+': SkipAndEndOperator(1, Operator.Add); return;
                case '-': SkipAndEndOperator(1, Operator.Subtract); return;
                case '*': SkipAndEndOperator(1, Operator.Multiply); return;
                case '/': SkipAndEndOperator(1, Operator.Divide); return;
                case '%': SkipAndEndOperator(1, Operator.Remainder); return;
                case '&': SkipAndEndOperator(1, Operator.BitwiseAnd); return;
                case '^': SkipAndEndOperator(1, Operator.BitwiseXor); return;
                case '|': SkipAndEndOperator(1, Operator.BitwiseOr); return;
                case '<': SkipAndEndOperator(1, Operator.Lt); return;
                case '>': SkipAndEndOperator(1, Operator.Gt); return;
                case '~': SkipAndEndOperator(1, Operator.BitwiseNot); return;
                case '"':
                case '\'':
                    ScanStringLiteral();
                    return;
                // keyword-allowed identifier
                case '#':
                    ScanPossiblyKeywordIdentifier();
                    return;
            }
            var idStart = ScanOptUnicodeEscapeForIdentifier(true);
            if (idStart != "") {
                ScanIdentifier(idStart);
                return;
            }
            if (!AtEof) {
                RaiseUnexpected();
            }
            EndToken(TToken.Eof);
        }
    }

    private void ScanIdentifier(string s, bool reserveKeyword = true) {
        for (;;) {
            var ch = Lookahead();
            if (SourceCharacter.IsIdentifierPart(ch)) {
                Index += 1;
                s += ch.ToString();
            } else {
                var s2 = ScanOptUnicodeEscapeForIdentifier(false);
                if (s2 == "") break;
                s += s2;
            }
        }
        EndToken(TToken.Identifier);
        Token.StringValue = s;
        if (reserveKeyword && ReservedWords.IsReservedWord(s)) {
            if (Token.RawLength != s.Count()) {
                throw new ParseException(Script.CollectDiagnostic(Diagnostic.SyntaxError(2, GetCharacterSpan())));
            }
            Token.Type = TToken.Keyword;
        }
    }

    private void ScanPossiblyKeywordIdentifier() {
        Index += 1;
        var s = "";
        if (SourceCharacter.IsIdentifierPart(Lookahead())) {
            do {
                Index += 1;
            } while (SourceCharacter.IsIdentifierPart(Lookahead()));
            s = Content.Substring(Token.Start + 1, Index - Token.Start + 1);
        } else {
            s = ScanOptUnicodeEscapeForIdentifier(false);
            if (s == "") {
                RaiseUnexpected();
            }
        }
        ScanIdentifier(s, false);
    }

    private void ScanNumericLiteral(bool startsWithDot) {
        var ch = '\x00';
        if (startsWithDot) {
            Index += 2;
        } else {
            var startsWithZero = Lookahead() == '0';
            ++Index;
            ch = Lookahead();
            if (startsWithZero) {
                // 0x... 0X...
                if (ch == '\x78' || ch == '\x58') {
                    ScanHexLiteral();
                    return;
                }
                // 0b... 0B...
                if (ch == '\x62' || ch == '\x42') {
                    ScanBinLiteral();
                    return;
                }
                if (SourceCharacter.IsDecDigit(ch)) {
                    RaiseUnexpected();
                }
            }
            ScanDecDigitSequence();
        }
        var dot = startsWithDot;
        if (!dot && Lookahead() == '\x2E') {
            Index += 1;
            dot = true;
        }
        if (dot) {
            ScanDecDigitSequence();
        }
        ch = Lookahead();
        // e E
        if (ch == '\x65' || ch == '\x45') {
            Index += 1;
            ch = Lookahead();
            if (ch == '\x2B' || ch == '\x2D') {
                Index += 1;
            }
            if (!SourceCharacter.IsDecDigit(Lookahead())) {
                RaiseUnexpected();
            }
            ScanDecDigitSequence();
        }
        EndToken(TToken.NumericLiteral);
        var s = Content.Substring(Token.Start, Index - Token.Start).Replace("_", "");
        try {
            Token.NumberValue = double.Parse(s);
        } catch (OverflowException) {
            Token.NumberValue = double.NaN;
        }
    }

    private void ScanDecDigitSequence() {
        for (;;) {
            var ch = Lookahead();
            if (SourceCharacter.IsDecDigit(ch)) {
                Index += 1;
            } else if (ch == '\x5F') {
                Index += 1;
                if (!SourceCharacter.IsDecDigit(Lookahead())) {
                    RaiseUnexpected();
                }
                Index += 1;
            } else {
                break;
            }
        }
    }

    private void ScanHexLiteral() {
        Index += 1;
        if (!SourceCharacter.IsHexDigit(Lookahead())) {
            RaiseUnexpected();
        }
        for (;;) {
            var ch = Lookahead();
            if (SourceCharacter.IsHexDigit(ch)) {
                Index += 1;
            } else if (ch == '\x5F') {
                Index += 1;
                if (!SourceCharacter.IsHexDigit(Lookahead())) {
                    RaiseUnexpected();
                }
                Index += 1;
            } else {
                break;
            }
        }
        EndToken(TToken.NumericLiteral);
        var s = Content.Substring(Token.Start + 2, Index - Token.Start + 2).Replace("_", "");
        try {
            Token.NumberValue = (double) Convert.ToInt64(s, 16);
        } catch (OverflowException) {
            Token.NumberValue = double.NaN;
        }
    }

    private void ScanBinLiteral() {
        Index += 1;
        if (!SourceCharacter.IsBinDigit(Lookahead())) {
            RaiseUnexpected();
        }
        for (;;) {
            var ch = Lookahead();
            if (SourceCharacter.IsBinDigit(ch)) {
                Index += 1;
            } else if (ch == '\x5F') {
                Index += 1;
                if (!SourceCharacter.IsBinDigit(Lookahead())) {
                    RaiseUnexpected();
                }
                Index += 1;
            } else {
                break;
            }
        }
        EndToken(TToken.NumericLiteral);
        var s = Content.Substring(Token.Start + 2, Index - Token.Start + 2).Replace("_", "");
        try {
            Token.NumberValue = (double) Convert.ToInt64(s, 2);
        } catch (OverflowException) {
            Token.NumberValue = double.NaN;
        }
    }

    private bool ScanLineTerminator() {
        var ch = Lookahead();
        if (SourceCharacter.IsLineTerminator(ch)) {
            if (ch == '\x0D' && Lookahead(1) == '\x0A') {
                Index += 1;
            }
            Index += 1;
            Line += 1;
            Script.AddLineStart(Index);
            return true;
        }
        return false;
    }

    private bool ScanComment() {
        var ch = Lookahead();
        if (ch == '\x3C' && Lookahead(1) == '\x21' && Lookahead(2) == '\x2d' && Lookahead(3) == '\x2d') {
            var startSpan = GetCharacterSpan();
            Index += 4;
            for (;;) {
                ch = Lookahead();
                if (ch == '\x2d' && Lookahead(1) == '\x2d' && Lookahead(2) == '\x3e') {
                    Index += 3;
                    break;
                } else if (!ScanLineTerminator()) {
                    if (AtEof) {
                        RaiseUnexpected();
                    }
                    Index += 1;
                }
            }
            Script.AddComment(new Comment(true, Content.Substring(startSpan.Start + 4, (Index - 3) - (startSpan.Start + 4)), startSpan.To(GetCharacterSpan())));
            return true;
        }
        if (ch != '\x2F') {
            return false;
        }
        ch = Lookahead(1);
        if (ch == '\x2A') {
            var startSpan = GetCharacterSpan();
            int nested = 1;
            Index += 2;
            // NOTE: ActionScript must not support nested
            // comments.
            for (;;) {
                ch = Lookahead();
                if (ch == '\x2F' && Lookahead(1) == '\x2A') {
                    Index += 2;
                    nested += 1;
                } else if (ch == '\x2A' && Lookahead(1) == '\x2F') {
                    Index += 2;
                    if (--nested == 0) {
                        break;
                    }
                } else if (!ScanLineTerminator()) {
                    if (AtEof) {
                        RaiseUnexpected();
                    }
                    Index += 1;
                }
            }
            var content = Content.Substring(startSpan.Start + 2, (Index - 2) - (startSpan.Start + 2));
            Script.AddComment(new Comment(true, content, startSpan.To(GetCharacterSpan())));
            return true;
        }
        if (ch == '\x2F') {
            var start = Index;
            Index += 2;
            while (!SourceCharacter.IsLineTerminator(Lookahead())) {
                Index += 1;
            }
            var content = Content.Substring(start + 2, Index - (start + 2));
            Script.AddComment(new Comment(false, content, Span.Inline(Script, Line, start, Index)));
            return true;
        }
        return false;
    }

    private string ScanOptUnicodeEscapeForIdentifier(bool atIdStart, bool mustBeValid = true) {
        if (Lookahead() != '\x5C') {
            return "";
        }
        Index += 1;
        var ch = (char) ScanUnicodeEscapeXOrU();
        if (mustBeValid) {
            if (atIdStart && !SourceCharacter.IsIdentifierStart(ch)) {
                throw new ParseException(Script.CollectDiagnostic(Diagnostic.SyntaxError(1, GetCharacterSpan())));
            }
            if (!atIdStart && !SourceCharacter.IsIdentifierPart(ch)) {
                throw new ParseException(Script.CollectDiagnostic(Diagnostic.SyntaxError(1, GetCharacterSpan())));
            }
        }
        return ch.ToString();
    }

    // scans \x or \u escape sequences starting from 'x' or 'u'; raises error
    // if a different character is found or end of program is reached.
    private int ScanUnicodeEscapeXOrU() {
        var ch = Lookahead();
        if (ch == '\x75') {
            Index += 1;
            if (Lookahead() == '\x7B') {
                return ScanMultiDigitUnicodeEscape();
            }
            return (RequireHexDigit() << 12)
                | (RequireHexDigit() << 8)
                | (RequireHexDigit() << 4)
                | RequireHexDigit();
        } else if (ch == '\x78') {
            Index += 1;
            if (Lookahead() == '\x7B') {
                return ScanMultiDigitUnicodeEscape();
            }
            return (RequireHexDigit() << 4) | RequireHexDigit();
        }
        RaiseUnexpected();
        return 0;
    }

    // scans \x{...} or \u{...} starting from {
    private int ScanMultiDigitUnicodeEscape() {
        Index += 1;
        var v = SourceCharacter.HexDigitMV(Lookahead());
        if (v == -1) {
            RaiseUnexpected();
        }
        for (;;) {
            var ch = Lookahead();
            if (ch == '\x7D') {
                break;
            } else {
                v = (v << 4) | RequireHexDigit();
            }
        }
        Index += 1;
        return v;
    }

    private int RequireHexDigit() {
        var v = SourceCharacter.HexDigitMV(Lookahead());
        if (v == -1) {
            RaiseUnexpected();
        }
        Index += 1;
        return v;
    }

    private void RequireBinDigit() {
        if (!SourceCharacter.IsBinDigit(Lookahead())) {
            RaiseUnexpected();
        }
        Index += 1;
    }

    private string ScanOptEscapeSequence() {
        if (Lookahead() != '\x5C') {
            return null;
        }
        Index += 1;
        var ch = Lookahead();
        if (ch == 0x75 || ch == 0x78) {
            return char.ConvertFromUtf32(ScanUnicodeEscapeXOrU());
        }
        switch (ch) {
            case '\x27': Index += 1; return "'";
            case '\x22': Index += 1; return "\"";
            case '\x5C': Index += 1; return "\\";
            case '\x62': Index += 1; return "\b";
            case '\x66': Index += 1; return "\f";
            case '\x6E': Index += 1; return "\n";
            case '\x72': Index += 1; return "\r";
            case '\x74': Index += 1; return "\t";
            case '\x76': Index += 1; return "\v";
            case '\x30': Index += 1; return "\0";
        }
        if (ScanLineTerminator()) {
            return "";
        }
        if (AtEof) {
            RaiseUnexpected();
        }
        Index += 1;
        return ch.ToString();
    }

    public void ScanRegExpLiteral() {
        var ch = '\x00';
        for (;;) {
            ch = Lookahead();
            if (ch == '\x5C') {
                Index += 1;
                if (AtEof) RaiseUnexpected();
                if (!ScanLineTerminator()) {
                    Index += 1;
                }
            } else if (ch == '\x2F') {
                Index += 1;
                break;
            } else if (AtEof) {
                RaiseUnexpected();
            } else if (!ScanLineTerminator()) {
                Index += 1;
            }
        }
        var bodyStart = Token.Start + (Token.IsOperator(Operator.Divide) ? 1 : 2);
        var body = SourceCharacter.CrOrCrLfRegex.Replace(Content.Substring(bodyStart, Index - 1 - bodyStart), "\n");
        Token.StringValue = body;
        var flags = "";
        for (;;) {
            var s = ScanOptUnicodeEscapeForIdentifier(false, false);
            if (s != "") {
                flags += s;
            } else if (SourceCharacter.IsIdentifierPart(Lookahead())) {
                flags += Lookahead().ToString();
                Index += 1;
            } else {
                break;
            }
        }
        Token.Flags = flags;
        EndToken(TToken.RegExpLiteral);
    }

    private void ScanStringLiteral() {
        var delim = Lookahead();
        Index += 1;
        if (Lookahead() == delim && Lookahead(1) == delim) {
            ScanTripleStringLiteral(delim);
            return;
        }
        var ch = '\x00';
        var lineBreakFound = false;
        var builder = new List<String>();
        BeginSlice();
        for (;;) {
            ch = Lookahead();
            if (ch == delim) {
                builder.Add(EndSlice());
                Index += 1;
                break;
            } else if (ch == '\x5C') {
                builder.Add(EndSlice());
                builder.Add(ScanOptEscapeSequence());
                BeginSlice();
            } else if (SourceCharacter.IsLineTerminator(ch)) {
                lineBreakFound = true;
                builder.Add(EndSlice());
                builder.Add("\n");
                ScanLineTerminator();
                BeginSlice();
            } else if (AtEof) {
                RaiseUnexpected();
            } else {
                Index += 1;
            }
        }
        EndToken(TToken.StringLiteral);
        Token.StringValue = String.Join("", builder);
        if (lineBreakFound) {
            throw new ParseException(Script.CollectDiagnostic(Diagnostic.SyntaxError(3, Token.Span)));
        }
    }

    private void ScanTripleStringLiteral(char delim) {
        Index += 2;
        var ch = '\x00';
        var lines = new List<string> {};
        var builder = new List<string> {};
        var startedWithLineBreak = ScanLineTerminator();
        BeginSlice();
        for (;;) {
            ch = Lookahead();
            if (ch == delim && Lookahead(1) == delim && Lookahead(2) == delim) {
                builder.Add(EndSlice());
                lines.Add(String.Join("", builder));
                builder.Clear();
                Index += 3;
                break;
            } else if (ch == '\x5C') {
                builder.Add(EndSlice());
                builder.Add(ScanOptEscapeSequence());
                BeginSlice();
            } else if (SourceCharacter.IsLineTerminator(ch)) {
                builder.Add(EndSlice());
                lines.Add(String.Join("", builder));
                builder.Clear();
                ScanLineTerminator();
                BeginSlice();
            } else if (AtEof) {
                RaiseUnexpected();
            } else {
                Index += 1;
            }
        }
        EndToken(TToken.StringLiteral);
        var lastLine = "";
        if (startedWithLineBreak && lines.Count > 1) {
            lastLine = lines.Last();
            lines.RemoveAt(lines.Count - 1);
        }
        int baseIndent = 0;
        for (; baseIndent < lastLine.Count(); ++baseIndent) {
            if (!SourceCharacter.IsWhitespace(StringSubstr.CharAt(lastLine, baseIndent))) {
                break;
            }
        }
        lines = new List<string>(lines.Select(line => {
            int indent = 0;
            for (; indent < line.Count(); ++indent) {
                if (!SourceCharacter.IsWhitespace(StringSubstr.CharAt(line, indent))) {
                    break;
                }
            }
            return line.Substring(Math.Min(baseIndent, indent));
        }));
        lastLine = lastLine.Substring(baseIndent);
        if (lastLine.Count() > 0) {
            lines.Add(lastLine);
        }
        Token.StringValue = String.Join("\n", lines);
    }
}