namespace VioletScript.Parser.Parser;

using Ast = VioletScript.Parser.Ast;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Diagnostic;
using VioletScript.Parser.Source;
using VioletScript.Parser.Tokenizer;
using VioletScript.Util;
using System.IO;
using System.Text.RegularExpressions;

using DiagnosticArguments = Dictionary<string, object>;
using TToken = VioletScript.Parser.Tokenizer.Token;

public sealed class Parser {
    private Script Script;
    private Tokenizer Tokenizer;
    private ParserBackend Backend;

    public Parser(Script script) {
        Script = script;
        Tokenizer = new Tokenizer(script);
        Backend = new ParserBackend(script, Tokenizer);
        try {
            Tokenizer.Tokenize();
        } catch (ParseException) {
        }
    }

    public Ast.Program ParseProgram() {
        if (!Script.IsValid) {
            return null;
        }
        Ast.Program r = null;
        try {
            r = Backend.ParseProgram();
        } catch (ParseException) {
        }
        return Script.IsValid ? r : null;
    }

    public Ast.TypeExpression ParseTypeExpression() {
        if (!Script.IsValid) {
            return null;
        }
        Ast.TypeExpression r = null;
        try {
            r = Backend.ParseTypeExpression();
            if (Backend.Token.Type != TToken.Eof) {
                Backend.ThrowUnexpected();
            }
        } catch (ParseException) {
        }
        return Script.IsValid ? r : null;
    }
}

internal record struct OperatorFilter(
    Operator @operator,
    OperatorPrecedence precedence,
    OperatorPrecedence nextPrecedence
);

internal class ParserBackend {
    private Script Script;
    private Tokenizer Tokenizer;
    public TokenData Token;
    public TokenData PreviousToken;
    private Stack<(int line, int index)> Locations = new Stack<(int line, int index)>();
    private Stack<StackFunction> FunctionStack = new Stack<StackFunction>();
    private List<String> AlreadyIncludedFilePaths = new List<String>{};

    public ParserBackend(Script script, Tokenizer tokenizer) {
        Script = script;
        Tokenizer = tokenizer;
        Token = Tokenizer.Token;
        PreviousToken = new TokenData(script);
        this.AlreadyIncludedFilePaths.Add(this.Script.FilePath);
    }

    public StackFunction StackFunction {
        get => FunctionStack.Count > 0 ? FunctionStack.Peek() : null;
    }

    public void NextToken() {
        Token.CopyTo(PreviousToken);
        Tokenizer.Tokenize();
    }

    public Diagnostic SyntaxError(int id, Span span, DiagnosticArguments args = null) {
        return Script.CollectDiagnostic(Diagnostic.SyntaxError(id, span, args));
    }

    public Diagnostic VerifyError(int id, Span span, DiagnosticArguments args = null) {
        return Script.CollectDiagnostic(Diagnostic.VerifyError(id, span, args));
    }

    public Diagnostic Warn(int id, Span span, DiagnosticArguments args = null) {
        return Script.CollectDiagnostic(Diagnostic.Warning(id, span, args));
    }

    public void ThrowUnexpected() {
        RaiseUnexpected();
    }

    public void RaiseUnexpected() {
        throw new ParseException(SyntaxError(0, Token.Span, new DiagnosticArguments {["t"] = this.Token}));
    }

    public bool Consume(TToken type) {
        if (Token.Type == type) {
            NextToken();
            return true;
        }
        return false;
    }

    public string ConsumeIdentifier(bool keyword = false) {
        if (Token.Type == TToken.Identifier || (keyword && Token.Type == TToken.Keyword)) {
            NextToken();
            return PreviousToken.StringValue;
        }
        return null;
    }

    public bool ConsumeOperator(Operator type) {
        if (Token.IsOperator(type)) {
            NextToken();
            return true;
        }
        return false;
    }

    public bool ConsumeKeyword(string name) {
        if (Token.IsKeyword(name)) {
            NextToken();
            return true;
        }
        return false;
    }

    public bool ConsumeContextKeyword(string name) {
        if (Token.IsContextKeyword(name)) {
            NextToken();
            return true;
        }
        return false;
    }

    public void Expect(Token type) {
        if (Token.Type != type) {
            ThrowUnexpected();
        }
        NextToken();
    }

    public string ExpectIdentifier(bool keyword = false) {
        if (Token.Type == TToken.Identifier || (keyword && Token.Type == TToken.Keyword)) {
            NextToken();
            return PreviousToken.StringValue;
        }
        ThrowUnexpected();
        return "";
    }

    public void ExpectOperator(Operator type) {
        if (!Token.IsOperator(type)) {
            ThrowUnexpected();
        }
        NextToken();
    }

    public void ExpectGT() {
        if (ConsumeOperator(Operator.Gt)) {
            return;
        }
        if (Token.IsOperator(Operator.Ge)) {
            this.PreviousToken.Type = TToken.Operator;
            this.PreviousToken.Operator = Operator.Gt;
            this.PreviousToken.FirstLine =
            this.PreviousToken.LastLine = this.Token.FirstLine;
            this.PreviousToken.Start = this.Token.Start;
            this.PreviousToken.End = this.Token.Start + 1;
            this.Token.Type = TToken.Assign;
            this.Token.Start += 1;
            return;
        }
        if (Token.IsOperator(Operator.RightShift)) {
            this.PreviousToken.Type = TToken.Operator;
            this.PreviousToken.Operator = Operator.Gt;
            this.PreviousToken.FirstLine =
            this.PreviousToken.LastLine = this.Token.FirstLine;
            this.PreviousToken.Start = this.Token.Start;
            this.PreviousToken.End = this.Token.Start + 1;
            this.Token.Type = TToken.Operator;
            this.Token.Operator = Operator.Gt;
            this.Token.Start += 1;
            return;
        }
        if (Token.IsOperator(Operator.UnsignedRightShift)) {
            this.PreviousToken.Type = TToken.Operator;
            this.PreviousToken.Operator = Operator.Gt;
            this.PreviousToken.FirstLine =
            this.PreviousToken.LastLine = this.Token.FirstLine;
            this.PreviousToken.Start = this.Token.Start;
            this.PreviousToken.End = this.Token.Start + 1;
            this.Token.Type = TToken.Operator;
            this.Token.Operator = Operator.RightShift;
            this.Token.Start += 1;
            return;
        }
        ThrowUnexpected();
    }

    public void ExpectKeyword(string name) {
        if (!Token.IsKeyword(name)) {
            ThrowUnexpected();
        }
        NextToken();
    }

    public void ExpectContextKeyword(string name) {
        if (!Token.IsContextKeyword(name)) {
            ThrowUnexpected();
        }
        NextToken();
    }

    public void MarkLocation() {
        Locations.Push((Token.FirstLine, Token.Start));
    }

    public void PushLocation(Span span) {
        Locations.Push((span.FirstLine, span.Start));
    }

    public void DuplicateLocation() {
        Locations.Push(Locations.Peek());
    }

    public Span PopLocation() {
        var l = Locations.Pop();
        return Span.WithLinesAndIndexes(Script, l.line, PreviousToken.LastLine, l.index, PreviousToken.End);
    }

    public Ast.Node FinishNode(Ast.Node node, object lastSpanOrNode = null) {
        if (lastSpanOrNode != null) {
            Span lastSpan = lastSpanOrNode is Ast.Node ? ((Ast.Node) lastSpanOrNode).Span.Value : ((Span) lastSpanOrNode);
            var (firstLine, start) = Locations.Pop();
            node.Span = Span.WithLinesAndIndexes(Script, firstLine, lastSpan.LastLine, start, lastSpan.End);
        } else {
            node.Span = PopLocation();
        }
        return node;
    }

    public Ast.Expression FinishExp(Ast.Node node, object lastSpanOrNode = null) {
        return (Ast.Expression) FinishNode(node, lastSpanOrNode);
    }

    public Ast.Statement FinishStatement(Ast.Node node, object lastSpanOrNode = null) {
        return (Ast.Statement) FinishNode(node, lastSpanOrNode);
    }

    public Ast.TypeExpression ParseTypeExpression() {
        MarkLocation();
        Ast.TypeExpression r = null;
        ConsumeOperator(Operator.BitwiseOr);
        if (ConsumeContextKeyword("undefined")) {
            r = (Ast.TypeExpression) FinishNode(new Ast.UndefinedTypeExpression());
        } else if (Consume(TToken.Identifier)) {
            r = (Ast.TypeExpression) FinishNode(new Ast.IdentifierTypeExpression(PreviousToken.StringValue));
            if (Consume(TToken.FatArrow)) {
                PushLocation(r.Span.Value);
                var (@params, optParams) = ConvertTypeExpressionsIntoFunctionParams(new List<Ast.TypeExpression>{r});
                r = ParseArrowFunctionTypeExpression(@params, null, null);
            }
        } else if (ConsumeKeyword("function")) {
            Expect(TToken.LParen);
            List<Ast.Identifier> @params = null;
            List<Ast.Identifier> optParams = null;
            Ast.Identifier restParam = null;
            do {
                if (Token.Type == TToken.RParen) break;
                if (Consume(TToken.Ellipsis)) {
                    MarkLocation();
                    var name = ExpectIdentifier();
                    restParam = (Ast.Identifier) FinishNode(new Ast.Identifier(name, Consume(TToken.Colon) ? ParseTypeExpression() : null));
                    break;
                } else if (Consume(TToken.Identifier)) {
                    MarkLocation();
                    var name = PreviousToken.StringValue;
                    var opt = Consume(TToken.QuestionMark);
                    var p = (Ast.Identifier) FinishNode(new Ast.Identifier(name, Consume(TToken.Colon) ? ParseTypeExpression() : null));
                    if (opt) {
                        optParams ??= new List<Ast.Identifier>();
                        optParams.Add(p);
                    } else {
                        if (@params != null) {
                            SyntaxError(4, Token.Span);
                        }
                        @params ??= new List<Ast.Identifier>();
                        @params.Add(p);
                    }
                } else ThrowUnexpected();
            } while (Consume(TToken.Comma));
            Expect(TToken.RParen);
            var returnType = Consume(TToken.Colon) ? ParseTypeExpression() : null;
            r = (Ast.TypeExpression) FinishNode(new Ast.FunctionTypeExpression(@params, optParams, restParam, returnType));
        } else if (Consume(TToken.LSquare)) {
            if (Consume(TToken.RSquare)) {
                r = (Ast.TypeExpression) FinishNode(new Ast.TupleTypeExpression(new List<Ast.TypeExpression>{}));
            } else {
                var fst = ParseTypeExpression();
                if (Token.Type == TToken.Comma) {
                    var itemTypes = new List<Ast.TypeExpression>{fst};
                    while (Consume(TToken.Comma)) {
                        if (Token.Type == TToken.RSquare) break;
                        itemTypes.Add(ParseTypeExpression());
                    }
                    Expect(TToken.RSquare);
                    r = (Ast.TypeExpression) FinishNode(new Ast.TupleTypeExpression(itemTypes));
                } else {
                    Expect(TToken.RSquare);
                    r = (Ast.TypeExpression) FinishNode(new Ast.ArrayTypeExpression(fst));
                }
            }
        } else if (Consume(TToken.LCurly)) {
            var fields = new List<Ast.Identifier>{};
            do {
                if (Token.Type == TToken.RCurly) break;
                fields.Add(this.ParseRecordTypeField());
            } while (Consume(TToken.Comma));
            Expect(TToken.RCurly);
            r = (Ast.TypeExpression) FinishNode(new Ast.RecordTypeExpression(fields));
        } else if (Consume(TToken.LParen)) {
            r = ParseParensTypeExpression();
        } else if (ConsumeKeyword("void")) {
            r = (Ast.TypeExpression) FinishNode(new Ast.VoidTypeExpression());
        } else if (ConsumeOperator(Operator.Multiply)) {
            r = (Ast.TypeExpression) FinishNode(new Ast.AnyTypeExpression());
        } else if (ConsumeKeyword("null")) {
            r = (Ast.TypeExpression) FinishNode(new Ast.NullTypeExpression());
        } else if (ConsumeOperator(Operator.BitwiseOr)) {
            PopLocation();
            r = ParseTypeExpression();
        } else if (Consume(TToken.QuestionMark)) {
            r = (Ast.TypeExpression) FinishNode(new Ast.NullableTypeExpression(ParseTypeExpression()));
        } else ThrowUnexpected();

        for (;;) {
            if (Consume(TToken.QuestionMark)) {
                PushLocation(r.Span.Value);
                r = (Ast.TypeExpression) FinishNode(new Ast.NullableTypeExpression(r));
            }
            else if (Consume(TToken.ExclamationMark)) {
                PushLocation(r.Span.Value);
                r = (Ast.TypeExpression) FinishNode(new Ast.NonNullableTypeExpression(r));
            }
            else if (Consume(TToken.Dot)) {
                PushLocation(r.Span.Value);
                if (ConsumeOperator(Operator.Lt)) {
                    var argumentsList = new List<Ast.TypeExpression>();
                    do {
                        argumentsList.Add(ParseTypeExpression());
                    } while (Consume(TToken.Comma));
                    ExpectGT();
                    r = (Ast.TypeExpression) FinishNode(new Ast.TypeExpressionWithArguments(r, argumentsList));
                } else {
                    r = (Ast.TypeExpression) FinishNode(new Ast.MemberTypeExpression(r, ParseIdentifier(true)));
                }
            }
            else if (Consume(TToken.Colon)) {
                PushLocation(r.Span.Value);
                r = (Ast.TypeExpression) FinishNode(new Ast.TypedTypeExpression(r, ParseTypeExpression()));
            }
            else if (ConsumeOperator(Operator.BitwiseOr)) {
                PushLocation(r.Span.Value);
                var m = r is Ast.UnionTypeExpression ? ((Ast.UnionTypeExpression) r).Types : new List<Ast.TypeExpression>{r};
                m.Add(ParseTypeExpression());
                // var m2 = new List<Ast.TypeExpression>{...m, ParseTypeExpression()};
                r = (Ast.TypeExpression) FinishNode(new Ast.UnionTypeExpression(m));
            }
            else break;
        }

        return r;
    }

    private Ast.Identifier ParseRecordTypeField() {
        this.MarkLocation();
        var name = ExpectIdentifier();
        Span? optionalSpan = null;
        if (this.Token.Type == TToken.QuestionMark) {
            this.MarkLocation();
            this.NextToken();
            optionalSpan = this.PopLocation();
        }
        var fieldType = this.Consume(TToken.Colon) ? ParseTypeExpression() : null;
        if (optionalSpan.HasValue) {
            var undefinedType = new Ast.UndefinedTypeExpression();
            undefinedType.Span = optionalSpan;
            this.PushLocation(fieldType.Span.Value);
            fieldType = (Ast.TypeExpression) this.FinishNode(new Ast.UnionTypeExpression(new List<Ast.TypeExpression>{undefinedType, fieldType}));
        }
        return (Ast.Identifier) FinishNode(new Ast.Identifier(name, fieldType));
    }

    private Ast.TypeExpression ParseParensTypeExpression() {
        Ast.TypeExpression r = null;
        if (Token.Type == TToken.RParen) {
            NextToken();
            Expect(TToken.FatArrow);
            return ParseArrowFunctionTypeExpression(null, null, null);
        }
        if (Consume(TToken.Ellipsis)) {
            MarkLocation();
            var restParamName = ExpectIdentifier();
            var restParam = (Ast.Identifier) FinishNode(new Ast.Identifier(restParamName, Consume(TToken.Colon) ? ParseTypeExpression() : null));
            Expect(TToken.RParen);
            Expect(TToken.FatArrow);
            return ParseArrowFunctionTypeExpression(null, null, restParam);
        }
        var fst = ParseTypeExpression();
        if (Token.Type == TToken.Comma) {
            var itemTypes = new List<Ast.TypeExpression>{fst};
            while (Consume(TToken.Comma)) {
                if (Token.Type == TToken.RParen
                ||  Token.Type == TToken.Ellipsis)
                    break;
                itemTypes.Add(ParseTypeExpression());
            }
            if (Consume(TToken.Ellipsis)) {
                var (@params, optParams) = ConvertTypeExpressionsIntoFunctionParams(itemTypes);
                MarkLocation();
                var restParamName = ExpectIdentifier();
                var restParam = (Ast.Identifier) FinishNode(new Ast.Identifier(restParamName, Consume(TToken.Colon) ? ParseTypeExpression() : null));
                Expect(TToken.RParen);
                Expect(TToken.FatArrow);
                r = ParseArrowFunctionTypeExpression(@params, optParams, restParam);
            } else {
                Expect(TToken.RParen);
                Expect(TToken.FatArrow);
                var (@params, optParams) = ConvertTypeExpressionsIntoFunctionParams(itemTypes);
                r = ParseArrowFunctionTypeExpression(@params, optParams, null);
            }
        } else {
            Expect(TToken.RParen);
            if (Consume(TToken.FatArrow)) {
                var (@params, optParams) = ConvertTypeExpressionsIntoFunctionParams(new List<Ast.TypeExpression>{fst});
                r = ParseArrowFunctionTypeExpression(@params, optParams, null);
            } else r = (Ast.TypeExpression) FinishNode(new Ast.ParensTypeExpression(fst));
        }
        return r;
    }

    private (List<Ast.Identifier>, List<Ast.Identifier>) ConvertTypeExpressionsIntoFunctionParams(List<Ast.TypeExpression> types) {
        List<Ast.Identifier> @params = null;
        List<Ast.Identifier> optParams = null;
        foreach (var t in types) {
            if (t is Ast.TypedTypeExpression) {
                var t_asTte = (Ast.TypedTypeExpression) t;
                var r = ConvertTypeExpressionsIntoFunctionParams(t_asTte.Base, t_asTte.Type, @params, optParams);
                @params = r.Item1;
                optParams = r.Item2;
            } else {
                var r = ConvertTypeExpressionsIntoFunctionParams(t, null, @params, optParams);
                @params = r.Item1;
                @optParams = r.Item2;
            }
        }
        return (@params, optParams);
    }

    private (List<Ast.Identifier>, List<Ast.Identifier>) ConvertTypeExpressionsIntoFunctionParams(Ast.TypeExpression node, Ast.TypeExpression type, List<Ast.Identifier> outParams, List<Ast.Identifier> outOptParams) {
        if (node is Ast.NullableTypeExpression) {
            var node_asNte = (Ast.NullableTypeExpression) node;
            if (node_asNte.Base is Ast.IdentifierTypeExpression) {
                var b = (Ast.IdentifierTypeExpression) node_asNte.Base;
                PushLocation(node.Span.Value);
                outOptParams = outOptParams ?? new List<Ast.Identifier>{};
                outOptParams.Add((Ast.Identifier) FinishNode(new Ast.Identifier(b.Name, type), node.Span.Value));
            } else SyntaxError(28, node_asNte.Base.Span.Value);
        } else if (node is Ast.IdentifierTypeExpression) {
            var node_asIte = (Ast.IdentifierTypeExpression) node;
            PushLocation(node.Span.Value);
            outParams = outParams ?? new List<Ast.Identifier>{};
            outParams.Add((Ast.Identifier) FinishNode(new Ast.Identifier(node_asIte.Name, type), node.Span.Value));
            if (outOptParams != null) SyntaxError(4, node.Span.Value);
        } else SyntaxError(1, node.Span.Value);
        return (outParams, outOptParams);
    }

    private Ast.TypeExpression ParseArrowFunctionTypeExpression(List<Ast.Identifier> @params, List<Ast.Identifier> optParams, Ast.Identifier restParam) {
        var returnType = ParseTypeExpression();
        return (Ast.TypeExpression) FinishNode(new Ast.FunctionTypeExpression(@params, optParams, restParam, returnType));
    }

    private Ast.VariableBinding ParseVariableBinding(bool allowIn = true) {
        MarkLocation();
        var pattern = ParseDestructuringPattern();
        var init = Consume(TToken.Assign) ? ParseExpression(allowIn, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction) : null;
        return (Ast.VariableBinding) FinishNode(new Ast.VariableBinding(pattern, init));
    }

    public Ast.SimpleVariableDeclaration ParseSimpleVariableDeclaration(bool allowIn = true) {
        var r = ParseOptSimpleVariableDeclaration(allowIn);
        if (r == null) ThrowUnexpected();
        return r;
    }

    public Ast.SimpleVariableDeclaration ParseOptSimpleVariableDeclaration(bool allowIn = true) {
        var constFound = Token.IsKeyword("const");
        if (!(Token.IsKeyword("var") || constFound)) {
            return null;
        }
        MarkLocation();
        NextToken();
        var bindings = new List<Ast.VariableBinding>{};
        do {
            bindings.Add(ParseVariableBinding(allowIn));
        } while (Consume(TToken.Comma));
        return (Ast.SimpleVariableDeclaration) FinishNode(new Ast.SimpleVariableDeclaration(constFound, bindings));
    }

    public Ast.DestructuringPattern ParseDestructuringPattern() {
        if (!(Token.Type == TToken.Identifier || Token.Type == TToken.LCurly || Token.Type == TToken.LSquare)) {
            ThrowUnexpected();
        }
        var p = ParseOptPrimaryExpression();
        if (this.Consume(TToken.ExclamationMark)) {
            PushLocation(p.Span.Value);
            p = this.FinishExp(new Ast.UnaryExpression(Operator.NonNull, p));
        }
        return ConvertExpressionIntoDestructuringPattern(p);
    }

    private Ast.DestructuringPattern ConvertExpressionIntoDestructuringPattern(Ast.Expression e) {
        var r = OptConvertExpressionIntoDestructuringPattern(e);
        if (r == null) throw new ParseException(SyntaxError(5, e.Span.Value));
        return r;
    }

    private Ast.DestructuringPattern OptConvertExpressionIntoDestructuringPattern(Ast.Expression e) {
        if (e is Ast.Identifier e_asId) {
            PushLocation(e_asId.Span.Value);
            return (Ast.DestructuringPattern) FinishNode(new Ast.NondestructuringPattern(e_asId.Name, e_asId.Type, null), e_asId.Span.Value);
        } else if (e is Ast.ArrayInitializer e_asAi) {
            PushLocation(e_asAi.Span.Value);
            var items = new List<Ast.Node>{};
            foreach (var item in e_asAi.Items) {
                if (item is Ast.Spread item_asSpr) {
                    PushLocation(item_asSpr.Span.Value);
                    var se = ConvertExpressionIntoDestructuringPattern(item_asSpr.Expression);
                    items.Add(FinishNode(new Ast.ArrayDestructuringSpread(se), item.Span.Value));
                } else if (item == null) {
                    items.Add(null);
                } else {
                    items.Add(ConvertExpressionIntoDestructuringPattern((Ast.Expression) item));
                }
            }
            return (Ast.DestructuringPattern) FinishNode(new Ast.ArrayDestructuringPattern(items, e_asAi.Type, null));
        } else if (e is Ast.ObjectInitializer e_asOi) {
            PushLocation(e_asOi.Span.Value);
            var fields = new List<Ast.RecordDestructuringPatternField>{};
            foreach (var field in e_asOi.Fields) {
                if (field is Ast.Spread field_asSpread) {
                    throw new ParseException(SyntaxError(5, field.Span.Value));
                } else {
                    var field_asOf = (Ast.ObjectField) field;
                    var subpattern = field_asOf.Value != null ? ConvertExpressionIntoDestructuringPattern(field_asOf.Value) : null;
                    PushLocation(field.Span.Value);
                    fields.Add((Ast.RecordDestructuringPatternField) FinishNode(new Ast.RecordDestructuringPatternField(field_asOf.Key, subpattern, field_asOf.KeySuffix), field.Span.Value));
                }
            }
            return (Ast.DestructuringPattern) FinishNode(new Ast.RecordDestructuringPattern(fields, e_asOi.Type, null));
        } else if (e is Ast.UnaryExpression unary && unary.Operator == Operator.NonNull) {
            var p = this.OptConvertExpressionIntoDestructuringPattern(unary.Operand);
            if (p != null) {
                p.Suffix = '!';
                return p;
            }
        }
        return null;
    }

    public Ast.Expression ParseExpression(bool allowIn = true, OperatorPrecedence minPrecedence = null, bool allowTypedId = true) {
        var r = ParseOptExpression(allowIn, minPrecedence, allowTypedId);
        if (r == null) ThrowUnexpected();
        return r;
    }

    public Ast.Expression ParseOptExpression(bool allowIn = true, OperatorPrecedence minPrecedence = null, bool allowTypedId = true) {
        minPrecedence = minPrecedence ?? OperatorPrecedence.List;
        var r = ParseOptPrimaryExpression(allowTypedId, minPrecedence);
        if (r == null) {
            var filter = FilterUnaryOperator();
            if (filter != null && minPrecedence.ValueOf() <= filter.Value.precedence.ValueOf()) {
                MarkLocation();
                NextToken();
                r = FinishExp(new Ast.UnaryExpression(filter.Value.@operator, ParseExpression(allowIn, filter.Value.nextPrecedence, allowTypedId)));
                if (filter.Value.@operator == Operator.Await) {
                    var stackFunction = StackFunction;
                    if (stackFunction != null) {
                        if (stackFunction.UsesYield) {
                            SyntaxError(6, r.Span.Value);
                        } else stackFunction.UsesAwait = true;
                    } else if (this.FunctionStack.Count() == 0) {
                        SyntaxError(38, r.Span.Value);
                    }
                } else if (filter.Value.@operator == Operator.Yield) {
                    var stackFunction = StackFunction;
                    if (stackFunction != null) {
                        if (stackFunction.UsesAwait) {
                            SyntaxError(7, r.Span.Value);
                        } else stackFunction.UsesYield = true;
                    } else SyntaxError(17, r.Span.Value);
                }
            }
            // function expression
            else if (minPrecedence.ValueOf() <= OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction.ValueOf() && ConsumeKeyword("function")) {
                PushLocation(PreviousToken.Span);
                Ast.Identifier id = null;
                if (Consume(TToken.Identifier)) {
                    PushLocation(PreviousToken.Span);
                    id = (Ast.Identifier) FinishNode(new Ast.Identifier(PreviousToken.StringValue));
                }
                var (common, _) = ParseFunctionCommon();
                r = FinishExp(new Ast.FunctionExpression(id, common));
                if (common.Body == null) SyntaxError(20, r.Span.Value);
            }
        }
        return r != null ? ParseSubexpressions(r, allowIn, minPrecedence) : null;
    }

    private Ast.Expression ParseOptPrimaryExpression(bool allowTypedId = true, OperatorPrecedence minPrecedence = null) {
        minPrecedence = minPrecedence ?? OperatorPrecedence.Postfix;
        if (Consume(TToken.Identifier)) {
            return ParseIdentifierPrimaryExpression(allowTypedId, minPrecedence);
        } else if (ConsumeKeyword("default")) {
            PushLocation(PreviousToken.Span);
            Expect(TToken.LParen);
            var te = ParseTypeExpression();
            Expect(TToken.RParen);
            return FinishExp(new Ast.DefaultExpression(te));
        } else if (ConsumeKeyword("this")) {
            PushLocation(PreviousToken.Span);
            return FinishExp(new Ast.ThisLiteral());
        } else if (ConsumeKeyword("import")) {
            PushLocation(PreviousToken.Span);
            Expect(TToken.Dot);
            ExpectContextKeyword("meta");
            return FinishExp(new Ast.ImportMetaExpression());
        } else if (ConsumeKeyword("null")) {
            PushLocation(PreviousToken.Span);
            return FinishExp(new Ast.NullLiteral());
        } else if (Consume(TToken.StringLiteral)) {
            PushLocation(PreviousToken.Span);
            return FinishExp(new Ast.StringLiteral(PreviousToken.StringValue));
        } else if (Consume(TToken.NumericLiteral)) {
            PushLocation(PreviousToken.Span);
            return FinishExp(new Ast.NumericLiteral(PreviousToken.NumberValue));
        } else if (ConsumeKeyword("true")) {
            PushLocation(PreviousToken.Span);
            return FinishExp(new Ast.BooleanLiteral(true));
        } else if (ConsumeKeyword("false")) {
            PushLocation(PreviousToken.Span);
            return FinishExp(new Ast.BooleanLiteral(false));
        } else if (Token.IsOperator(Operator.Divide) || Token.IsCompoundAssignment(Operator.Divide)) {
            MarkLocation();
            Tokenizer.ScanRegExpLiteral();
            NextToken();
            return FinishExp(new Ast.RegExpLiteral(PreviousToken.StringValue, PreviousToken.Flags));
        } else if (ConsumeKeyword("super")) {
            PushLocation(PreviousToken.Span);
            return FinishExp(new Ast.SuperExpression());
        } else if (Consume(TToken.LParen)) {
            PushLocation(PreviousToken.Span);
            // arrow function
            if (minPrecedence.ValueOf() <= OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction.ValueOf() && Consume(TToken.RParen)) {
                Expect(TToken.FatArrow);
                return ParseArrowFunctionExpression(null, null, null);
            }
            if (minPrecedence.ValueOf() <= OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction.ValueOf()) {
                return ParseParensExpOrArrowF();
            } else {
                var exp = ParseExpression();
                Expect(TToken.RParen);
                return FinishExp(new Ast.ParensExpression(exp));
            }
        } else if (Consume(TToken.LCurly)) {
            PushLocation(PreviousToken.Span);
            var fields = new List<Ast.Node>{};
            do {
                if (Token.Type == TToken.RCurly) break;
                if (Consume(TToken.Ellipsis)) {
                    PushLocation(PreviousToken.Span);
                    fields.Add(FinishNode(new Ast.Spread(ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction))));
                } else {
                    MarkLocation();
                    Ast.Expression key = null;
                    Ast.Expression value = null;
                    char? suffix = null;
                    if (Consume(TToken.Identifier)) {
                        PushLocation(PreviousToken.Span);
                        key = FinishExp(new Ast.StringLiteral(PreviousToken.StringValue));
                        suffix = this.Consume(TToken.ExclamationMark) ? '!' : null;
                        value = Consume(TToken.Colon) ? ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction) : null;
                    } else {
                        if (Consume(TToken.LSquare)) {
                            key = ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction);
                            Expect(TToken.RSquare);
                        } else if (Consume(TToken.NumericLiteral)) {
                            PushLocation(PreviousToken.Span);
                            key = FinishExp(new Ast.NumericLiteral(PreviousToken.NumberValue));
                        } else {
                            Expect(TToken.StringLiteral);
                            PushLocation(PreviousToken.Span);
                            key = FinishExp(new Ast.StringLiteral(PreviousToken.StringValue));
                            suffix = this.Consume(TToken.ExclamationMark) ? '!' : null;
                        }
                        Expect(TToken.Colon);
                        value = ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction);
                    }
                    var field = new Ast.ObjectField(key, value);
                    field.KeySuffix = suffix;
                    fields.Add(FinishNode(field));
                }
            } while (Consume(TToken.Comma));
            Expect(TToken.RCurly);
            return FinishExp(new Ast.ObjectInitializer(fields, Consume(TToken.Colon) ? ParseTypeExpression() : null));
        } else if (Consume(TToken.LSquare)) {
            PushLocation(PreviousToken.Span);
            var items = new List<Ast.Expression>{};
            do {
                while (Consume(TToken.Comma)) items.Add(null);
                if (Token.Type == TToken.RSquare) break;
                if (Consume(TToken.Ellipsis)) {
                    PushLocation(PreviousToken.Span);
                    items.Add(FinishExp(new Ast.Spread(ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction))));
                } else items.Add(ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction));
            } while (Consume(TToken.Comma));
            Expect(TToken.RSquare);
            return FinishExp(new Ast.ArrayInitializer(items, Consume(TToken.Colon) ? ParseTypeExpression() : null));
        } else if (ConsumeKeyword("new")) {
            return ParseNewExpression();
        } else if (Token.IsOperator(Operator.Lt)) {
            return ParseMarkupInitializer(true);
        }
        return null;
    }

    private Ast.Expression ParseNewExpression() {
        PushLocation(PreviousToken.Span);
        var @base = ParseOptPrimaryExpression(false);
        if (@base == null) ThrowUnexpected();
        for (;;) {
            if (Token.Type == TToken.Dot) {
                PushLocation(@base.Span.Value);
                NextToken();
                if (ConsumeOperator(Operator.Lt)) {
                    var argumentsList2 = new List<Ast.TypeExpression>{};
                    do {
                        argumentsList2.Add(ParseTypeExpression());
                    } while (Consume(TToken.Comma));
                    ExpectGT();
                    @base = FinishExp(new Ast.ExpressionWithTypeArguments(@base, argumentsList2));
                } else {
                    var id = ParseIdentifier(true);
                    @base = FinishExp(new Ast.MemberExpression(@base, id));
                }
            }
            else if (Token.Type == TToken.LSquare && InlineOrAtHigherIndentLine) {
                PushLocation(@base.Span.Value);
                NextToken();
                var exp = ParseExpression();
                Expect(TToken.RSquare);
                @base = FinishExp(new Ast.IndexExpression(@base, exp));
            }
            else break;
        }
        var argumentsList = new List<Ast.Expression>();
        if (Consume(TToken.LParen)) {
            do {
                if (Token.Type == TToken.RParen) break;
                argumentsList.Add(ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction));
            } while (Consume(TToken.Comma));
            Expect(TToken.RParen);
        }
        return FinishExp(new Ast.NewExpression(@base, argumentsList));
    }

    // assumes previousToken is the starting identifier
    private Ast.Expression ParseIdentifierPrimaryExpression(bool allowTypedId = true, OperatorPrecedence minPrecedence = null) {
        minPrecedence = minPrecedence ?? OperatorPrecedence.Postfix;
        PushLocation(PreviousToken.Span);
        var name = PreviousToken.StringValue;
        if (allowTypedId && Consume(TToken.Colon)) {
            return FinishExp(new Ast.Identifier(name, ParseTypeExpression()));
        } else if (Token.Type == TToken.StringLiteral && PreviousToken.IsContextKeyword("embed")) {
            NextToken();
            var source = PreviousToken.StringValue;
            var embedType = Consume(TToken.Colon) ? ParseTypeExpression() : null;
            return FinishExp(new Ast.EmbedExpression(source, embedType));
        }
        var r = FinishExp(new Ast.Identifier(name));
        if (Consume(TToken.FatArrow)) {
            PushLocation(r.Span.Value);
            var (@params, optParams) = ConvertExpsIntoArrowFParams(new List<Ast.Expression>{r});
            r = ParseArrowFunctionExpression(@params, null, null);
        }
        return r;
    }

    private Ast.Expression ParseParensExpOrArrowF() {
        var expressions = new List<Ast.Expression>();
        do {
            if (Consume(TToken.Ellipsis)) {
                var (@params, optParams) = ConvertExpsIntoArrowFParams(expressions);
                var restParam = ParseVariableBinding();
                if (restParam.Init != null) SyntaxError(29, restParam.Span.Value);
                Expect(TToken.RParen);
                var returnType = Consume(TToken.Colon) ? ParseTypeExpression() : null;
                Expect(TToken.FatArrow);
                return ParseArrowFunctionExpression(@params, optParams, restParam, returnType);
            }
            expressions.Add(ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction));
        } while (Consume(TToken.Comma));
        Expect(TToken.RParen);
        if (Consume(TToken.Colon)) {
            var (@params, optParams) = ConvertExpsIntoArrowFParams(expressions);
            var returnType = ParseTypeExpression();
            Expect(TToken.FatArrow);
            return ParseArrowFunctionExpression(@params, optParams, null, returnType);
        }
        if (Consume(TToken.FatArrow)) {
            var (@params, optParams) = ConvertExpsIntoArrowFParams(expressions);
            return ParseArrowFunctionExpression(@params, optParams, null, null);
        }
        Ast.Expression exp = null;
        if (expressions.Count == 1) {
            exp = expressions[0];
        } else {
            var startSpan = expressions[0].Span.Value;
            exp = new Ast.ListExpression(expressions);
            exp.Span = startSpan.To(expressions.Last().Span.Value);
        }
        return FinishExp(new Ast.ParensExpression(exp));
    }

    private (List<Ast.VariableBinding>, List<Ast.VariableBinding>) ConvertExpsIntoArrowFParams(List<Ast.Expression> expressions) {
        List<Ast.VariableBinding> @params = null;
        List<Ast.VariableBinding> optParams = null;
        foreach (var exp in expressions) {
            if (exp is Ast.AssignmentExpression exp_asAe) {
                if (exp_asAe.Compound != null) {
                    SyntaxError(28, exp.Span.Value);
                } else {
                    var r = ConvertExpIntoArrowFParam(exp_asAe.Left, exp_asAe.Right, @params, optParams);
                    @params = r.Item1;
                    optParams = r.Item2;
                }
            } else {
                var r = ConvertExpIntoArrowFParam(exp, null, @params, optParams);
                @params = r.Item1;
                optParams = r.Item2;
            }
        }
        return (@params, optParams);
    }

    private (List<Ast.VariableBinding>, List<Ast.VariableBinding>) ConvertExpIntoArrowFParam(Ast.Node node, Ast.Expression init, List<Ast.VariableBinding> outParams, List<Ast.VariableBinding> outOptParams) {
        if (init != null) {
            PushLocation(node.Span.Value);
            var pattern = (node is Ast.DestructuringPattern) ? ((Ast.DestructuringPattern) node) : ConvertExpressionIntoDestructuringPattern((Ast.Expression) node);
            outOptParams = outOptParams ?? new List<Ast.VariableBinding>{};
            outOptParams.Add((Ast.VariableBinding) FinishNode(new Ast.VariableBinding(pattern, init), node.Span.Value.To(init.Span.Value)));
        } else {
            PushLocation(node.Span.Value);
            var pattern = ConvertExpressionIntoDestructuringPattern((Ast.Expression) node);
            outParams = outParams ?? new List<Ast.VariableBinding>{};
            outParams.Add((Ast.VariableBinding) FinishNode(new Ast.VariableBinding(pattern, null), node.Span.Value));
            if (outOptParams != null) {
                SyntaxError(4, node.Span.Value);
            }
        }
        return (outParams, outOptParams);
    }

    // parses arrow function, assuming the previous token is `=>`
    // and that the starting location was pushed to the stack.
    private Ast.Expression ParseArrowFunctionExpression(List<Ast.VariableBinding> @params, List<Ast.VariableBinding> optParams, Ast.VariableBinding restParam, Ast.TypeExpression returnType = null) {
        var sf = new StackFunction();
        FunctionStack.Push(sf);
        Ast.Node body = Token.Type == TToken.LCurly
            ? ParseBlock(new ConstructorContext())
            : ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction);
        FunctionStack.Pop();
        DuplicateLocation();
        var common = (Ast.FunctionCommon) FinishNode(new Ast.FunctionCommon(sf.UsesAwait, sf.UsesYield, @params, optParams, restParam, returnType, body));
        return FinishExp(new Ast.FunctionExpression(null, common));
    }

    private Ast.Expression ParseMarkupInitializer(bool root = false) {
        MarkLocation();
        ExpectOperator(Operator.Lt);
        if (root && ConsumeOperator(Operator.Gt)) {
            var children2 = new List<Ast.Expression>{};
            while (!Consume(TToken.LtSlash)) {
                if (Token.Type == TToken.LCurly) {
                    MarkLocation();
                    NextToken();
                    var expr = ParseExpression();
                    Expect(TToken.RCurly);
                    children2.Add(FinishExp(new Ast.Spread(expr)));
                } else {
                    children2.Add(FinishExp(ParseMarkupInitializer()));
                }
            }
            ExpectOperator(Operator.Gt);
            return FinishExp(new Ast.MarkupListInitializer(children2));
        }
        MarkLocation();
        var id = FinishExp(new Ast.Identifier(ExpectIdentifier()));
        while (Consume(TToken.Colon) || Consume(TToken.Dot)) {
            PushLocation(id.Span.Value);
            id = (Ast.Expression) FinishNode(new Ast.MemberExpression(id, ParseIdentifier()));
        }
        var attribs = new List<Ast.MarkupAttribute>{};
        while (!Token.IsOperator(Operator.Divide) && !Token.IsOperator(Operator.Gt)) {
            MarkLocation();
            DuplicateLocation();
            var id2 = (Ast.Identifier) FinishNode(new Ast.Identifier(ExpectIdentifier()));
            Ast.Expression value = null;
            if (Consume(TToken.Assign)) {
                if (Consume(TToken.LCurly)) {
                    value = ParseExpression();
                    Expect(TToken.RCurly);
                } else {
                    value = ParseOptPrimaryExpression(false);
                    if (value == null) ThrowUnexpected();
                }
            }
            var attrib = (Ast.MarkupAttribute) FinishNode(new Ast.MarkupAttribute(id2, value));
            attribs.Add(attrib);
        }
        List<Ast.Expression> children = null;
        if (ConsumeOperator(Operator.Divide)) {
            ExpectOperator(Operator.Gt);
        } else {
            ExpectOperator(Operator.Gt);
            children = new List<Ast.Expression>{};
            while (!Consume(TToken.LtSlash)) {
                if (Token.Type == TToken.LCurly) {
                    MarkLocation();
                    NextToken();
                    var expr2 = ParseExpression();
                    Expect(TToken.RCurly);
                    children.Add(FinishExp(new Ast.Spread(expr2)));
                } else {
                    children.Add(ParseMarkupInitializer());
                }
            }
            ExpectIdentifier();
            while (Consume(TToken.Colon) || Consume(TToken.Dot)) {
                ExpectIdentifier();
            }
            ExpectOperator(Operator.Gt);
        }
        return FinishExp(new Ast.MarkupInitializer(id, attribs, children));
    }

    private static Dictionary<Operator, OperatorFilter> m_UnaryOperatorFiltersByOperator = null;
    private static Dictionary<string, OperatorFilter> m_UnaryOperatorFiltersByKeyword = null;
    private static Dictionary<Token, OperatorFilter> m_UnaryOperatorFiltersByTokenType = null;

    private OperatorFilter? FilterUnaryOperator() {
        if (Token.Type == TToken.Operator) {
            if (m_UnaryOperatorFiltersByOperator == null) {
                m_UnaryOperatorFiltersByOperator = new Dictionary<Operator, OperatorFilter> {
                    [Operator.Add] = new OperatorFilter(Operator.Positive, OperatorPrecedence.Unary, OperatorPrecedence.Unary),
                    [Operator.Subtract] = new OperatorFilter(Operator.Negate, OperatorPrecedence.Unary, OperatorPrecedence.Unary),
                    [Operator.BitwiseNot] = new OperatorFilter(Operator.BitwiseNot, OperatorPrecedence.Unary, OperatorPrecedence.Unary),
                    [Operator.PreIncrement] = new OperatorFilter(Operator.PreIncrement, OperatorPrecedence.Unary, OperatorPrecedence.Postfix),
                    [Operator.PreDecrement] = new OperatorFilter(Operator.PreDecrement, OperatorPrecedence.Unary, OperatorPrecedence.Postfix),
                };
            }
            return m_UnaryOperatorFiltersByOperator.ContainsKey(Token.Operator) ? m_UnaryOperatorFiltersByOperator[Token.Operator] : null;
        }
        else if (Token.Type == TToken.Keyword) {
            if (m_UnaryOperatorFiltersByKeyword == null) {
                m_UnaryOperatorFiltersByKeyword = new Dictionary<string, OperatorFilter> {
                    ["await"] = new OperatorFilter(Operator.Await, OperatorPrecedence.Unary, OperatorPrecedence.Unary),
                    ["yield"] = new OperatorFilter(Operator.Yield, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction),
                    ["delete"] = new OperatorFilter(Operator.Delete, OperatorPrecedence.Unary, OperatorPrecedence.Postfix),
                    ["typeof"] = new OperatorFilter(Operator.Typeof, OperatorPrecedence.Unary, OperatorPrecedence.Unary),
                    ["void"] = new OperatorFilter(Operator.Void, OperatorPrecedence.Unary, OperatorPrecedence.Unary),
                };
            }
            return m_UnaryOperatorFiltersByKeyword.ContainsKey(Token.StringValue) ? m_UnaryOperatorFiltersByKeyword[Token.StringValue] : null;
        }
        else {
            if (m_UnaryOperatorFiltersByTokenType == null) {
                m_UnaryOperatorFiltersByTokenType = new Dictionary<TToken, OperatorFilter> {
                    [TToken.ExclamationMark] = new OperatorFilter(Operator.LogicalNot, OperatorPrecedence.Unary, OperatorPrecedence.Unary),
                };
            }
            return m_UnaryOperatorFiltersByTokenType.ContainsKey(Token.Type) ? m_UnaryOperatorFiltersByTokenType[Token.Type] : null;
        }
    }

    private static Dictionary<Operator, OperatorFilter> m_BinaryOperatorFiltersByOperator = null;

    public OperatorFilter? FilterBinaryOperator() {
        if (Token.Type == TToken.Operator) {
            if (m_BinaryOperatorFiltersByOperator == null) {
                m_BinaryOperatorFiltersByOperator = new Dictionary<Operator, OperatorFilter> {
                    [Operator.Add] = new OperatorFilter(Operator.Add, OperatorPrecedence.Additive, OperatorPrecedence.Multiplicative),
                    [Operator.Subtract] = new OperatorFilter(Operator.Subtract, OperatorPrecedence.Additive, OperatorPrecedence.Multiplicative),
                    [Operator.Multiply] = new OperatorFilter(Operator.Multiply, OperatorPrecedence.Multiplicative, OperatorPrecedence.Exponentiation),
                    [Operator.Divide] = new OperatorFilter(Operator.Divide, OperatorPrecedence.Multiplicative, OperatorPrecedence.Exponentiation),
                    [Operator.Remainder] = new OperatorFilter(Operator.Remainder, OperatorPrecedence.Multiplicative, OperatorPrecedence.Exponentiation),
                    [Operator.Pow] = new OperatorFilter(Operator.Pow, OperatorPrecedence.Exponentiation, OperatorPrecedence.Exponentiation),
                    [Operator.LogicalAnd] = new OperatorFilter(Operator.LogicalAnd, OperatorPrecedence.LogicalAnd, OperatorPrecedence.BitwiseOr),
                    [Operator.LogicalXor] = new OperatorFilter(Operator.LogicalXor, OperatorPrecedence.LogicalXor, OperatorPrecedence.LogicalAnd),
                    [Operator.LogicalOr] = new OperatorFilter(Operator.LogicalOr, OperatorPrecedence.LogicalOr, OperatorPrecedence.LogicalXor),
                    [Operator.NullCoalescing] = new OperatorFilter(Operator.NullCoalescing, OperatorPrecedence.NullCoalescing, OperatorPrecedence.LogicalOr),
                    [Operator.BitwiseAnd] = new OperatorFilter(Operator.BitwiseAnd, OperatorPrecedence.BitwiseAnd, OperatorPrecedence.Equality),
                    [Operator.BitwiseXor] = new OperatorFilter(Operator.BitwiseXor, OperatorPrecedence.BitwiseXor, OperatorPrecedence.BitwiseAnd),
                    [Operator.BitwiseOr] = new OperatorFilter(Operator.BitwiseOr, OperatorPrecedence.BitwiseOr, OperatorPrecedence.BitwiseXor),
                    [Operator.LeftShift] = new OperatorFilter(Operator.LeftShift, OperatorPrecedence.Shift, OperatorPrecedence.Additive),
                    [Operator.RightShift] = new OperatorFilter(Operator.RightShift, OperatorPrecedence.Shift, OperatorPrecedence.Additive),
                    [Operator.UnsignedRightShift] = new OperatorFilter(Operator.UnsignedRightShift, OperatorPrecedence.Shift, OperatorPrecedence.Additive),
                    [Operator.Equals] = new OperatorFilter(Operator.Equals, OperatorPrecedence.Equality, OperatorPrecedence.Relational),
                    [Operator.NotEquals] = new OperatorFilter(Operator.NotEquals, OperatorPrecedence.Equality, OperatorPrecedence.Relational),
                    [Operator.StrictEquals] = new OperatorFilter(Operator.StrictEquals, OperatorPrecedence.Equality, OperatorPrecedence.Relational),
                    [Operator.StrictNotEquals] = new OperatorFilter(Operator.StrictNotEquals, OperatorPrecedence.Equality, OperatorPrecedence.Relational),
                    [Operator.Lt] = new OperatorFilter(Operator.Lt, OperatorPrecedence.Relational, OperatorPrecedence.Shift),
                    [Operator.Gt] = new OperatorFilter(Operator.Gt, OperatorPrecedence.Relational, OperatorPrecedence.Shift),
                    [Operator.Le] = new OperatorFilter(Operator.Le, OperatorPrecedence.Relational, OperatorPrecedence.Shift),
                    [Operator.Ge] = new OperatorFilter(Operator.Ge, OperatorPrecedence.Relational, OperatorPrecedence.Shift),
                    [Operator.In] = new OperatorFilter(Operator.In, OperatorPrecedence.Relational, OperatorPrecedence.Shift),
                };
            }
            return m_BinaryOperatorFiltersByOperator.ContainsKey(Token.Operator) ? m_BinaryOperatorFiltersByOperator[Token.Operator] : null;
        }
        return null;
    }

    // determines whether current token is in same line as previous token
    // or if current token is at a new line with higher indentation
    // than previous token.
    private bool InlineOrAtHigherIndentLine {
        get => PreviousToken.FirstLine == Token.FirstLine || Script.GetLineIndent(PreviousToken.FirstLine) < Script.GetLineIndent(Token.FirstLine);
    }

    private Ast.Expression ParseSubexpressions(Ast.Expression r, bool allowIn = true, OperatorPrecedence minPrecedence = null) {
        minPrecedence = minPrecedence ?? OperatorPrecedence.List;
        for (;;) {
            var filter = FilterBinaryOperator();
            if (filter != null && minPrecedence.ValueOf() <= filter.Value.precedence.ValueOf()) {
                PushLocation(r.Span.Value);
                NextToken();
                r = FinishExp(new Ast.BinaryExpression(filter.Value.@operator, r, ParseExpression(allowIn, filter.Value.nextPrecedence)));
            // 'not in'
            } else if (minPrecedence.ValueOf() <= OperatorPrecedence.Relational.ValueOf() && this.Token.IsKeyword("not")) {
                PushLocation(r.Span.Value);
                NextToken();
                ExpectKeyword("in");
                r = FinishExp(new Ast.BinaryExpression(Operator.In, r, ParseExpression(allowIn, OperatorPrecedence.Shift), true));
            } else if (Token.Type == TToken.ExclamationMark && minPrecedence.ValueOf() <= OperatorPrecedence.Postfix.ValueOf() && PreviousToken.LastLine == Token.FirstLine) {
                PushLocation(r.Span.Value);
                NextToken();
                r = FinishExp(new Ast.UnaryExpression(Operator.NonNull, r));
            } else if (Token.IsOperator(Operator.PreIncrement) && minPrecedence.ValueOf() <= OperatorPrecedence.Postfix.ValueOf() && PreviousToken.LastLine == Token.FirstLine) {
                PushLocation(r.Span.Value);
                NextToken();
                r = FinishExp(new Ast.UnaryExpression(Operator.PostIncrement, r));
            } else if (Token.IsOperator(Operator.PreDecrement) && minPrecedence.ValueOf() <= OperatorPrecedence.Postfix.ValueOf() && PreviousToken.LastLine == Token.FirstLine) {
                PushLocation(r.Span.Value);
                NextToken();
                r = FinishExp(new Ast.UnaryExpression(Operator.PostDecrement, r));
            } else if (Token.IsKeyword("as") && minPrecedence.ValueOf() <= OperatorPrecedence.Relational.ValueOf()) {
                PushLocation(r.Span.Value);
                NextToken();
                var strict = Consume(TToken.ExclamationMark);
                if (!strict) Consume(TToken.QuestionMark);
                r = FinishExp(new Ast.TypeBinaryExpression(strict ? Operator.AsStrict : Operator.As, r, ParseTypeExpression(), null));
            } else if (Token.IsKeyword("instanceof") && minPrecedence.ValueOf() <= OperatorPrecedence.Relational.ValueOf()) {
                PushLocation(r.Span.Value);
                NextToken();
                r = FinishExp(new Ast.TypeBinaryExpression(Operator.Instanceof, r, ParseTypeExpression(), null));
            } else if (Token.IsKeyword("is") && minPrecedence.ValueOf() <= OperatorPrecedence.Relational.ValueOf()) {
                PushLocation(r.Span.Value);
                NextToken();
                var isOpRight = ParseTypeExpression();
                Ast.Identifier isOpBindsTo = null;
                if (isOpRight is Ast.TypedTypeExpression tte && (tte.Base is Ast.IdentifierTypeExpression)) {
                    isOpBindsTo = new Ast.Identifier(((Ast.IdentifierTypeExpression) tte.Base).Name);
                    isOpBindsTo.Span = tte.Base.Span;
                    isOpRight = tte.Type;
                }
                r = FinishExp(new Ast.TypeBinaryExpression(Operator.Is, r, isOpRight, isOpBindsTo));
            } else if (Token.Type == TToken.Assign && minPrecedence.ValueOf() <= OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction.ValueOf()) {
                PushLocation(r.Span.Value);
                NextToken();
                var leftAsPattern = r is Ast.ArrayInitializer || r is Ast.ObjectInitializer ? OptConvertExpressionIntoDestructuringPattern(r) : null;
                var left = (Ast.Node) leftAsPattern ?? r;
                r = FinishExp(new Ast.AssignmentExpression(left, null, ParseExpression(allowIn, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction)));
            } else if (Token.Type == TToken.CompoundAssignment && minPrecedence.ValueOf() <= OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction.ValueOf()) {
                PushLocation(r.Span.Value);
                var compound = Token.Operator;
                NextToken();
                r = FinishExp(new Ast.AssignmentExpression(r, compound, ParseExpression(allowIn, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction)));
            } else if (Token.Type == TToken.QuestionMark && minPrecedence.ValueOf() <= OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction.ValueOf()) {
                PushLocation(r.Span.Value);
                NextToken();
                var consequent = ParseExpression(allowIn, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction, false);
                Expect(TToken.Colon);
                var alternative = ParseExpression(allowIn, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction);
                r = FinishExp(new Ast.ConditionalExpression(r, consequent, alternative));
            } else if (Token.Type == TToken.Comma && minPrecedence.ValueOf() <= OperatorPrecedence.List.ValueOf()) {
                PushLocation(r.Span.Value);
                var expressions = new List<Ast.Expression>{r};
                while (Consume(TToken.Comma)) {
                    expressions.Add(ParseExpression(allowIn, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction));
                }
                r = FinishExp(new Ast.ListExpression(expressions));
            } else if (Token.Type == TToken.Dot && minPrecedence.ValueOf() <= OperatorPrecedence.Postfix.ValueOf()) {
                PushLocation(r.Span.Value);
                NextToken();
                if (ConsumeOperator(Operator.Lt)) {
                    var argumentsList = new List<Ast.TypeExpression>{};
                    do {
                        argumentsList.Add(ParseTypeExpression());
                    } while (Consume(TToken.Comma));
                    ExpectGT();
                    r = FinishExp(new Ast.ExpressionWithTypeArguments(r, argumentsList));
                } else {
                    var id = ParseIdentifier(true);
                    r = FinishExp(new Ast.MemberExpression(r, id));
                }
            } else if (Token.Type == TToken.LSquare && minPrecedence.ValueOf() <= OperatorPrecedence.Postfix.ValueOf() && InlineOrAtHigherIndentLine) {
                PushLocation(r.Span.Value);
                NextToken();
                var exp = ParseExpression();
                Expect(TToken.RSquare);
                r = FinishExp(new Ast.IndexExpression(r, exp));
            } else if (Token.Type == TToken.LParen && minPrecedence.ValueOf() <= OperatorPrecedence.Postfix.ValueOf()) {
                PushLocation(r.Span.Value);
                NextToken();
                var argumentsList = new List<Ast.Expression>{};
                do {
                    if (Token.Type == TToken.RParen) break;
                    argumentsList.Add(ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction));
                } while (Consume(TToken.Comma));
                Expect(TToken.RParen);
                r = FinishExp(new Ast.CallExpression(r, argumentsList));
            } else if (Token.Type == TToken.QuestionDot && minPrecedence.ValueOf() <= OperatorPrecedence.Postfix.ValueOf()) {
                PushLocation(r.Span.Value);
                NextToken();
                if (Consume(TToken.LParen)) {
                    var argumentsList = new List<Ast.Expression>{};
                    do {
                        if (Token.Type == TToken.RParen) break;
                        argumentsList.Add(ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction));
                    } while (Consume(TToken.Comma));
                    Expect(TToken.RParen);
                    DuplicateLocation();
                    DuplicateLocation();
                    var optChain = FinishExp(new Ast.OptionalChainingPlaceholder());
                    optChain = FinishExp(new Ast.CallExpression(optChain, argumentsList));
                    optChain = ParseSubexpressions(optChain, true, OperatorPrecedence.Postfix);
                    r = FinishExp(new Ast.OptChainingExpression(r, optChain));
                } else if (Consume(TToken.LSquare)) {
                    var exp = ParseExpression();
                    Expect(TToken.RSquare);
                    DuplicateLocation();
                    DuplicateLocation();
                    var optChain = FinishExp(new Ast.OptionalChainingPlaceholder());
                    optChain = FinishExp(new Ast.IndexExpression(optChain, exp));
                    optChain = ParseSubexpressions(optChain, true, OperatorPrecedence.Postfix);
                    r = FinishExp(new Ast.OptChainingExpression(r, optChain));
                } else {
                    var id = ParseIdentifier(true);
                    DuplicateLocation();
                    DuplicateLocation();
                    var optChain = FinishExp(new Ast.OptionalChainingPlaceholder());
                    optChain = FinishExp(new Ast.MemberExpression(optChain, id));
                    optChain = ParseSubexpressions(optChain, true, OperatorPrecedence.Postfix);
                    r = FinishExp(new Ast.OptChainingExpression(r, optChain));
                }
            } else break;
        }
        return r;
    }

    private Ast.Generics ParseOptGenericTypesDeclaration() {
        if (Token.Type != TToken.Dot) return null;
        MarkLocation();
        NextToken();
        ExpectOperator(Operator.Lt);
        var @params = new List<Ast.GenericTypeParameter>{};
        do {
            MarkLocation();
            var id = ParseIdentifier();
            var isBound = Consume(TToken.Colon) ? ParseTypeExpression() : null;
            @params.Add((Ast.GenericTypeParameter) FinishNode(new Ast.GenericTypeParameter(id, isBound)));
        } while (Consume(TToken.Comma));
        ExpectGT();
        return (Ast.Generics) FinishNode(new Ast.Generics(@params));
    }

    private (bool, Ast.Generics) ParseOptGenericBounds(Ast.Generics generics) {
        if (!ConsumeKeyword("where")) return (false, generics);
        do {
            MarkLocation();
            var id = ParseIdentifier();
            ExpectKeyword("is");
            var bound = (Ast.GenericTypeParameterBound) FinishNode(new Ast.GenericTypeParameterIsBound(id, ParseTypeExpression()));
            if (generics == null) {
                generics = new Ast.Generics(new List<Ast.GenericTypeParameter>{});
                PushLocation(id.Span.Value);
                FinishNode(generics);
            }
            generics.Bounds = generics.Bounds ?? new List<Ast.GenericTypeParameterBound>{};
            generics.Bounds.Add(bound);
        } while (Consume(TToken.Comma));
        return (true, generics);
    }

    private (Ast.FunctionCommon common, bool semicolonInserted) ParseFunctionCommon(Ast.Generics generics = null, bool forFunctionDefinition = false, bool isConstructor = false) {
        MarkLocation();
        Expect(TToken.LParen);
        List<Ast.VariableBinding> @params = null;
        List<Ast.VariableBinding> optParams = null;
        Ast.VariableBinding restParam = null;
        do {
            if (Token.Type == TToken.RParen) break;
            if (Consume(TToken.Ellipsis)) {
                restParam = ParseVariableBinding();
                if (restParam.Init != null) SyntaxError(29, restParam.Span.Value);
                break;
            }
            var binding = ParseVariableBinding();
            if (binding.Init != null) {
                optParams ??= new List<Ast.VariableBinding>{};
                optParams.Add(binding);
            } else {
                if (optParams != null) SyntaxError(4, binding.Span.Value);
                @params ??= new List<Ast.VariableBinding>{};
                @params.Add(binding);
            }
        } while (Consume(TToken.Comma));
        Expect(TToken.RParen);
        var returnType = Consume(TToken.Colon) ? ParseTypeExpression() : null;
        for (;;) {
            var (gotWhereClause, generics2) = ParseOptGenericBounds(generics);
            generics = generics2;
            if (!gotWhereClause) break;
        }
        var sf = new StackFunction();
        FunctionStack.Push(sf);
        var (body, semicolonInserted) = ParseFunctionBody(sf, forFunctionDefinition, isConstructor);
        FunctionStack.Pop();
        return ((Ast.FunctionCommon) FinishNode(new Ast.FunctionCommon(sf.UsesAwait, sf.UsesYield, @params, optParams, restParam, returnType, body)), semicolonInserted);
    }

    private (Ast.Node body, bool semicolonInserted) ParseFunctionBody(StackFunction stackFunction, bool forFunctionDefinition, bool isConstructor) {
        var context = new ConstructorContext();
        context.IsConstructor = isConstructor;
        var block = Token.Type == TToken.LCurly ? ParseBlock(context) : null;
        if (block != null) return (block, true);
        var semicolonInserted = forFunctionDefinition && Token.Type != TToken.LParen ? ParseSemicolon() : false;
        if (semicolonInserted) return (null, true);
        var exp = ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction);
        semicolonInserted = forFunctionDefinition ? ParseSemicolon() : semicolonInserted;
        return (exp, semicolonInserted);
    }

    // tries parsing semicolon. Differently from EcmaScript,
    // line terminator only causes a semicolon to be inserted if
    // current token has less than or the same number of
    // indentation spaces compared to the last line of
    // the previous token.
    private bool ParseSemicolon() {
        return Consume(TToken.Semicolon)
            || PreviousToken.Type == TToken.RCurly
            || (PreviousToken.LastLine != Token.FirstLine && Script.GetLineIndent(PreviousToken.FirstLine) >= Script.GetLineIndent(Token.FirstLine));
    }

    public (Ast.Statement node, bool semicolonInserted) ParseStatement(Context context = null) {
        context ??= new Context();
        var r = ParseOptStatement(context);
        if (r == null) ThrowUnexpected();
        return r.Value;
    }

    public (Ast.Statement node, bool semicolonInserted)? ParseOptStatement(Context context = null) {
        context ??= new Context();
        bool semicolonInserted = false;
        if (Token.Type == TToken.Keyword) {
            switch (Token.StringValue) {
                case "package": return ParsePackageStatement(context);
                case "super": return ParseSuperStatement(context);
                case "import": return ParseImportStatement();
                case "if": return ParseIfStatement(context.RetainLabelsOnly);
                case "do": return ParseDoStatement(context.RetainLabelsOnly);
                case "while": return ParseWhileStatement(context.RetainLabelsOnly);
                case "break": return ParseBreakStatement(context);
                case "continue": return ParseContinueStatement(context);
                case "return": return ParseReturnStatement();
                case "throw": return ParseThrowStatement();
                case "try": return ParseTryStatement(context.RetainLabelsOnly);
                case "for": return ParseForStatement(context.RetainLabelsOnly);
                case "switch": return ParseSwitchStatement(context);
                case "use": return ParseUseStatement(context);
                case "with": return ParseWithStatement(context);
            }
            if (FacingDefinitionStrictKeyword || TakeTokenAsStrictKeywordAccessModifier(Token) != null) {
                var startSpan = Token.Span;
                var attribs = new DefinitionAttributes();
                ParseDefinitionAttributes(attribs);
                if (FacingDefinitionStrictKeyword || FacingDefinitionContextKeyword)
                    NextToken();
                return ParseAnnotatableDefinition(attribs, context, startSpan);
            }
        } else if (Consume(TToken.Identifier)) {
            var startSpan = PreviousToken.Span;
            var name = PreviousToken.StringValue;
            if (Consume(TToken.Colon)) {
                return ParseLabeledStatement(name, context, startSpan);
            } else if (Token.Type == TToken.StringLiteral && PreviousToken.IsContextKeyword("include") && TokenIsInline) {
                return ParseIncludeStatement(context, startSpan);
            } else if (Token.Type == TToken.Identifier && PreviousTokenIsDefinitionContextKeyword) {
                return ParseAnnotatableDefinition(new DefinitionAttributes(), context, startSpan);
            } else {
                var modifier = (Token.Type == TToken.Identifier || FacingDefinitionStrictKeyword || TakeTokenAsStrictKeywordAccessModifier(Token) != null) && TokenIsInline ? TakeTokenAsContextKeywordModifier(PreviousToken) : null;
                if (modifier != null) {
                    var attribs = new DefinitionAttributes();
                    attribs.Modifiers |= modifier.Value;
                    ParseDefinitionAttributes(attribs);
                    if (FacingDefinitionStrictKeyword || FacingDefinitionContextKeyword)
                        NextToken();
                    return ParseAnnotatableDefinition(attribs, context, startSpan);
                }
                if (TokenIsInline && PreviousTokenIsDefinitionContextKeyword)
                    return ParseAnnotatableDefinition(new DefinitionAttributes(), context, startSpan);
                var exp = ParseIdentifierPrimaryExpression();
                exp = ParseSubexpressions(exp);
                semicolonInserted = ParseSemicolon();
                PushLocation(exp.Span.Value);
                return (FinishStatement(new Ast.ExpressionStatement(exp)), semicolonInserted);
            }
        } else if (Token.Type == TToken.LCurly) {
            return (ParseBlock(context.RetainLabelsOnly), true);
        } else if (Consume(TToken.Semicolon)) {
            PushLocation(PreviousToken.Span);
            return (FinishStatement(new Ast.EmptyStatement()), true);
        } else if (Token.Type == TToken.LSquare) {
            return ParseLSquareStartedStatement(context);
        }
        var es_exp = ParseOptExpression();
        if (es_exp != null) {
            PushLocation(es_exp.Span.Value);
            semicolonInserted = ParseSemicolon();
            return (FinishStatement(new Ast.ExpressionStatement(es_exp)), semicolonInserted);
        }
        return null;
    }

    private (Ast.Statement node, bool semicolonInserted) ParsePackageStatement(Context context) {
        MarkLocation();
        NextToken();
        var id = new List<string>{};
        if (Consume(TToken.Identifier)) {
            id.Add(PreviousToken.StringValue);
            while (Consume(TToken.Dot)) {
                id.Add(ExpectIdentifier(true));
            }
        }
        var r = ((Ast.PackageDefinition) FinishNode(new Ast.PackageDefinition(id.ToArray(), ParsePackageBlock())));
        r.Block.ConsumeStrictnessPragmas();
        var atTopLevelOrPackage = context is TopLevelContext || context is PackageContext;
        if (!atTopLevelOrPackage) {
            SyntaxError(36, r.Span.Value);
        }
        return (r, true);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseIncludeStatement(Context context, Span startSpan) {
        PushLocation(startSpan);
        var src = Token.StringValue;
        NextToken();
        var semicolonInserted = ParseSemicolon();
        var r = (Ast.IncludeStatement) FinishStatement(new Ast.IncludeStatement(src));
        r.ConsumeStrictnessPragmas();

        if (Script.FilePath != null) {
            string innerSource = "";
            Script innerScript = null;
            var innerStatements = new List<Ast.Statement>{};
            var resolvedPath = "";
            if (src.StartsWith(".")) {
                resolvedPath = this.ResolveIncludePath(src);
                try {
                    innerSource = File.ReadAllText(resolvedPath);
                    if (this.AlreadyIncludedFilePaths.Contains(resolvedPath)) {
                        VerifyError(39, r.Span.Value, new DiagnosticArguments {["path"] = resolvedPath});
                        innerSource = "";
                    }
                } catch (IOException) {
                    VerifyError(25, r.Span.Value);
                }
            }
            else {
                VerifyError(40, r.Span.Value);
            }
            innerScript = new Script(resolvedPath, innerSource);
            try {
                var tokenizer = new Tokenizer(innerScript);
                tokenizer.Tokenize();
                var innerParser = new ParserBackend(innerScript, tokenizer);
                this.AlreadyIncludedFilePaths.Add(innerScript.FilePath);
                innerParser.AlreadyIncludedFilePaths = this.AlreadyIncludedFilePaths;
                innerParser.FunctionStack = this.FunctionStack;
                while (innerParser.Token.Type != TToken.Eof) {
                    var stmt = innerParser.ParseOptStatement(context);
                    if (stmt != null) innerStatements.Add(stmt.Value.node);
                    if (stmt == null || !stmt.Value.semicolonInserted) break;
                }
                if (innerParser.Token.Type != TToken.Eof) innerParser.ThrowUnexpected();
            } catch (ParseException) {
            }

            r.InnerScript = innerScript;
            r.InnerStatements = innerStatements;
            if (!innerScript.IsValid) {
                this.Script.Invalidate();
            }
            this.Script.AddIncludedScript(innerScript);
        } else {
            VerifyError(25, r.Span.Value);
        }

        return (r, semicolonInserted);
    }

    private string ResolveIncludePath(string src) {
        var resolvedPath = Path.Combine(Path.GetDirectoryName(Script.FilePath), src);
        // directory/index.vs
        try {
            resolvedPath = File.GetAttributes(resolvedPath).HasFlag(FileAttributes.Directory) ? Path.Combine(resolvedPath, "./index.vs") : resolvedPath;
        } catch {
        }
        resolvedPath = Path.GetFullPath(Path.ChangeExtension(resolvedPath, ".vs"));
        return resolvedPath;
    }

    private bool TokenIsInline {
        get => PreviousToken.LastLine == Token.FirstLine;
    }

    private static bool IsExpAValidDecorator(Ast.Expression exp) {
        if (exp is Ast.ArrayInitializer && ((Ast.ArrayInitializer) exp).Type == null)
            return true;
        if (exp is Ast.IndexExpression exp_asIe) {
            return IsExpAValidDecorator(exp_asIe.Base);
        }
        return false;
    }

    private static Ast.Expression TransformDecoratorLiteral(Ast.Expression exp) {
        if (exp is Ast.CallExpression callExp) {
            if (callExp.ArgumentsList.Count == 0 || (callExp.ArgumentsList[0] is Ast.ObjectInitializer)) {
                return exp;
            }
            var assignments = callExp.ArgumentsList.Where(e => e is Ast.AssignmentExpression ae && ae.Left is Ast.Identifier && ae.Compound == null).ToArray();
            if (assignments.Count() != callExp.ArgumentsList.Count()) {
                return exp;
            }
            var fields = new List<Ast.Node>{};
            foreach (var assign in assignments) {
                var assignAe = (Ast.AssignmentExpression) assign;
                var strLtr = new Ast.StringLiteral(((Ast.Identifier) assignAe.Left).Name);
                strLtr.Span = assignAe.Left.Span;
                var field = new Ast.ObjectField(strLtr, assignAe.Right);
                field.Span = assign.Span;
                fields.Add(field);
            }
            var objectInit = new Ast.ObjectInitializer(fields, null);
            objectInit.Span = assignments[0].Span.Value.To(assignments.Last().Span.Value);
            var newCallExp = new Ast.CallExpression(callExp.Base, new List<Ast.Expression>{objectInit});
            newCallExp.Span = callExp.Span;
            return newCallExp;
        }
        return exp;
    }

    private static void ExtractDecorators(Ast.Expression exp, List<Ast.Expression> into) {
        if (exp is Ast.ArrayInitializer exp_asAi) {
            foreach (var item in exp_asAi.Items) {
                if (!(item is Ast.Spread)) into.Add(TransformDecoratorLiteral(item));
            }
        } else {
            var exp_asIe = (Ast.IndexExpression) exp;
            ExtractDecorators(exp_asIe.Base, into);
            var k = exp_asIe.Key;
            if (k is Ast.ListExpression le) {
                foreach (var exp2 in le.Expressions) into.Add(TransformDecoratorLiteral(exp2));
            } else into.Add(TransformDecoratorLiteral(k));
        }
    }

    private (Ast.Statement node, bool semicolonInserted) ParseLSquareStartedStatement(Context context) {
        var startSpan = Token.Span;
        var exp = ParseExpression();
        var exps = new List<Ast.Expression>{exp};
        var foundInvalidDecorator = !IsExpAValidDecorator(exp);
        while (Token.Type == TToken.LSquare) {
            exp = ParseExpression();
            exps.Add(exp);
            if (!IsExpAValidDecorator(exp)) {
                foundInvalidDecorator = true;
                break;
            }
        }
        if (!foundInvalidDecorator && (FacingDefinitionContextKeyword || FacingDefinitionStrictKeyword || TakeTokenAsContextKeywordModifier(Token) != null || TakeTokenAsStrictKeywordAccessModifier(Token) != null)) {
            var decorators = new List<Ast.Expression>{};
            foreach (var exp2 in exps) ExtractDecorators(exp2, decorators);
            var attribs = new DefinitionAttributes();
            attribs.Decorators = decorators;
            ParseDefinitionAttributes(attribs);
            if (FacingDefinitionContextKeyword || FacingDefinitionStrictKeyword)
                NextToken();
            return ParseAnnotatableDefinition(attribs, context, startSpan);
        } else {
            PushLocation(startSpan);
            DuplicateLocation();
            exp = FinishExp(new Ast.ListExpression(exps));
            var semicolonInserted = ParseSemicolon();
            return (FinishStatement(new Ast.ExpressionStatement(exp)), semicolonInserted);
        }
    }

    private static Dictionary<string, bool> m_DefinitionContextKeywords = new Dictionary<string, bool> {
        ["enum"] = true,
        ["namespace"] = true,
        ["type"] = true,
    };

    private bool PreviousTokenIsDefinitionContextKeyword {
        get {
            var l = PreviousToken.RawLength;
            if (Token.Type != TToken.Identifier) return false;
            var v = PreviousToken.StringValue;
            return m_DefinitionContextKeywords.ContainsKey(v) && l == v.Count();
        }
    }

    private bool FacingDefinitionContextKeyword {
        get {
            var l = Token.RawLength;
            if (Token.Type != TToken.Identifier) return false;
            var v = Token.StringValue;
            return m_DefinitionContextKeywords.ContainsKey(v) && l == v.Count();
        }
    }

    private static Dictionary<string, bool> m_DefinitionStrictKeywords = new Dictionary<string, bool> {
        ["class"] = true,
        ["function"] = true,
        ["interface"] = true,
        ["var"] = true,
        ["const"] = true,
    };

    private bool FacingDefinitionStrictKeyword {
        get {
            if (Token.Type != TToken.Keyword) return false;
            var v = Token.StringValue;
            return m_DefinitionStrictKeywords.ContainsKey(v);
        }
    }

    private Ast.AnnotatableDefinitionModifier? TakeTokenAsContextKeywordModifier(TokenData tokenData) {
        var v = tokenData.StringValue;
        if (tokenData.Type != TToken.Identifier || tokenData.RawLength != v.Count()) {
            return null;
        }
        return v == "final" ? Ast.AnnotatableDefinitionModifier.Final
            : v == "native" ? Ast.AnnotatableDefinitionModifier.Native
            : v == "override" ? Ast.AnnotatableDefinitionModifier.Override
            : v == "proxy" ? Ast.AnnotatableDefinitionModifier.Proxy
            : v == "static" ? Ast.AnnotatableDefinitionModifier.Static
            : null;
    }

    private Ast.AnnotatableDefinitionAccessModifier? TakeTokenAsStrictKeywordAccessModifier(TokenData tokenData) {
        if (tokenData.Type != TToken.Keyword) {
            return null;
        }
        var v = tokenData.StringValue;
        return v == "public" ? Ast.AnnotatableDefinitionAccessModifier.Public
            : v == "private" ? Ast.AnnotatableDefinitionAccessModifier.Private
            : v == "protected" ? Ast.AnnotatableDefinitionAccessModifier.Protected
            : v == "internal" ? Ast.AnnotatableDefinitionAccessModifier.Internal
            : null;
    }

    private DefinitionAttributes ParseDefinitionAttributes(DefinitionAttributes attribs) {
        var empty = attribs.HasEmptyModifiers;
        for (;;) {
            if (!empty && !TokenIsInline) SyntaxError(13, Token.Span);
            var modifier = TakeTokenAsContextKeywordModifier(Token);
            if (modifier != null) {
                NextToken();
                attribs.Modifiers |= modifier.Value;
                empty = false;
                continue;
            }
            var accessModifier = TakeTokenAsStrictKeywordAccessModifier(Token);
            if (accessModifier != null) {
                NextToken();
                attribs.AccessModifier = accessModifier;
                empty = false;
                continue;
            }
            break;
        }
        return attribs;
    }

    // parses annotatable definition, where the start keyword
    // is the previous token.
    private (Ast.Statement node, bool semicolonInserted) ParseAnnotatableDefinition(DefinitionAttributes attribs, Context context, Span startSpan) {
        var r = ParseOptAnnotatableDefinition(attribs, context, startSpan);
        if (r == null) ThrowUnexpected();
        return r.Value;
    }

    // tries parsing annotatable definition, where
    // the start keyword is the previous token.
    private (Ast.Statement node, bool semicolonInserted)? ParseOptAnnotatableDefinition(DefinitionAttributes attribs, Context context, Span startSpan) {
        if (PreviousToken.IsKeyword("var") || PreviousToken.IsKeyword("const"))
            return ParseVariableDefinition(attribs, context, startSpan);
        if (PreviousToken.IsKeyword("function"))
            return ParseFunctionDefinition(attribs, context, startSpan);
        if (PreviousToken.IsContextKeyword("namespace"))
            return ParseNamespaceDefinition(attribs, context, startSpan);
        if (PreviousToken.IsKeyword("class"))
            return ParseClassDefinition(attribs, context, startSpan);
        if (PreviousToken.IsKeyword("interface"))
            return ParseInterfaceDefinition(attribs, context, startSpan);
        if (PreviousToken.IsContextKeyword("enum"))
            return ParseEnumDefinition(attribs, context, startSpan);
        if (PreviousToken.IsContextKeyword("type"))
            return ParseTypeDefinition(attribs, context, startSpan);
        return null;
    }

    private Ast.AnnotatableDefinition FinishAnnotatableDefinition(Ast.AnnotatableDefinition node, DefinitionAttributes attribs) {
        node.Modifiers = attribs.Modifiers;
        node.AccessModifier = attribs.AccessModifier;
        if (attribs.Decorators != null) {
            // separate Metadata
            var d = attribs.Decorators
                .Where(e => e is Ast.CallExpression d_asCe
                    && d_asCe.Base is Ast.Identifier d_asId
                    && d_asId.Name == "Metadata"
                    && d_asCe.ArgumentsList.Count() == 1
                    && d_asCe.ArgumentsList[0] is Ast.ObjectInitializer)
                .ToArray();
            if (d.Count() > 0) {
                attribs.Decorators.Remove(d.First());
                node.Metadata = (Ast.ObjectInitializer) ((Ast.CallExpression) d.First()).ArgumentsList[0];
            }
            // separate Allow
            var allowDec = attribs.Decorators
                .Where(e => e is Ast.CallExpression d_asCe
                    && d_asCe.Base is Ast.Identifier d_asId
                    && d_asId.Name == "Allow")
                .ToArray();
            if (allowDec.Count() > 0) {
                attribs.Decorators.Remove(allowDec.First());
                node.AllowAttribute = (Ast.CallExpression) allowDec.First();
            }
            // separate Warn
            var warnDec = attribs.Decorators
                .Where(e => e is Ast.CallExpression d_asCe
                    && d_asCe.Base is Ast.Identifier d_asId
                    && d_asId.Name == "Warn")
                .ToArray();
            if (warnDec.Count() > 0) {
                attribs.Decorators.Remove(warnDec.First());
                node.WarnAttribute = (Ast.CallExpression) warnDec.First();
            }
            // separate FFI
            var ffiDec = attribs.Decorators
                .Where(e => e is Ast.CallExpression d_asCe
                    && d_asCe.Base is Ast.Identifier d_asId
                    && d_asId.Name == "FFI")
                .ToArray();
            if (ffiDec.Count() > 0) {
                attribs.Decorators.Remove(ffiDec.First());
                node.FfiAttribute = (Ast.CallExpression) ffiDec.First();
            }
        }
        node.Decorators = attribs.Decorators != null && attribs.Decorators.Count() > 0 ? attribs.Decorators : null;
        FinishNode(node);
        return node;
    }

    private void RestrictModifiers(Span span, Ast.AnnotatableDefinitionModifier actualModifiers, params Ast.AnnotatableDefinitionModifier[] modifiersToRestrict) {
        foreach (var m in modifiersToRestrict)
            if (actualModifiers.HasFlag(m)) {
                SyntaxError(18, span, new DiagnosticArguments {["m"] = m});
            }
    }

    private (Ast.Statement node, bool semicolonInserted) ParseVariableDefinition(DefinitionAttributes attribs, Context context, Span startSpan) {
        PushLocation(startSpan);
        var readOnly = PreviousToken.IsKeyword("const");
        var isStatic = attribs.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Static);
        var bindings = new List<Ast.VariableBinding>{};
        do {
            var binding = ParseVariableBinding();
            bindings.Add(binding);
        } while (Consume(TToken.Comma));
        var semicolonInserted = ParseSemicolon();
        var r = FinishAnnotatableDefinition(new Ast.VariableDefinition(readOnly, bindings), attribs);

        // enum context
        if (context is EnumContext && !isStatic) {
            VerifyError(14, r.Span.Value);
        } else if (context is InterfaceContext) {
            VerifyError(16, r.Span.Value);
        }

        RestrictModifiers(r.Span.Value, attribs.Modifiers,
            Ast.AnnotatableDefinitionModifier.Final,
            Ast.AnnotatableDefinitionModifier.Native,
            Ast.AnnotatableDefinitionModifier.Override,
            Ast.AnnotatableDefinitionModifier.Proxy
            /* Ast.AnnotatableDefinitionModifier.Static */
        );

        return (r, semicolonInserted);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseFunctionDefinition(DefinitionAttributes attribs, Context context, Span startSpan) {
        PushLocation(startSpan);
        var id = ParseIdentifier();
        if (Token.Type == TToken.Identifier && (PreviousToken.IsContextKeyword("get") || PreviousToken.IsContextKeyword("set"))) {
            var getter = PreviousToken.IsContextKeyword("get");
            id = ParseIdentifier();
            return ParseGetterOrSetterDefinition(attribs, context, id, getter);
        }
        if (context is ClassContext && id.Name == context.Name) {
            return ParseConstructorDefinition(attribs, id);
        }
        if (attribs.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Proxy)) {
            return ParseProxyDefinition(attribs, context, id);
        }

        var generics = ParseOptGenericTypesDeclaration();
        var (common, semicolonInserted) = ParseFunctionCommon(generics, true, false);
        var r = FinishAnnotatableDefinition(new Ast.FunctionDefinition(id, generics, common), attribs);

        if (context is InterfaceContext) {
            if (((int) attribs.Modifiers) != 0 || attribs.AccessModifier != null || (attribs.Decorators != null && attribs.Decorators.Count > 0)) {
                SyntaxError(21, id.Span.Value);
            }
            ((Ast.FunctionDefinition) r).AccessModifier = Ast.AnnotatableDefinitionAccessModifier.Public;
        } else {
            if (attribs.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Native)) {
                // native function must not have body
                if (common.Body != null) {
                    SyntaxError(19, id.Span.Value);
                }
            } else if (common.Body == null) {
                // non-native function must have body
                SyntaxError(20, id.Span.Value);
            }
        }

        /*
        RestrictModifiers(id.Span.Value, attribs.Modifiers,
            Ast.AnnotatableDefinitionModifier.Final,
            Ast.AnnotatableDefinitionModifier.Native,
            Ast.AnnotatableDefinitionModifier.Override,
            Ast.AnnotatableDefinitionModifier.Proxy,
            Ast.AnnotatableDefinitionModifier.Static
        );
        */

        return (r, semicolonInserted);
    }

    // constructor
    private (Ast.Statement node, bool semicolonInserted) ParseConstructorDefinition(DefinitionAttributes attribs, Ast.Identifier id) {
        var (common, semicolonInserted) = ParseFunctionCommon(null, true, true);
        var r = FinishAnnotatableDefinition(new Ast.ConstructorDefinition(id, common), attribs);

        if (attribs.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Native)) {
            // native function must not have body
            if (common.Body != null) {
                SyntaxError(19, id.Span.Value);
            }
        } else if (common.Body == null) {
            // non-native function must have body
            SyntaxError(20, id.Span.Value);
        }

        if (common.UsesAwait) {
            SyntaxError(26, id.Span.Value);
        }
        if (common.UsesYield) {
            SyntaxError(27, id.Span.Value);
        }

        RestrictModifiers(id.Span.Value, attribs.Modifiers,
            Ast.AnnotatableDefinitionModifier.Final,
            /* Ast.AnnotatableDefinitionModifier.Native, */
            Ast.AnnotatableDefinitionModifier.Override,
            Ast.AnnotatableDefinitionModifier.Proxy,
            Ast.AnnotatableDefinitionModifier.Static
        );

        return (r, semicolonInserted);
    }

    // getter or setter
    private (Ast.Statement node, bool semicolonInserted) ParseGetterOrSetterDefinition(DefinitionAttributes attribs, Context context, Ast.Identifier id, bool isGetter) {
        var (common, semicolonInserted) = ParseFunctionCommon(null, true, false);
        var r = FinishAnnotatableDefinition(isGetter ? new Ast.GetterDefinition(id, common) : new Ast.SetterDefinition(id, common), attribs);

        if (isGetter) {
            if (common.Params != null || common.OptParams != null || common.RestParam != null) {
                SyntaxError(34, id.Span.Value);
            }
        } else {
            if (common.OptParams != null || common.RestParam != null || common.Params == null || common.Params.Count() != 1) {
                SyntaxError(35, id.Span.Value);
            }
        }

        if (context is InterfaceContext) {
            if (((int) attribs.Modifiers) != 0 || attribs.AccessModifier != null || (attribs.Decorators != null && attribs.Decorators.Count > 0)) {
                SyntaxError(21, id.Span.Value);
            }
        } else {
            if (attribs.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Native)) {
                // native function must not have body
                if (common.Body != null) {
                    SyntaxError(19, id.Span.Value);
                }
            } else if (common.Body == null) {
                // non-native function must have body
                SyntaxError(20, id.Span.Value);
            }
        }

        if (common.UsesAwait) {
            SyntaxError(26, id.Span.Value);
        }
        if (common.UsesYield) {
            SyntaxError(27, id.Span.Value);
        }

        RestrictModifiers(id.Span.Value, attribs.Modifiers,
            /* Ast.AnnotatableDefinitionModifier.Final, */
            /* Ast.AnnotatableDefinitionModifier.Native, */
            /* Ast.AnnotatableDefinitionModifier.Override, */ 
            Ast.AnnotatableDefinitionModifier.Proxy
            /* Ast.AnnotatableDefinitionModifier.Static */
        );

        return (r, semicolonInserted);
    }

    private static Operator IdentifierAsProxyOperator(string id) {
        switch (id) {
            case "positive": return Operator.Positive;
            case "negate": return Operator.Negate;
            case "bitNot": return Operator.BitwiseNot;
            case "equals": return Operator.Equals;
            case "notEquals": return Operator.NotEquals;
            case "lt": return Operator.Lt;
            case "gt": return Operator.Gt;
            case "le": return Operator.Le;
            case "ge": return Operator.Ge;
            case "add": return Operator.Add;
            case "subtract": return Operator.Subtract;
            case "multiply": return Operator.Multiply;
            case "divide": return Operator.Divide;
            case "remainder": return Operator.Remainder;
            case "pow": return Operator.Pow;
            case "bitAnd": return Operator.BitwiseAnd;
            case "bitXor": return Operator.BitwiseXor;
            case "bitOr": return Operator.BitwiseOr;
            case "leftShift": return Operator.LeftShift;
            case "rightShift": return Operator.RightShift;
            case "unsignedRightShift": return Operator.UnsignedRightShift;
            case "getIndex": return Operator.ProxyToGetIndex;
            case "setIndex": return Operator.ProxyToSetIndex;
            case "deleteIndex": return Operator.ProxyToDeleteIndex;
            case "has": return Operator.In;
            case "iterateKeys": return Operator.ProxyToIterateKeys;
            case "iterateValues": return Operator.ProxyToIterateValues;
            case "convertImplicit": return Operator.ProxyToConvertImplicit;
            case "convertExplicit": return Operator.ProxyToConvertExplicit;
        }
        return null;
    }

    // proxy definition
    private (Ast.Statement node, bool semicolonInserted) ParseProxyDefinition(DefinitionAttributes attribs, Context context, Ast.Identifier id) {
        var (common, semicolonInserted) = ParseFunctionCommon(null, true, false);

        var @operator = IdentifierAsProxyOperator(id.Name);
        if (@operator == null) {
            VerifyError(23, id.Span.Value, new DiagnosticArguments { ["p"] = id.Name });
            return (FinishAnnotatableDefinition(new Ast.ProxyDefinition(id, Operator.In, common), attribs), semicolonInserted);
        }

        var r = FinishAnnotatableDefinition(new Ast.ProxyDefinition(id, @operator, common), attribs);

        if (context is ClassContext || context is EnumContext) {
            if (attribs.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Native)) {
                // native function must not have body
                if (common.Body != null) {
                    SyntaxError(19, id.Span.Value);
                }
            } else if (common.Body == null) {
                // non-native function must have body
                SyntaxError(20, id.Span.Value);
            }
        } else VerifyError(16, id.Span.Value);

        if (common.UsesAwait) {
            SyntaxError(26, id.Span.Value);
        }
        // only iterateKeys() and iterateValues() can yield
        if (common.UsesYield && @operator != Operator.ProxyToIterateKeys && @operator != Operator.ProxyToIterateValues) {
            SyntaxError(27, id.Span.Value);
        }

        RestrictModifiers(id.Span.Value, attribs.Modifiers,
            Ast.AnnotatableDefinitionModifier.Final,
            /* Ast.AnnotatableDefinitionModifier.Native, */
            Ast.AnnotatableDefinitionModifier.Override,
            /* Ast.AnnotatableDefinitionModifier.Proxy, */
            Ast.AnnotatableDefinitionModifier.Static
        );

        // validate number of parameters
        var proxyNumParams = @operator.ProxyNumberOfParameters;
        if (common.RestParam != null || common.OptParams != null || (proxyNumParams == 0 ? common.Params != null : (common.Params == null || common.Params.Count != proxyNumParams))) {
            VerifyError(22, id.Span.Value);
        }

        return (r, semicolonInserted);
    }

    // namespace
    private (Ast.Statement node, bool semicolonInserted) ParseNamespaceDefinition(DefinitionAttributes attribs, Context context, Span startSpan) {
        PushLocation(startSpan);
        var id = ParseIdentifier();
        if (Consume(TToken.Assign)) {
            return ParseNamespaceAliasDefinition(attribs, context, id);
        }
        var block = ParseBlock(new NamespaceContext());
        var r = FinishAnnotatableDefinition(new Ast.NamespaceDefinition(id, block), attribs);

        if (!(context is PackageContext || context is NamespaceContext || context is TopLevelContext)) {
            VerifyError(16, id.Span.Value);
        }

        RestrictModifiers(id.Span.Value, attribs.Modifiers,
            Ast.AnnotatableDefinitionModifier.Final,
            Ast.AnnotatableDefinitionModifier.Native,
            Ast.AnnotatableDefinitionModifier.Override,
            Ast.AnnotatableDefinitionModifier.Proxy,
            Ast.AnnotatableDefinitionModifier.Static
        );

        return (r, true);
    }

    // namespace alias
    private (Ast.Statement node, bool semicolonInserted) ParseNamespaceAliasDefinition(DefinitionAttributes attribs, Context context, Ast.Identifier id) {
        var exp = ParseExpression();
        var semicolonInserted = ParseSemicolon();
        var r = FinishAnnotatableDefinition(new Ast.NamespaceAliasDefinition(id, exp), attribs);

        if (context is InterfaceContext) {
            VerifyError(16, id.Span.Value);
        }

        RestrictModifiers(id.Span.Value, attribs.Modifiers,
            Ast.AnnotatableDefinitionModifier.Final,
            Ast.AnnotatableDefinitionModifier.Native,
            Ast.AnnotatableDefinitionModifier.Override,
            Ast.AnnotatableDefinitionModifier.Proxy,
            Ast.AnnotatableDefinitionModifier.Static
        );

        return (r, semicolonInserted);
    }

    // class
    private (Ast.Statement node, bool semicolonInserted) ParseClassDefinition(DefinitionAttributes attribs, Context context, Span startSpan) {
        PushLocation(startSpan);
        var id = ParseIdentifier();
        bool isValue = false;
        bool dontInit = false;
        if (attribs.Decorators != null) {
            IEnumerable<Ast.Expression> d = null;
            d = attribs.Decorators.Where(e => e is Ast.Identifier d_asId && d_asId.Name == "Value" && d_asId.Type == null);
            if (d.Count() > 0) {
                attribs.Decorators.Remove(d.First());
                isValue = true;
            }
            d = attribs.Decorators.Where(e => e is Ast.Identifier d_asId && d_asId.Name == "DontInit" && d_asId.Type == null);
            if (d.Count() > 0) {
                attribs.Decorators.Remove(d.First());
                dontInit = true;
            }
        }
        var generics = ParseOptGenericTypesDeclaration();
        Ast.TypeExpression extendsType = null;
        List<Ast.TypeExpression> implementsList = null;
        for  (;;) {
            if (ConsumeKeyword("extends")) {
                var extendsType2 = ParseTypeExpression();
                if (extendsType != null) SyntaxError(24, extendsType2.Span.Value);
                extendsType = extendsType2;
            } else if (ConsumeKeyword("implements")) {
                implementsList ??= new List<Ast.TypeExpression>{};
                do {
                    implementsList.Add(ParseTypeExpression());
                } while (Consume(TToken.Comma));
            } else {
                var (gotWhereClause, generics2) = ParseOptGenericBounds(generics);
                generics = generics2;
                if (!gotWhereClause) break;
            }
        }
        var block = ParseBlock(new ClassContext(id.Name));
        var r = FinishAnnotatableDefinition(new Ast.ClassDefinition(id, isValue, dontInit, generics, extendsType, implementsList, block), attribs);

        if (!(context is PackageContext || context is NamespaceContext || context is TopLevelContext)) {
            VerifyError(16, id.Span.Value);
        }

        if (isValue && extendsType != null) {
            VerifyError(30, id.Span.Value);
        }

        RestrictModifiers(id.Span.Value, attribs.Modifiers,
            /* Ast.AnnotatableDefinitionModifier.Final, */
            Ast.AnnotatableDefinitionModifier.Native,
            Ast.AnnotatableDefinitionModifier.Override,
            Ast.AnnotatableDefinitionModifier.Proxy,
            Ast.AnnotatableDefinitionModifier.Static
        );

        return (r, true);
    }

    // interface
    private (Ast.Statement node, bool semicolonInserted) ParseInterfaceDefinition(DefinitionAttributes attribs, Context context, Span startSpan) {
        PushLocation(startSpan);
        var id = ParseIdentifier();
        var generics = ParseOptGenericTypesDeclaration();
        List<Ast.TypeExpression> extendsList = null;
        for  (;;) {
            if (ConsumeKeyword("extends")) {
                extendsList ??= new List<Ast.TypeExpression>{};
                do {
                    extendsList.Add(ParseTypeExpression());
                } while (Consume(TToken.Comma));
            } else {
                var (gotWhereClause, generics2) = ParseOptGenericBounds(generics);
                generics = generics2;
                if (!gotWhereClause) break;
            }
        }
        var block = ParseBlock(new InterfaceContext());
        var r = FinishAnnotatableDefinition(new Ast.InterfaceDefinition(id, generics, extendsList, block), attribs);

        if (!(context is PackageContext || context is NamespaceContext || context is TopLevelContext)) {
            VerifyError(16, id.Span.Value);
        }

        RestrictModifiers(id.Span.Value, attribs.Modifiers,
            Ast.AnnotatableDefinitionModifier.Final,
            Ast.AnnotatableDefinitionModifier.Native,
            Ast.AnnotatableDefinitionModifier.Override,
            Ast.AnnotatableDefinitionModifier.Proxy,
            Ast.AnnotatableDefinitionModifier.Static
        );

        return (r, true);
    }

    // enum
    private (Ast.Statement node, bool semicolonInserted) ParseEnumDefinition(DefinitionAttributes attribs, Context context, Span startSpan) {
        PushLocation(startSpan);
        var id = ParseIdentifier();
        bool isFlags = false;
        if (attribs.Decorators != null) {
            var d = attribs.Decorators.Where(e => e is Ast.Identifier d_asId && d_asId.Name == "Flags" && d_asId.Type == null);
            if (d.Count() > 0) {
                attribs.Decorators.Remove(d.First());
                isFlags = true;
            }
        }
        Ast.TypeExpression numericType = ConsumeContextKeyword("wraps") ? ParseTypeExpression() : null;
        var block = ParseBlock(new EnumContext());
        for (int i = 0; i < block.Statements.Count(); ++i) {
            if (block.Statements[i] is Ast.ExpressionStatement expStmt) {
                if (expStmt.Expression is Ast.Identifier id2) {
                    PushLocation(id2.Span.Value);
                    block.Statements[i] = (Ast.Statement) FinishNode(new Ast.EnumVariantDefinition(id2, null), id2.Span.Value);
                } else if (expStmt.Expression is Ast.AssignmentExpression assignExp && assignExp.Compound == null) {
                    if (assignExp.Left is Ast.Identifier id3) {
                        PushLocation(id3.Span.Value);
                        block.Statements[i] = (Ast.Statement) FinishNode(new Ast.EnumVariantDefinition(id3, assignExp.Right), assignExp.Right.Span.Value);
                    }
                } else if (expStmt.Expression is Ast.ListExpression listExp) {
                    var variantExps = listExp.Expressions.Where(exp => {
                        return exp is Ast.Identifier
                            || (exp is Ast.AssignmentExpression assignExp && assignExp.Compound == null && assignExp.Left is Ast.Identifier);
                    });
                    if (variantExps.Count() == listExp.Expressions.Count()) {
                        IEnumerable<Ast.Statement> variants = variantExps.Select(exp => {
                            if (exp is Ast.Identifier id2) {
                                PushLocation(id2.Span.Value);
                                return (Ast.Statement) FinishNode(new Ast.EnumVariantDefinition(id2, null), id2.Span.Value);
                            } else {
                                var assignExp = (Ast.AssignmentExpression) exp;
                                var id3 = (Ast.Identifier) assignExp.Left;
                                PushLocation(id3.Span.Value);
                                return (Ast.Statement) FinishNode(new Ast.EnumVariantDefinition(id3, assignExp.Right), assignExp.Right.Span.Value);
                            }
                        }); // variants
                        block.Statements.RemoveAt(i);
                        block.Statements.InsertRange(i, variants);
                        i += variants.Count();
                        --i;
                    }
                }
            }
        }
        var r = FinishAnnotatableDefinition(new Ast.EnumDefinition(id, isFlags, numericType, block), attribs);

        if (!(context is PackageContext || context is NamespaceContext || context is TopLevelContext)) {
            VerifyError(16, id.Span.Value);
        }

        RestrictModifiers(id.Span.Value, attribs.Modifiers,
            Ast.AnnotatableDefinitionModifier.Final,
            Ast.AnnotatableDefinitionModifier.Native,
            Ast.AnnotatableDefinitionModifier.Override,
            Ast.AnnotatableDefinitionModifier.Proxy,
            Ast.AnnotatableDefinitionModifier.Static
        );

        return (r, true);
    }

    // type alias
    private (Ast.Statement node, bool semicolonInserted) ParseTypeDefinition(DefinitionAttributes attribs, Context context, Span startSpan) {
        PushLocation(startSpan);
        var wildcardSpan = Token.Span;
        var id = ParseIdentifier();
        var generics = ParseOptGenericTypesDeclaration();
        for (;;) {
            var (gotWhereClause, generics2) = ParseOptGenericBounds(generics);
            generics = generics2;
            if (!gotWhereClause) break;
        }
        Expect(TToken.Assign);
        var type = ParseTypeExpression();
        var semicolonInserted = ParseSemicolon();
        var r = FinishAnnotatableDefinition(new Ast.TypeDefinition(id, generics, type), attribs);

        if (context is InterfaceContext) {
            VerifyError(16, id.Span.Value);
        }

        RestrictModifiers(id.Span.Value, attribs.Modifiers,
            Ast.AnnotatableDefinitionModifier.Final,
            Ast.AnnotatableDefinitionModifier.Native,
            Ast.AnnotatableDefinitionModifier.Override,
            Ast.AnnotatableDefinitionModifier.Proxy,
            Ast.AnnotatableDefinitionModifier.Static
        );

        return (r, semicolonInserted);
    }

    private Ast.Block ParseBlock(Context context = null) {
        context ??= new Context();
        MarkLocation();
        Expect(TToken.LCurly);
        var statements = new List<Ast.Statement>{};
        while (Token.Type != TToken.RCurly) {
            var (stmt, semicolonInserted) = ParseStatement(context);
            statements.Add(stmt);
            if (!semicolonInserted) break;
        }
        Expect(TToken.RCurly);
        var r = (Ast.Block) FinishStatement(new Ast.Block(statements));
        r.ConsumeStrictnessPragmas();
        return r;
    }

    // a package block can omit the curly brackets.
    private Ast.Block ParsePackageBlock() {
        var context = new PackageContext();
        if (Token.Type == TToken.LCurly)
        {
            return ParseBlock(context);
        }
        MarkLocation();
        this.ParseSemicolon();
        var statements = new List<Ast.Statement>{};
        while (Token.Type != TToken.Eof) {
            var (stmt, semicolonInserted) = ParseStatement(context);
            statements.Add(stmt);
            if (!semicolonInserted) break;
        }
        Expect(TToken.Eof);
        return (Ast.Block) FinishStatement(new Ast.Block(statements));
    }

    private (Ast.Statement node, bool semicolonInserted) ParseLabeledStatement(string label, Context context, Span startSpan) {
        PushLocation(startSpan);
        var labeledContext = (context is LabeledStatementsContext ? context : new LabeledStatementsContext()).AddLabel(label);
        var (r, semicolonInserted) = ParseStatement(labeledContext);
        return (FinishStatement(new Ast.LabeledStatement(label, r)), semicolonInserted);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseSuperStatement(Context context) {
        MarkLocation();
        NextToken();
        if (Consume(TToken.LParen)) {
            var argumentsList = new List<Ast.Expression>();
            do {
                if (Token.Type == TToken.RParen) break;
                argumentsList.Add(ParseExpression(true, OperatorPrecedence.AssignmentOrConditionalOrYieldOrFunction));
            } while (Consume(TToken.Comma));
            Expect(TToken.RParen);
            var semicolonInserted = ParseSemicolon();
            var r = FinishStatement(new Ast.SuperStatement(argumentsList));
            if (!context.IsConstructor || context.SuperStatementFound) {
                SyntaxError(12, r.Span.Value);
            } else context.SuperStatementFound = true;
            return (r, semicolonInserted);
        } else {
            var exp = ParseSubexpressions(FinishExp(new Ast.SuperExpression()));
            PushLocation(exp.Span.Value);
            var semicolonInserted = ParseSemicolon();
            return (FinishStatement(new Ast.ExpressionStatement(exp)), semicolonInserted);
        }
    }

    private (Ast.Statement node, bool semicolonInserted) ParseImportStatement() {
        MarkLocation();
        NextToken();
        if (Consume(TToken.Dot)) {
            ExpectContextKeyword("meta");
            DuplicateLocation();
            var exp = FinishExp(new Ast.ImportMetaExpression());
            exp = ParseSubexpressions(exp);
            var semicolonInserted2 = ParseSemicolon();
            return (FinishStatement(new Ast.ExpressionStatement(exp)), semicolonInserted2);
        }
        var id = ParseIdentifier();
        Ast.Identifier alias = null;
        var importName = new List<string>{};
        bool wildcard = false;
        if (Token.Type == TToken.Dot) {
            importName.Add(id.Name);
        } else {
            alias = id;
            Expect(TToken.Assign);
            importName.Add(ParseIdentifier().Name);
            if (Token.Type != TToken.Dot) ThrowUnexpected();
        }
        while (Consume(TToken.Dot)) {
            if (ConsumeOperator(Operator.Multiply)) {
                wildcard = true;
                break;
            } else importName.Add(ParseIdentifier(true).Name);
        }
        var semicolonInserted = ParseSemicolon();
        return (FinishStatement(new Ast.ImportStatement(alias, importName.ToArray(), wildcard)), semicolonInserted);
    }

    private Ast.Identifier ParseIdentifier(bool keyword = false) {
        MarkLocation();
        var name = ExpectIdentifier(keyword);
        return (Ast.Identifier) FinishNode(new Ast.Identifier(name));
    }

    private (Ast.Statement node, bool semicolonInserted) ParseIfStatement(Context context) {
        MarkLocation();
        NextToken();
        Expect(TToken.LParen);
        Ast.Expression test = ParseExpression();
        Expect(TToken.RParen);
        var (consequent, semicolonInserted) = ParseStatement(context.RetainLabelsOnly);
        Ast.Statement alternative = null;
        if (semicolonInserted && ConsumeKeyword("else")) {
            var a = ParseStatement(context.RetainLabelsOnly);
            alternative = a.node;
            semicolonInserted = a.semicolonInserted;
        }
        return (FinishStatement(new Ast.IfStatement(test, consequent, alternative)), semicolonInserted);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseDoStatement(Context context) {
        MarkLocation();
        NextToken();
        var bodyContext = context.RetainLabelsOnly;
        bodyContext.InIterationStatement = true;
        var (body, semicolonInserted) = ParseStatement(bodyContext);
        if (!semicolonInserted) ThrowUnexpected();
        ExpectKeyword("while");
        Expect(TToken.LParen);
        var test = ParseExpression();
        Expect(TToken.RParen);
        return (FinishStatement(new Ast.DoStatement(body, test)), true);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseWhileStatement(Context context) {
        MarkLocation();
        NextToken();
        Expect(TToken.LParen);
        Ast.Expression test = ParseExpression();
        Expect(TToken.RParen);
        var bodyContext = context.RetainLabelsOnly;
        bodyContext.InIterationStatement = true;
        var (body, semicolonInserted) = ParseStatement(bodyContext);
        return (FinishStatement(new Ast.WhileStatement(test, body)), semicolonInserted);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseBreakStatement(Context context) {
        MarkLocation();
        NextToken();
        string label = PreviousToken.LastLine == Token.FirstLine && Consume(TToken.Identifier) ? PreviousToken.StringValue : null;
        var semicolonInserted = ParseSemicolon();
        var r = FinishStatement(new Ast.BreakStatement(label));
        if (label != null && !context.HasLabel(label)) {
            VerifyError(9, r.Span.Value, new DiagnosticArguments { ["label"] = label });
        } else if (label == null && !(context.InIterationStatement || context.InSwitchStatement)) {
            VerifyError(32, r.Span.Value);
        }
        return (r, semicolonInserted);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseContinueStatement(Context context) {
        MarkLocation();
        NextToken();
        string label = PreviousToken.LastLine == Token.FirstLine && Consume(TToken.Identifier) ? PreviousToken.StringValue : null;
        var semicolonInserted = ParseSemicolon();
        var r = FinishStatement(new Ast.ContinueStatement(label));
        if (label != null && !context.HasLabel(label)) {
            VerifyError(9, r.Span.Value, new DiagnosticArguments { ["label"] = label });
        } else if (label == null && !(context.InIterationStatement || context.InSwitchStatement)) {
            VerifyError(33, r.Span.Value);
        }
        return (r, semicolonInserted);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseReturnStatement() {
        MarkLocation();
        NextToken();
        Ast.Expression exp = null;
        var semicolonInserted = ParseSemicolon();
        if (!semicolonInserted && PreviousToken.LastLine == Token.FirstLine) {
            exp = ParseOptExpression();
            semicolonInserted = exp != null ? ParseSemicolon() : semicolonInserted;
        }
        var r = FinishStatement(new Ast.ReturnStatement(exp));
        if (this.FunctionStack.Count() == 0) {
            SyntaxError(37, r.Span.Value);
        }
        return (r, semicolonInserted);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseThrowStatement() {
        MarkLocation();
        NextToken();
        var exp = ParseExpression();
        var semicolonInserted = ParseSemicolon();
        return (FinishStatement(new Ast.ThrowStatement(exp)), semicolonInserted);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseTryStatement(Context context) {
        MarkLocation();
        NextToken();
        var block = ParseBlock(context.RetainLabelsOnly);
        var catchClauses = new List<Ast.CatchClause>{};
        while (Token.IsKeyword("catch")) {
            MarkLocation();
            NextToken();
            Expect(TToken.LParen);
            var pattern = ParseDestructuringPattern();
            Expect(TToken.RParen);
            var block2 = ParseBlock(context.RetainLabelsOnly);
            catchClauses.Add((Ast.CatchClause) FinishNode(new Ast.CatchClause(pattern, block2)));
        }
        var finallyBlock = ConsumeKeyword("finally") ? ParseBlock(context.RetainLabelsOnly) : null;
        var node = FinishStatement(new Ast.TryStatement(block, catchClauses, finallyBlock));
        if (catchClauses.Count == 0 && finallyBlock == null) {
            SyntaxError(10, node.Span.Value);
        }
        return (node, true);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseForStatement(Context context) {
        MarkLocation();
        NextToken();
        if (ConsumeContextKeyword("each")) {
            Expect(TToken.LParen);
            var left = ((Ast.Node) ParseOptSimpleVariableDeclaration(false)) ?? ((Ast.Node) ParseExpression(false, OperatorPrecedence.Postfix));
            ExpectKeyword("in");
            return ParseForInStatement(context, false, left);
        }
        Expect(TToken.LParen);

        Ast.Node initOrLeft = ParseOptSimpleVariableDeclaration(false);
        initOrLeft ??= ParseOptExpression(false, OperatorPrecedence.Postfix);
        if (initOrLeft != null && ConsumeKeyword("in")) {
            return ParseForInStatement(context, true, initOrLeft);
        }
        if (initOrLeft is Ast.Expression) {
            initOrLeft = ParseSubexpressions((Ast.Expression) initOrLeft, false);
        } else if (initOrLeft == null) {
            initOrLeft = ParseOptExpression(false);
        }

        Expect(TToken.Semicolon);
        var test = Token.Type != TToken.Semicolon ? ParseExpression() : null;
        Expect(TToken.Semicolon);
        var update = Token.Type != TToken.RParen ? ParseExpression() : null;
        Expect(TToken.RParen);
        var bodyContext = context.RetainLabelsOnly;
        bodyContext.InIterationStatement = true;
        var (body, semicolonInserted) = ParseStatement(bodyContext);
        return (FinishStatement(new Ast.ForStatement(initOrLeft, test, update, body)), semicolonInserted);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseForInStatement(Context context, bool iteratesKeys, Ast.Node left) {
        // ensure there is only one binding and no initializer
        var left_asSVD = left is Ast.SimpleVariableDeclaration ? ((Ast.SimpleVariableDeclaration) left) : null;
        if (left_asSVD != null && (left_asSVD.Bindings.Count > 1 || left_asSVD.Bindings[0].Init != null)) {
            SyntaxError(11, left_asSVD.Span.Value);
        }
        var right = ParseExpression();
        Expect(TToken.RParen);
        var bodyContext = context.RetainLabelsOnly;
        bodyContext.InIterationStatement = true;
        var (body, semicolonInserted) = ParseStatement(bodyContext);
        return (FinishStatement(new Ast.ForInStatement(iteratesKeys, left, right, body)), semicolonInserted);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseSwitchStatement(Context context) {
        MarkLocation();
        NextToken();
        if (ConsumeContextKeyword("type")) return ParseSwitchTypeStatement(context);
        Expect(TToken.LParen);
        var discriminant = ParseExpression();
        Expect(TToken.RParen);
        Expect(TToken.LCurly);
        var cases = new List<Ast.SwitchCase>{};
        var consequentContext = context.RetainLabelsOnly;
        consequentContext.InSwitchStatement = true;
        while (Token.Type != TToken.RCurly) {
            if (Token.IsKeyword("case")) {
                MarkLocation();
                NextToken();
                var testList = new List<Ast.Expression>();
                var test = ParseExpression(true, OperatorPrecedence.List, false);
                testList.Add(test);
                Expect(TToken.Colon);
                while (ConsumeKeyword("case"))
                {
                    testList.Add(ParseExpression(true, OperatorPrecedence.List, false));
                    Expect(TToken.Colon);
                }
                var consequent = new List<Ast.Statement>{};
                while (!Token.IsKeyword("case") && !Token.IsKeyword("default")) {
                    var (stmt2, semicolonInserted2) = ParseStatement(consequentContext);
                    consequent.Add(stmt2);
                    if (!semicolonInserted2) goto endCases;
                }
                cases.Add((Ast.SwitchCase) FinishNode(new Ast.SwitchCase(testList, consequent)));
            } else if (Token.IsKeyword("default")) {
                MarkLocation();
                NextToken();
                Expect(TToken.Colon);
                var consequent = new List<Ast.Statement>{};
                while (!Token.IsKeyword("case") && !Token.IsKeyword("default")) {
                    var (stmt2, semicolonInserted2) = ParseStatement(consequentContext);
                    consequent.Add(stmt2);
                    if (!semicolonInserted2) goto endCases;
                }
                cases.Add((Ast.SwitchCase) FinishNode(new Ast.SwitchCase(null, consequent)));
            } else ThrowUnexpected();
        }
        endCases:
        Expect(TToken.RCurly);
        return (FinishStatement(new Ast.SwitchStatement(discriminant, cases)), true);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseSwitchTypeStatement(Context context) {
        Expect(TToken.LParen);
        var discriminant = ParseExpression();
        Expect(TToken.RParen);
        Expect(TToken.LCurly);
        var cases = new List<Ast.SwitchTypeCase>{};
        var consequentContext = context.RetainLabelsOnly;
        for (;;) {
            if (Token.IsKeyword("case")) {
                MarkLocation();
                NextToken();
                Expect(TToken.LParen);
                var pattern = ParseDestructuringPattern();
                Expect(TToken.RParen);
                cases.Add((Ast.SwitchTypeCase) FinishNode(new Ast.SwitchTypeCase(pattern, ParseBlock(consequentContext))));
            } else if (Token.IsKeyword("default")) {
                if (Consume(TToken.LParen)) {
                    Expect(TToken.RParen);
                }
                cases.Add((Ast.SwitchTypeCase) FinishNode(new Ast.SwitchTypeCase(null, ParseBlock(consequentContext))));
            } else break;
        }
        Expect(TToken.RCurly);
        return (FinishStatement(new Ast.SwitchTypeStatement(discriminant, cases)), true);
    }

    private (Ast.Statement node, bool semicolonInserted) ParseUseStatement(Context context) {
        MarkLocation();
        NextToken();
        if (ConsumeContextKeyword("resource")) {
            Expect(TToken.LParen);
            var bindings = new List<Ast.VariableBinding>{};
            do {
                if (Token.Type == TToken.RParen) break;
                bindings.Add(ParseVariableBinding());
            } while (Consume(TToken.Comma));
            Expect(TToken.RParen);
            var block = ParseBlock(context.RetainLabelsOnly);
            var semicolonInserted = true;
            return (FinishStatement(new Ast.UseResourceStatement(bindings, block)), semicolonInserted);
        } else {
            ExpectContextKeyword("namespace");
            var exp = ParseExpression();
            var semicolonInserted = ParseSemicolon();
            return (FinishStatement(new Ast.UseNamespaceStatement(exp)), semicolonInserted);
        }
    }

    private (Ast.Statement node, bool semicolonInserted) ParseWithStatement(Context context) {
        MarkLocation();
        NextToken();
        Expect(TToken.LParen);
        var @object = ParseExpression();
        Expect(TToken.RParen);
        var (body, semicolonInserted) = ParseStatement(context.RetainLabelsOnly);
       return (FinishStatement(new Ast.WithStatement(@object, body)), semicolonInserted);
    }

    public Ast.Program ParseProgram() {
        MarkLocation();
        List<Ast.Statement> statements = new List<Ast.Statement>{};
        Context topLevelContext = new TopLevelContext();
        while (Token.Type != TToken.Eof) {
            var (stmt, semicolonInserted) = ParseStatement(topLevelContext);
            statements.Add(stmt);
            if (!semicolonInserted) break;
        }
        Expect(TToken.Eof);
        var r = FinishNode(new Ast.Program(statements));
        r.ConsumeStrictnessPragmas();
        return (Ast.Program) r;
    }
}

internal class StackFunction {
    public bool UsesAwait = false;
    public bool UsesYield = false;

    public StackFunction() {
    }
}

internal class DefinitionAttributes {
    public List<Ast.Expression> Decorators = null;
    public Ast.AnnotatableDefinitionModifier Modifiers = (Ast.AnnotatableDefinitionModifier) 0;
    public Ast.AnnotatableDefinitionAccessModifier? AccessModifier = null;

    public bool HasEmptyModifiers {
        get => ((int) Modifiers) == 0 && AccessModifier == null;
    }
}

internal class Context {
    public bool InSwitchStatement = false;
    public bool InIterationStatement = false;

    public Context RetainLabelsOnly {
        get {
            Context r = null;
            if (this is LabeledStatementsContext) {
                r = new LabeledStatementsContext();
                ((LabeledStatementsContext) r).Labels = new Dictionary<string, bool>(((LabeledStatementsContext) this).Labels);
            } else {
                r = new Context();
            }
            r.InSwitchStatement = this.InSwitchStatement;
            r.InIterationStatement = this.InIterationStatement;
            return r;
        }
    }

    public virtual string Name {
        get => "";
    }

    public virtual Context AddLabel(string label) {
        return new Context();
    }

    public virtual bool HasLabel(string label) {
        return false;
    }

    public virtual bool IsConstructor {
        get => false;
        set {}
    }

    public virtual bool SuperStatementFound {
        get => false;
        set {}
    }
}

internal class LabeledStatementsContext : Context {
    public Dictionary<string, bool> Labels = new Dictionary<string, bool>();

    public override Context AddLabel(string label) {
        var r = new LabeledStatementsContext();
        r.Labels = new Dictionary<string, bool>(this.Labels);
        r.Labels[label] = true;
        return r;
    }

    public override bool HasLabel(string label) {
        return Labels.ContainsKey(label);
    }
}

internal class ConstructorContext : Context {
    private bool m_IsConstructor = false;
    private bool m_SuperStatementFound = false;

    public override bool IsConstructor {
        get => m_IsConstructor;
        set { m_IsConstructor = value; }
    }

    public override bool SuperStatementFound {
        get => m_SuperStatementFound;
        set { m_SuperStatementFound = value; }
    }
}

internal class ClassContext : Context {
    private string m_Name;

    public ClassContext(string name) {
        m_Name = name;
    }

    public override string Name {
        get => m_Name;
    }
}

internal class EnumContext : Context {
}

internal class InterfaceContext : Context {
}

internal class NamespaceContext : Context {
}

internal class PackageContext : Context {
}

internal class TopLevelContext : Context {
}