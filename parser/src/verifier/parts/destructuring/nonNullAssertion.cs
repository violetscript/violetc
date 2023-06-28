namespace VioletScript.Parser.Verifier;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Diagnostic;
using VioletScript.Parser.Semantic.Logic;
using VioletScript.Parser.Semantic.Model;
using VioletScript.Parser.Source;
using Ast = VioletScript.Parser.Ast;

using DiagnosticArguments = Dictionary<string, object>;

public partial class Verifier
{
    private Symbol DestructuringNonNullAssertion(Symbol type, Span span)
    {
        if (!(type.IncludesNull || type.IncludesUndefined))
        {
            VerifyError(null, 269, span, new DiagnosticArguments {["type"] = type});
        }
        return type.ToNonNullableType();
    }
}