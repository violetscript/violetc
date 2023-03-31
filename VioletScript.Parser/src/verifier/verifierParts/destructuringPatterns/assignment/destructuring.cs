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
    // verify a destructuring pattern for assignment expressions.
    private void VerifyAssignmentDestructuringPattern(Ast.DestructuringPattern pattern, Symbol type)
    {
        if (pattern is Ast.BindPattern ap)
        {
            VerifyAssignmentPattern(ap, type);
        }
        else if (pattern is Ast.ArrayDestructuringPattern arrayP)
        {
            VerifyAssignmentArrayDestructuringPattern(arrayP, type);
        }
        else
        {
            VerifyAssignmentRecordDestructuringPattern((Ast.RecordDestructuringPattern) pattern, type);
        }
    }
}