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
    // verify a binding pattern; ensure
    // - it is a non-duplicate property.
    // - if there is both a type annotation and an inferred type, ensure they are equals.
    // - if there is no type annotation and no inferred type, throw a VerifyError.
    private void VerifyNondestructuringPattern
    (
        Ast.NondestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol inferredType = null
    )
    {
        Symbol type = null;
        if (pattern.Type != null)
        {
            type = VerifyTypeExp(pattern.Type) ?? m_ModelCore.AnyType;
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

        type ??= m_ModelCore.AnyType;
        pattern.SemanticProperty = this.DefineOrReuseVariable(pattern.Name, output, type, pattern.Span.Value, readOnly, visibility);
    }

    private Symbol DefineOrReuseVariable(string name, Properties output, Symbol type, Span span, bool readOnly, Visibility visibility)
    {
        Symbol newDefinition = null;
        var previousDefinition = output[name];

        if (previousDefinition != null)
        {
            newDefinition = previousDefinition is VariableSlot ? previousDefinition : null;

            // assert newDefinition != null
            if (newDefinition == null)
            {
                throw new Exception("Duplicating definition with wrong kind.");
            }

            // ERROR: duplicate definition
            if (!m_Options.AllowDuplicates)
            {
                VerifyError(span.Script, 139, span, new DiagnosticArguments { ["name"] = name });
            }
        }
        else
        {
            newDefinition = m_ModelCore.Factory.VariableSlot(name, readOnly, type);
            newDefinition.Visibility = visibility;
            newDefinition.InitValue ??= type.DefaultValue;
            output[name] = newDefinition;
        }
        return newDefinition;
    }
}