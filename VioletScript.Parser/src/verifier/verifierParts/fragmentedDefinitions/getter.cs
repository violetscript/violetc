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
    private void Fragmented_VerifyGetterDefinition(Ast.GetterDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            Fragmented_VerifyGetterDefinition1(defn);
        }
        else if (phase == VerifyPhase.Phase2)
        {
            Fragmented_VerifyGetterDefinition2(defn);
        }
        else if (phase == VerifyPhase.Phase3)
        {
            Fragmented_VerifyGetterDefinition3(defn);
        }
        // VerifyPhase.Phase7
        else if (phase == VerifyPhase.Phase7)
        {
            Fragmented_VerifyGetterDefinition7(defn);
        }
    }

    private void Fragmented_VerifyGetterDefinition1(Ast.GetterDefinition defn)
    {
        var parentDefinition = m_Frame.TypeFromFrame ?? m_Frame.NamespaceFromFrame ?? m_Frame.PackageFromFrame;

        var isTypeStatic = defn.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Static) && parentDefinition is Type;
        var output = isTypeStatic || !(parentDefinition is Type) ? parentDefinition?.Properties ?? this.m_Frame.Properties : parentDefinition.Delegate.Properties;

        defn.SemanticMethodSlot = this.DefinePartialGetter(defn.Id.Name, output, defn.Id.Span.Value, defn.SemanticVisibility, defn.SemanticFlags(parentDefinition), parentDefinition);
        if (defn.SemanticMethodSlot != null)
        {
            defn.Common.SemanticActivation = this.m_ModelCore.Factory.ActivationFrame();
            // set `this`
            defn.Common.SemanticActivation.ActivationThisOrThisAsStaticType = isTypeStatic ? this.m_ModelCore.Factory.ClassStaticThis(parentDefinition) : parentDefinition is Type ? this.m_ModelCore.Factory.ThisValue(parentDefinition) : null;
        }
    }

    /// <summary>
    /// Defines a getter. If it is a duplicate, returns null.
    /// </summary>
    private Symbol DefinePartialGetter(string name, Properties output, Span span, Visibility visibility, MethodSlotFlags flags, Symbol parentDefinition)
    {
        var previousDefinition = output[name];
        var prevIsVirtualPropWithoutGetter = previousDefinition is VirtualSlot virtualSlot && virtualSlot.Getter == null;
        if (previousDefinition != null && !prevIsVirtualPropWithoutGetter)
        {
            VerifyError(span.Script, 139, span, new DiagnosticArguments { ["name"] = name });
            return null;
        }
        if (previousDefinition != null && previousDefinition.Visibility != visibility)
        {
            // ERROR: wrong visibility
            VerifyError(span.Script, 262, span, new DiagnosticArguments {});
        }
        Symbol newDefinition = previousDefinition ?? m_ModelCore.Factory.VirtualSlot(name, null);
        newDefinition.Visibility = visibility;
        output[name] = newDefinition;
        Symbol method = m_ModelCore.Factory.MethodSlot(name, null, flags);
        method.ParentDefinition = parentDefinition;
        method.BelongsToVirtualProperty = newDefinition;
        method.BelongsToVirtualProperty.ParentDefinition = parentDefinition;
        return method;
    }
}