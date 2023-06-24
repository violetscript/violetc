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
    private void Fragmented_VerifyNondestructuringPattern1(
        Ast.NondestructuringPattern pattern, bool readOnly,
        Properties output, Visibility visibility,
        Symbol parentDefinition)
    {
        Symbol newDefinition = null;
        var previousDefinition = output[pattern.Name];
        if (previousDefinition != null)
        {
            // VerifyError: duplicate definition
            newDefinition = previousDefinition is VariableSlot ? previousDefinition : null;
            // assert newDefinition != null
            if (newDefinition == null)
            {
                throw new Exception("Duplicating definition with wrong kind.");
            }
            if (!m_Options.AllowDuplicates)
            {
                VerifyError(pattern.Span.Value.Script, 139, pattern.Span.Value, new DiagnosticArguments { ["name"] = pattern.Name });
            }
        }
        else
        {
            newDefinition = m_ModelCore.Factory.VariableSlot(pattern.Name, readOnly, null);
            newDefinition.Visibility = visibility;
            newDefinition.ParentDefinition = parentDefinition;
            output[pattern.Name] = newDefinition;
        }
        pattern.SemanticProperty = newDefinition;
    }
}