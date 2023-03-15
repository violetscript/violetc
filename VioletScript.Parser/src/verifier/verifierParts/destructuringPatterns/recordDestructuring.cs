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
    // verify an array destructuring pattern; ensure:
    // - if there is both a type annotation and an inferred type, ensure they are equals.
    // - if there is no type annotation and no inferred type, throw a VerifyError.
    // - only one rest element is allowed and must be at the end of the pattern.
    private void VerifyRecordDestructuringPattern
    (
        Ast.RecordDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol inferredType = null
    )
    {
        Symbol type = null;
        if (pattern.Type != null)
        {
            type = VerifyTypeExp(pattern.Type);
        }
        if (type == null)
        {
            type = inferredType;
            if (type == null)
            {
                VerifyError(pattern.Span.Value.Script, 138, pattern.Span.Value, new DiagnosticArguments { ["name"] = pattern.Name });
                type = m_ModelCore.AnyType;
            }
        }
        // inferred type and type annotation must be the same
        else if (inferredType != null && inferredType != type)
        {
            VerifyError(pattern.Span.Value.Script, 140, pattern.Span.Value, new DiagnosticArguments { ["i"] = inferredType, ["a"] = type });
        }

        pattern.SemanticProperty = m_ModelCore.Factory.VariableSlot("", readOnly, type);

        if (type == m_ModelCore.AnyType)
        {
            VerifyRecordDestructuringPatternForAny(pattern, readOnly, output, visibility);
        }
    }

    private void VerifyRecordDestructuringPatternForAny
    (
        Ast.RecordDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility
    )
    {
        foreach (var field in pattern.Fields)
        {
            if (field.Key is Ast.StringLiteral key)
            {
                verifyRecordFieldHere();
            }
            // VerifyError: key is not an identifier
            else
            {
                VerifyError(field.Key.Span.Value.Script, 145, field.Key.Span.Value, new DiagnosticArguments {});
                if (field.Subpattern != null)
                {
                    VerifyDestructuringPattern(field.Subpattern, readOnly, output, visibility, m_ModelCore.AnyType);
                }
            }
        }
    }
}