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
        // it has been resolved by a separate method.
        if (defn.SemanticShadowFrame != null)
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
        this.Fragmented_VerifyDestructuringPattern2(binding.Pattern);
        // try resolving initializer as a constant expression
        if (binding.Init != null)
        {
            var val = this.VerifyConstantExpAsValue(binding.Init, false, binding.Pattern.SemanticProperty.StaticType);
            binding.Pattern.SemanticProperty.StaticType ??= val;
        }
    }

    private void Fragmented_VerifyDestructuringPattern2(Ast.DestructuringPattern pattern)
    {
        return pattern is Ast.NondestructuringPattern nondestructuring ? this.Fragmented_VerifyNondestructuringPattern2(nondestructuring)
            : pattern is Ast.RecordDestructuringPattern recordDestructuring ? this.Fragmented_VerifyRecordDestructuringPattern2(recordDestructuring) :
                this.Fragmented_VerifyArrayDestructuringPattern2((Ast.ArrayDestructuringPattern) pattern);
    }
}