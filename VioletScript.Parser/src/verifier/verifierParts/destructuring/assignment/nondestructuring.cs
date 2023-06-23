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
    // verify a non-destructuring pattern for assignment expressions.
    private void VerifyAssignmentNondestructuringPattern(Ast.NondestructuringPattern pattern, Symbol type)
    {
        Symbol annotatedType = null;
        if (pattern.Type != null)
        {
            annotatedType = VerifyTypeExp(pattern.Type) ?? m_ModelCore.AnyType;
            // inferred type and type annotation must be the same
            if (annotatedType != type)
            {
                VerifyError(pattern.Span.Value.Script, 140, pattern.Span.Value, new DiagnosticArguments { ["i"] = type, ["a"] = annotatedType });
            }
        }

        pattern.SemanticFrameAssignedReference = AssignmentRecordDestructuringLexicalRef(pattern.Name, pattern.Span.Value);
    }

    private Symbol AssignmentRecordDestructuringLexicalRef(string name, Span nameSpan)
    {
        var r = m_Frame.ResolveProperty(name);
        if (r == null)
        {
            // VerifyError: undefined reference
            VerifyError(null, 128, nameSpan, new DiagnosticArguments { ["name"] = name });
        }
        else if (r is AmbiguousReferenceIssue)
        {
            // VerifyError: ambiguous reference
            VerifyError(null, 129, nameSpan, new DiagnosticArguments { ["name"] = name });
        }
        // must be a lexical reference
        else if (!(r is ReferenceValueFromFrame))
        {
            VerifyError(null, 148, nameSpan, new DiagnosticArguments { ["name"] = name });
        }
        // read-only
        else if (r.ReadOnly)
        {
            VerifyError(null, 147, nameSpan, new DiagnosticArguments { ["name"] = name });
        }
        else
        {
            if (!r.PropertyIsVisibleTo(m_Frame))
            {
                // VerifyError: accessing private property
                VerifyError(null, 130, nameSpan, new DiagnosticArguments { ["name"] = name });
            }

            // extend variable life
            if (r.Base.FindActivation() != m_Frame.FindActivation())
            {
                r.Base.FindActivation().AddExtendedLifeVariable(r.Property);
            }

            return r;
        }
        return null;
    }
}