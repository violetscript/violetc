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
    private void Fragmented_VerifyRecordDestructuringPattern1(
        Ast.RecordDestructuringPattern pattern, bool readOnly,
        Properties output, Visibility visibility,
        Symbol parentDefinition)
    {
        pattern.SemanticProperty = m_ModelCore.Factory.VariableSlot("", readOnly, null);
        pattern.SemanticProperty.ParentDefinition = parentDefinition;

        foreach (var field in pattern.Fields)
        {
            if (field.Key is Ast.StringLiteral key)
            {
                if (field.Subpattern == null)
                {
                    Symbol newDefinition = null;
                    var previousDefinition = output[key.Value];
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
                            VerifyError(key.Span.Value.Script, 139, key.Span.Value, new DiagnosticArguments { ["name"] = key.Value });
                        }
                    }
                    else
                    {
                        newDefinition = m_ModelCore.Factory.VariableSlot(key.Value, readOnly, null);
                        newDefinition.Visibility = visibility;
                        newDefinition.ParentDefinition = parentDefinition;
                        output[key.Value] = newDefinition;
                    }
                    field.SemanticProperty = newDefinition;
                }
                else
                {
                    this.Fragmented_VerifyDestructuringPattern1(field.Subpattern, readOnly, output, visibility, parentDefinition);
                }
            }
            else
            {
                if (field.Subpattern != null)
                {
                    this.Fragmented_VerifyDestructuringPattern1(field.Subpattern, readOnly, output, visibility, parentDefinition);
                }
                else
                {
                    // VerifyError: key is not an identifier
                    VerifyError(field.Key.Span.Value.Script, 145, field.Key.Span.Value, new DiagnosticArguments {});
                }
            }
        }
    }
}