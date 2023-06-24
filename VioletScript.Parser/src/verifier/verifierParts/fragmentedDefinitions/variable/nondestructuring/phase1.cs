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
        pattern.SemanticProperty = this.DefineOrReuseVariable(pattern.Name, output, null, pattern.Span.Value, readOnly, visibility);
        if (pattern.SemanticProperty != null)
        {
            pattern.SemanticProperty.ParentDefinition = parentDefinition;
        }
    }
}