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
    private void VerifyStatement(Ast.Statement stmt)
    {
        if (stmt is Ast.ExpressionStatement expStmt)
        {
            VerifyExp(expStmt.Expression);
        }
        else
        {
            throw new Exception("Unimplemented directive or statement");
        }
    }
}