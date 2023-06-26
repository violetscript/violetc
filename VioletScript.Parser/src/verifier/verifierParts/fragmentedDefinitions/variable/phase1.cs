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
    private void Fragmented_VerifyVariableDefinition1(Ast.VariableDefinition defn)
    {
        var parentDefinition = m_Frame.TypeFromFrame ?? m_Frame.NamespaceFromFrame ?? m_Frame.PackageFromFrame;

        // if there is no parent item, this is a program top-level
        // variable.
        if (parentDefinition == null)
        {
            defn.SemanticAtToplevel = true;
            return;
        }

        // determine target set of properties. this depends
        // on the 'static' modifier.
        var output = (defn.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Static) && parentDefinition is Type) || !(parentDefinition is Type) ? parentDefinition.Properties : parentDefinition.Delegate.Properties;

        foreach (var binding in defn.Bindings)
        {
            this.Fragmented_VerifyVariableBinding1(binding, defn.ReadOnly, output, defn.SemanticVisibility, parentDefinition);
        }
    }

    private void Fragmented_VerifyVariableBinding1(
        Ast.VariableBinding binding, bool readOnly,
        Properties output, Visibility visibility,
        Symbol parentDefinition)
    {
        // either a type annotation or an initializer
        // has to be present.
        if (binding.Pattern.Type == null && binding.Init == null)
        {
            this.VerifyError(binding.Pattern.Span.Value.Script, 138, binding.Pattern.Span.Value, new DiagnosticArguments {});
        }

        // - set parent definition in the variables from the patterns
        this.Fragmented_VerifyDestructuringPattern1(binding.Pattern, readOnly, output, visibility, parentDefinition);
    }

    private void Fragmented_VerifyDestructuringPattern1(
        Ast.DestructuringPattern pattern, bool readOnly,
        Properties output, Visibility visibility,
        Symbol parentDefinition)
    {
        if (pattern is Ast.NondestructuringPattern nondestructuring)
        {
            this.Fragmented_VerifyNondestructuringPattern1(nondestructuring, readOnly, output, visibility, parentDefinition);
        }
        else if (pattern is Ast.RecordDestructuringPattern recordDestructuring)
        {
            this.Fragmented_VerifyRecordDestructuringPattern1(recordDestructuring, readOnly, output, visibility, parentDefinition);
        }
        else
        {
            this.Fragmented_VerifyArrayDestructuringPattern1((Ast.ArrayDestructuringPattern) pattern, readOnly, output, visibility, parentDefinition);
        }
    }
}