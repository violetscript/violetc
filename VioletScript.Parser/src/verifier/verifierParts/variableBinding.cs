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
    // verify a variable binding; ensure:
    // - destructuring pattern is valid.
    // - it has an initializer if it is read-only.
    public void VerifyVariableBinding
    (
        Ast.VariableBinding binding,
        bool readOnly,
        Properties output,
        Visibility visibility
    )
    {
        if (binding.SemanticVerified)
        {
            return;
        }
        Symbol init = null;
        // variable type must be inferred from the initializer.
        if (binding.Pattern.Type == null)
        {
            // VerifyError
            if (binding.Init == null)
            {
                VerifyError(binding.Pattern.Span.Value.Script, 138, binding.Pattern.Span.Value, new DiagnosticArguments {});
            }
            init = VerifyExp(binding.Init);
            VerifyDestructuringPattern(binding.Pattern, readOnly, output, visibility, init.StaticType);
        }
        else
        {
            VerifyDestructuringPattern(binding.Pattern, readOnly, output, visibility);
            if (binding.Init != null)
            {
                LimitExpType(binding.Init, binding.Pattern.SemanticProperty.StaticType);
            }
        }

        if (init is ConstantValue && init.StaticType == binding.Pattern.SemanticProperty)
        {
            binding.Pattern.SemanticProperty.InitValue = init;
        }

        binding.SemanticVerified = true;
    }
}