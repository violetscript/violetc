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
                    field.SemanticProperty = this.DefineOrReuseVariable(key.Value, output, null, key.Span.Value, readOnly, visibility);
                    if (field.SemanticProperty != null)
                    {
                        field.SemanticProperty.ParentDefinition = parentDefinition;
                    }
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
                    // ERROR: key is not an identifier
                    VerifyError(field.Key.Span.Value.Script, 145, field.Key.Span.Value, new DiagnosticArguments {});
                }
            }
        }
    }
}