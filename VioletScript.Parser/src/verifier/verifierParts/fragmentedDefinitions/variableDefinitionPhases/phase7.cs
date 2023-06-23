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
    private void Fragmented_VerifyVariableDefinition7(Ast.VariableDefinition defn)
    {
        // if it is a program top-level variable,
        // it has been resolved by a separate method.
        if (defn.SemanticShadowFrame != null)
        {
            return;
        }

        doFooBarQuxBaz();
    }
}