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
    private void Fragmented_VerifyConstructorDefinition(Ast.ConstructorDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            this.Fragmented_VerifyConstructorDefinition1(defn);
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

    private void Fragmented_VerifyConstructorDefinition1(Ast.ConstructorDefinition defn)
    {
        var parentDefinition = m_Frame.TypeFromFrame;
        // - do not allow duplicate constructor
        toDo();
    }

    private void Fragmented_VerifyConstructorDefinition2(Ast.ConstructorDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }
        // - resolve signature, however infer the void type.
        toDo();
    }

    private void Fragmented_VerifyConstructorDefinition3(Ast.ConstructorDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }
        // - if does not directly inherit Object and there is no super()
        // it is a verify error. look for super() in include directives also.
        var classType = m_Frame.TypeFromFrame;
        toDo();
    }

    private void Fragmented_VerifyConstructorDefinition7(Ast.ConstructorDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }
        this.EnterFrame(defn.Common.SemanticActivation);
        this.Fragmented_VerifyFunctionDefinition7Params(defn.Common);
        // ignore "throws" clause
        if (defn.Common.ThrowsType != null)
        {
            this.VerifyTypeExp(defn.Common.ThrowsType);
        }
        this.Fragmented_VerifyFunctionDefinition7Body(defn.Common, method, defn.Id.Span.Value);
        this.ExitFrame();
    }
}