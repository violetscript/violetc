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
            this.Fragmented_VerifyConstructorDefinition2(defn);
        }
        else if (phase == VerifyPhase.Phase3)
        {
            this.Fragmented_VerifyConstructorDefinition3(defn);
        }
        // VerifyPhase.Phase7
        else if (phase == VerifyPhase.Phase7)
        {
            this.Fragmented_VerifyConstructorDefinition7(defn);
        }
    }

    private void Fragmented_VerifyConstructorDefinition1(Ast.ConstructorDefinition defn)
    {
        var type = m_Frame.TypeFromFrame;
        // do not allow duplicate constructor.
        if (type.ConstructorDefinition != null)
        {
            VerifyError(defn.Id.Span.Value.Script, 256, defn.Id.Span.Value, new DiagnosticArguments {});
        }
        else
        {
            var method = m_ModelCore.Factory.MethodSlot(type.Name, null, defn.SemanticFlags(type));
            method.Visibility = Visibility.Public;
            method.ParentDefinition = type;
            type.ConstructorDefinition = method;

            defn.Common.SemanticActivation = this.m_ModelCore.Factory.ActivationFrame();
            // set `this`
            defn.Common.SemanticActivation.ActivationThisOrThisAsStaticType = this.m_ModelCore.Factory.ThisValue(type);
        }
    }

    private void Fragmented_VerifyConstructorDefinition2(Ast.ConstructorDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }
        this.EnterFrame(defn.Common.SemanticActivation);

        // resolve signature, however infer the void type.
        method.StaticType = this.Fragmented_ResolveFunctionSignature(defn.Common, defn.Id.Span.Value, true);

        this.ExitFrame();
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