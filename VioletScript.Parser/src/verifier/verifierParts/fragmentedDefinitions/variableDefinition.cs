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
            Fragmented_VerifyVariableDefinition1(defn);
        }
        else if (phase == VerifyPhase.Phase2)
        {
            Fragmented_VerifyVariableDefinition2(defn);
        }
        else if (phase == VerifyPhase.Phase3)
        {
            Fragmented_VerifyVariableDefinition3(defn);
        }
        // VerifyPhase.Phase7
        else if (phase == VerifyPhase.Phase7)
        {
            Fragmented_VerifyVariableDefinition7(defn);
        }
    }
}