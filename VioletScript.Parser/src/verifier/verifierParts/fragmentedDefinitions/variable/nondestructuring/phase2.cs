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
    private void Fragmented_VerifyNondestructuringPattern2(Ast.NondestructuringPattern pattern)
    {
        if (pattern.Type == null)
        {
            return;
        }
        var type = this.VerifyTypeExp(pattern.Type);
        if (pattern.SemanticProperty != null)
        {
            pattern.SemanticProperty.StaticType ??= type;
            if (type != null && pattern.SemanticProperty.StaticType != null && type != pattern.SemanticProperty.StaticType)
            {
                this.VerifyError(pattern.Span.Value.Script, 245, pattern.Span.Value, new DiagnosticArguments {});
            }
        }
    }
}