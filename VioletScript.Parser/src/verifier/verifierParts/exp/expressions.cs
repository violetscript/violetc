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
    public Symbol VerifyExp
    (
        Ast.Expression exp,
        Symbol expectedType = null,
        bool instantiatingGeneric = false
    )
    {
        if (exp.SemanticExpResolved)
        {
            return exp.SemanticSymbol;
        }
        if (!exp.SemanticConstantExpResolved)
        {
            var r = VerifyConstantExp(exp, false, expectedType, instantiatingGeneric);
            if (r !=  null)
            {
                return r;
            }
        }
        //
    }
}