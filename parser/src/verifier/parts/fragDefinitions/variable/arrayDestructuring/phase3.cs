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
    private void Fragmented_VerifyArrayDestructuringPattern3(Ast.ArrayDestructuringPattern pattern)
    {
        foreach (var item in pattern.Items)
        {
            if (item is Ast.ArrayDestructuringSpread spread)
            {
                this.Fragmented_VerifyDestructuringPattern3(spread.Pattern);
                continue;
            }
            // ignore hole
            if (item == null)
            {
                continue;
            }
            this.Fragmented_VerifyDestructuringPattern3((Ast.DestructuringPattern) item);
        }
    }
}