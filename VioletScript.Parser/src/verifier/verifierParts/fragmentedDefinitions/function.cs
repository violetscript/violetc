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
            this.Fragmented_VerifyFunctionDefinition1(defn);
        }
        else if (phase == VerifyPhase.Phase2)
        {
            this.Fragmented_VerifyFunctionDefinition2(defn);
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

    private void Fragmented_VerifyFunctionDefinition1(Ast.FunctionDefinition defn)
    {
        var parentDefinition = m_Frame.TypeFromFrame ?? m_Frame.NamespaceFromFrame ?? m_Frame.PackageFromFrame;

        var isTypeStatic = defn.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Static) && parentDefinition is Type;
        var output = isTypeStatic || !(parentDefinition is Type) ? parentDefinition?.Properties ?? this.m_Frame.Properties : parentDefinition.Delegate.Properties;

        defn.SemanticMethodSlot = this.DefineOrReusePartialMethod(defn.Id.Name, output, defn.Id.Span.Value, defn.SemanticVisibility, defn.SemanticFlags(parentDefinition), parentDefinition);
        if (defn.SemanticMethodSlot != null)
        {
            defn.SemanticActivation = this.m_ModelCore.Factory.ActivationFrame();
            // set `this`
            defn.SemanticActivation.ActivationThisOrThisAsStaticType = isTypeStatic ? this.m_ModelCore.Factory.ClassStaticThis(parentDefinition) : parentDefinition is Type ? this.m_ModelCore.Factory.ThisValue(parentDefinition) : null;

            if (defn.Generics != null)
            {
                this.EnterFrame(defn.SemanticActivation);
                defn.SemanticMethodSlot.TypeParameters = FragmentedA_VerifyTypeParameters(defn.Generics, defn.SemanticActivation.Properties, defn.SemanticMethodSlot);
                this.ExitFrame();
            }
        }
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

    private void Fragmented_VerifyFunctionDefinition2(Ast.FunctionDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }
        this.EnterFrame(defn.SemanticActivation);

        if (defn.Generics != null)
        {
            FragmentedB_VerifyTypeParameters(method.TypeParameters, defn.Generics, defn.SemanticActivation.Properties);
        }

        // resolve signature
        toDo();

        this.ExitFrame();
    }
}