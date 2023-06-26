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
    private void Fragmented_VerifyVariableDefinition3(Ast.VariableDefinition defn)
    {
        // if it is a program top-level variable,
        // it is resolved by a separate method at phase 7.
        if (defn.SemanticAtToplevel)
        {
            return;
        }

        var isStatic = defn.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Static);
        foreach (var binding in defn.Bindings)
        {
            this.Fragmented_VerifyVariableBinding3(binding, isStatic);
        }
    }

    private void Fragmented_VerifyVariableBinding3(Ast.VariableBinding binding, bool isStatic)
    {
        // do not allow shadowing instance inherited properties
        if (!isStatic && this.m_Frame.TypeFromFrame != null)
        {
            this.Fragmented_VerifyDestructuringPattern3(binding.Pattern);
        }
    }

    private void Fragmented_VerifyDestructuringPattern3(Ast.DestructuringPattern pattern)
    {
        if (pattern is Ast.NondestructuringPattern nondestructuring)
        {
            this.Fragmented_VerifyNondestructuringPattern3(nondestructuring);
        }
        else if (pattern is Ast.RecordDestructuringPattern recordDestructuring)
        {
            this.Fragmented_VerifyRecordDestructuringPattern3(recordDestructuring);
        }
        else
        {
            this.Fragmented_VerifyArrayDestructuringPattern3((Ast.ArrayDestructuringPattern) pattern);
        }
    }
}