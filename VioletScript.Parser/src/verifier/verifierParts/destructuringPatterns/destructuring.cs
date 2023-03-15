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
    // verify a destructuring pattern; ensure:
    // - it is a non-duplicate property if it is a bind pattern.
    // - if there is both a type annotation and an inferred type, ensure they are equals.
    // - if there is no type annotation and no inferred type, throw a VerifyError.
    //
    // subpatterns always need an `inferredType` argument.
    //
    public void VerifyDestructuringPattern
    (
        Ast.DestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Symbol inferredType = null
    )
    {
        if (pattern.SemanticProperty != null)
        {
            return;
        }
        if (pattern is Ast.BindPattern bp)
        {
            VerifyBindPattern(bp, readOnly, output, inferredType);
        }
        else if (pattern is Ast.ArrayDestructuringPattern arrayP)
        {
            VerifyArrayDestructuringPattern(arrayP, readOnly, output, inferredType);
        }
        else
        {
            VerifyRecordDestructuringPattern((Ast.RecordDestructuringPattern) pattern, readOnly, output, inferredType);
        }
    }

    private void VerifyBindPattern
    (
        Ast.BindPattern pattern,
        bool readOnly,
        Properties output,
        Symbol inferredType = null
    )
    {
        Symbol newDefinition = null;
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
        var previousDefinition = output[pattern.Name];
        if (previousDefinition != null)
        {
            // VerifyError: duplicate definition
            newDefinition = previousDefinition;
            if (!m_Options.AllowDuplicates)
            {
                VerifyError(pattern.Span.Value.Script, 139, pattern.Span.Value, new DiagnosticArguments { ["name"] = pattern.Name });
            }
        }
        else
        {
            newDefinition = m_ModelCore.Factory.VariableSlot(pattern.Name, readOnly, type);
            output[pattern.Name] = newDefinition;
        }
        pattern.SemanticProperty = newDefinition;
    }
}