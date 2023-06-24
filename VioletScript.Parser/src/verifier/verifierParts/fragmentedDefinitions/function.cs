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
    private void Fragmented_VerifyFunctionDefinition(Ast.FunctionDefinition defn, VerifyPhase phase)
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
        else if (phase == VerifyPhase.Phase6)
        {
            doFooBarQuxBaz();
        }
        // VerifyPhase.Phase7
        else
        {
            doFooBarQuxBaz();
        }
    }

    private void Fragmented_VerifyFunctionDefinition1(Ast.FunctionDefinition defn)
    {
        defn.SemanticMethodSlot = this.DefineOrReusePartialMethod();
    }

    /// <summary>
    /// Defines or re-uses a partially defined method.
    /// If it is a duplicate, returns null.
    /// </summary>
    private Symbol DefineOrReusePartialMethod(string name, Properties output, Span span, Visibility visibility, MethodSlotFlags flags, Symbol parentDefinition)
    {
        Symbol newDefinition = null;
        var previousDefinition = output[name];

        if (previousDefinition != null)
        {
            newDefinition = previousDefinition is MethodSlot ? previousDefinition : null;

            // ERROR: duplicate definition
            if (!m_Options.AllowDuplicates || newDefinition == null)
            {
                VerifyError(span.Script, 139, span, new DiagnosticArguments { ["name"] = name });
                newDefinition = null;
            }
        }
        else
        {
            newDefinition = m_ModelCore.Factory.MethodSlot(name, null, flags);
            newDefinition.Visibility = visibility;
            newDefinition.ParentDefinition = parentDefinition;
            output[name] = newDefinition;
        }
        return newDefinition;
    }
}