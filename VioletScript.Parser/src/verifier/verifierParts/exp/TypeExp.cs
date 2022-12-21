namespace VioletScript.Parser.Verifier;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Problem;
using VioletScript.Parser.Semantic.Logic;
using VioletScript.Parser.Semantic.Model;
using VioletScript.Parser.Source;
using Ast = VioletScript.Parser.Ast;

using ProblemVars = Dictionary<string, object>;

public partial class Verifier {
    public Symbol VerifyTypeExp(Ast.TypeExpression exp) {
        if (exp is Ast.IdentifierTypeExpression id) {
            //
        }
        //
        throw new Exception("Uncovered type expression");
    }
}