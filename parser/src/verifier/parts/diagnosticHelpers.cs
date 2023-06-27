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
    private void ReportNameNotFound(string name, Span span, Symbol @base = null)
    {
        if (@base is Value)
        {
            VerifyError(null, 198, span, new DiagnosticArguments { ["t"] = @base.StaticType, ["name"] = name });
        }
        else if (@base is Type)
        {
            VerifyError(null, 198, span, new DiagnosticArguments { ["t"] = @base, ["name"] = name });
        }
        else if (@base is Package)
        {
            VerifyError(null, 266, span, new DiagnosticArguments { ["base"] = @base, ["name"] = name });
        }
        else if (@base is Namespace)
        {
            VerifyError(null, 267, span, new DiagnosticArguments { ["base"] = @base, ["name"] = name });
        }
        else
        {
            VerifyError(null, 128, span, new DiagnosticArguments { ["name"] = name });
        }
    }
}