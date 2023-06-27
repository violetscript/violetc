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
    private void Fragmented_VerifyArrayDestructuringPattern2(Ast.ArrayDestructuringPattern pattern)
    {
        if (pattern.Type != null)
        {
            var type = this.VerifyTypeExp(pattern.Type);
            pattern.SemanticProperty.StaticType ??= type;
            if (type != null && type != pattern.SemanticProperty.StaticType)
            {
                this.VerifyError(pattern.Span.Value.Script, 245, pattern.Span.Value, new DiagnosticArguments {});
            }
        }

        foreach (var item in pattern.Items)
        {
            if (item is Ast.ArrayDestructuringSpread spread)
            {
                this.Fragmented_VerifyDestructuringPattern2(spread.Pattern);
                continue;
            }
            // ignore hole
            if (item == null)
            {
                continue;
            }
            this.Fragmented_VerifyDestructuringPattern2((Ast.DestructuringPattern) item);
        }
    }
}