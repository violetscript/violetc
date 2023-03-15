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
    // - it is a non-duplicate property if it is not a destructuring pattern.
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
    }
}