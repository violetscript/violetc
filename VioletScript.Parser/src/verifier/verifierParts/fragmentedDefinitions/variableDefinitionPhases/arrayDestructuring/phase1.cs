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
    private void Fragmented_VerifyArrayDestructuringPattern1(
        Ast.ArrayDestructuringPattern pattern, bool readOnly,
        Properties output, Visibility visibility,
        Symbol parentDefinition)
    {
        pattern.SemanticProperty = m_ModelCore.Factory.VariableSlot("", readOnly, null);
        pattern.SemanticProperty.ParentDefinition = parentDefinition;
        foreach (var item in pattern.Items)
        {
            if (item is Ast.ArrayDestructuringSpread spread)
            {
                this.Fragmented_VerifyDestructuringPattern1(spread.Pattern, readOnly, output, visibility, parentDefinition);
                continue;
            }
            // ignore hole
            if (item == null)
            {
                continue;
            }
            this.Fragmented_VerifyDestructuringPattern1((Ast.DestructuringPattern) item, readOnly, output, visibility, parentDefinition);
        }
    }
}