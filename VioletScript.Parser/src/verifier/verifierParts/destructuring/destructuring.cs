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
    // - if there is both a type annotation and an inferred type, ensure they are equals.
    // - if there is no type annotation and no inferred type, throw a VerifyError.
    //
    // nested patterns always need an `inferredType` argument.
    //
    private void VerifyDestructuringPattern
    (
        Ast.DestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol inferredType = null
    )
    {
        if (pattern.SemanticProperty != null)
        {
            return;
        }
        if (pattern is Ast.NondestructuringPattern bp)
        {
            VerifyNondestructuringPattern(bp, readOnly, output, visibility, inferredType);
        }
        else if (pattern is Ast.ArrayDestructuringPattern arrayP)
        {
            VerifyArrayDestructuringPattern(arrayP, readOnly, output, visibility, inferredType);
        }
        else
        {
            VerifyRecordDestructuringPattern((Ast.RecordDestructuringPattern) pattern, readOnly, output, visibility, inferredType);
        }
    }
}