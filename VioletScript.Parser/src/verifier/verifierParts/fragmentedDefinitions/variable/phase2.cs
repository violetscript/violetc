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
    private void Fragmented_VerifyVariableDefinition2(Ast.VariableDefinition defn)
    {
        // if it is a program top-level variable,
        // it is resolved by a separate method at phase 7.
        if (defn.SemanticAtToplevel)
        {
            return;
        }

        foreach (var binding in defn.Bindings)
        {
            this.Fragmented_VerifyVariableBinding2(binding);
        }
    }

    private void Fragmented_VerifyVariableBinding2(Ast.VariableBinding binding)
    {
        if (binding.Pattern.SemanticProperty != null)
        {
            // if not in class frame,
            // the binding must have a constant initial value
            // or initializer.
            // futurely an error will also occur for class
            // variables that are not initialized by the constructor.
            var notInClass = !(this.m_Frame is ClassFrame);
            var noInitialValueOrInit = binding.Pattern.SemanticProperty.InitValue == null && binding.Init == null;
            if (notInClass && noInitialValueOrInit)
            {
                VerifyError(binding.Pattern.Span.Value.Script, 244, binding.Span.Value, new DiagnosticArguments {});
            }
        }

        this.Fragmented_VerifyDestructuringPattern2(binding.Pattern);
    }

    private void Fragmented_VerifyDestructuringPattern2(Ast.DestructuringPattern pattern)
    {
        if (pattern is Ast.NondestructuringPattern nondestructuring)
        {
            this.Fragmented_VerifyNondestructuringPattern2(nondestructuring);
        }
        else if (pattern is Ast.RecordDestructuringPattern recordDestructuring)
        {
            this.Fragmented_VerifyRecordDestructuringPattern2(recordDestructuring);
        }
        else
        {
            this.Fragmented_VerifyArrayDestructuringPattern2((Ast.ArrayDestructuringPattern) pattern);
        }
    }
}