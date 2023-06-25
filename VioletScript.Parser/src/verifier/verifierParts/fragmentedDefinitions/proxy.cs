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
    private void Fragmented_VerifyProxyDefinition(Ast.ProxyDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            doFooBarQuxBaz();
        }
        else if (phase == VerifyPhase.Phase2)
        {
            doFooBarQuxBaz();
        }
        else if (phase == VerifyPhase.Phase3)
        {
            doFooBarQuxBaz();
        }
        // VerifyPhase.Phase7
        else if (phase == VerifyPhase.Phase7)
        {
            doFooBarQuxBaz();
        }
    }
}