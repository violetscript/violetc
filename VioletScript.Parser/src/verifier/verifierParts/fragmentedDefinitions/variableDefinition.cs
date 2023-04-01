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
    private void Fragmented_VerifyVariableDefinition(Ast.VariableDefinition defn, VerifyPhase phase)
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
        else if (phase == VerifyPhase.Phase4)
        {
            doFooBarQuxBaz();
        }
        else if (phase == VerifyPhase.Phase5)
        {
            doFooBarQuxBaz();
        }
        // VerifyPhase.Phase6
        else
        {
            doFooBarQuxBaz();
        }
    }
}