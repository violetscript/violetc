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
    // verify a destructuring pattern for assignment expressions.
    private void VerifyAssignmentPattern(Ast.BindPattern pattern, Symbol type)
    {
        Symbol annotatedType = null;
        if (pattern.Type != null)
        {
            annotatedType = VerifyTypeExp(pattern.Type);
            // inferred type and type annotation must be the same
            if (annotatedType != type)
            {
                VerifyError(pattern.Span.Value.Script, 140, pattern.Span.Value, new DiagnosticArguments { ["i"] = type, ["a"] = annotatedType });
            }
        }

        var r = m_Frame.ResolveProperty(pattern.Name);
        if (r == null)
        {
            // VerifyError: undefined reference
            VerifyError(pattern.Span.Value.Script, 128, pattern.Span.Value, new DiagnosticArguments { ["name"] = pattern.Name });
        }
        else if (r is AmbiguousReferenceIssue)
        {
            // VerifyError: ambiguous reference
            VerifyError(pattern.Span.Value.Script, 129, pattern.Span.Value, new DiagnosticArguments { ["name"] = pattern.Name });
        }
        // must be a lexical reference
        else if (!(r is ReferenceValueFromFrame))
        {
            VerifyError(pattern.Span.Value.Script, 148, pattern.Span.Value, new DiagnosticArguments { ["name"] = pattern.Name });
        }
        // read-only
        else if (r.ReadOnly)
        {
            VerifyError(pattern.Span.Value.Script, 147, pattern.Span.Value, new DiagnosticArguments { ["name"] = pattern.Name });
        }
        else
        {
            if (!r.PropertyIsVisibleTo(m_Frame))
            {
                // VerifyError: accessing private property
                VerifyError(pattern.Span.Value.Script, 130, pattern.Span.Value, new DiagnosticArguments { ["name"] = pattern.Name });
            }
            pattern.SemanticFrameAssignedReference = r;

            // extend variable life
            if (r.Base.FindActivation() != m_Frame.FindActivation())
            {
                r.Base.FindActivation().AddExtendedLifeVariable(r.Property);
            }
        }
    }
}