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
    private void VerifyVariableBinding
    (
        Ast.VariableBinding binding,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol inferType = null
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
            else
            {
                init = VerifyExp(binding.Init);
            }
            VerifyDestructuringPattern(binding.Pattern, readOnly, output, visibility, inferType != null ? inferType : init != null ? init.StaticType : m_ModelCore.AnyType);
        }
        else
        {
            VerifyDestructuringPattern(binding.Pattern, readOnly, output, visibility);
            if (binding.Init != null)
            {
                init = LimitExpType(binding.Init, binding.Pattern.SemanticProperty.StaticType);
            }
        }

        if (init is ConstantValue && init.StaticType == binding.Pattern.SemanticProperty.StaticType)
        {
            binding.Pattern.SemanticProperty.InitValue = init;
        }

        binding.SemanticVerified = true;
    }

    // verify a variable binding for a
    // function required parameter; ensure:
    // - destructuring pattern is valid.
    private void FRequiredParam_VerifyVariableBinding
    (
        Ast.VariableBinding binding,
        Properties output,
        Symbol inferType
    )
    {
        if (binding.SemanticVerified)
        {
            return;
        }
        if (binding.Pattern.Type == null)
        {
            // VerifyError
            if (inferType == null)
            {
                VerifyError(binding.Pattern.Span.Value.Script, 138, binding.Pattern.Span.Value, new DiagnosticArguments {});
            }
            VerifyDestructuringPattern(binding.Pattern, false, output, Visibility.Public, inferType ?? m_ModelCore.AnyType);
        }
        else
        {
            VerifyDestructuringPattern(binding.Pattern, false, output, Visibility.Public);
        }

        binding.SemanticVerified = true;
    }

    // verify a variable binding for a
    // function optional parameter; ensure:
    // - destructuring pattern is valid.
    private void FOptParam_VerifyVariableBinding
    (
        Ast.VariableBinding binding,
        Properties output,
        Symbol inferType
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
            init = VerifyConstantExpAsValue(binding.Init, false);
            VerifyDestructuringPattern(binding.Pattern, false, output, Visibility.Public, init?.StaticType ?? m_ModelCore.AnyType);
        }
        else
        {
            VerifyDestructuringPattern(binding.Pattern, false, output, Visibility.Public, inferType ?? m_ModelCore.AnyType);
            LimitConstantExpType(binding.Init, binding.Pattern.SemanticProperty.StaticType);
        }

        if (init is ConstantValue && init.StaticType == binding.Pattern.SemanticProperty.StaticType)
        {
            binding.Pattern.SemanticProperty.InitValue = init;
        }

        binding.SemanticVerified = true;
    }

    // verify a variable binding for a
    // function rest parameter; ensure:
    // - destructuring pattern is valid.
    private void FRestParam_VerifyVariableBinding
    (
        Ast.VariableBinding binding,
        Properties output,
        Symbol inferType
    )
    {
        if (binding.SemanticVerified)
        {
            return;
        }
        if (binding.Pattern.Type == null)
        {
            // VerifyError
            if (inferType == null)
            {
                VerifyError(binding.Pattern.Span.Value.Script, 138, binding.Pattern.Span.Value, new DiagnosticArguments {});
            }
            VerifyDestructuringPattern(binding.Pattern, false, output, Visibility.Public, inferType ?? m_ModelCore.AnyType);
        }
        else
        {
            VerifyDestructuringPattern(binding.Pattern, false, output, Visibility.Public);
        }

        binding.SemanticVerified = true;

        if (binding.Pattern.SemanticProperty != null && binding.Pattern.SemanticProperty.StaticType != m_ModelCore.AnyType && !binding.Pattern.SemanticProperty.StaticType.IsInstantiationOf(m_ModelCore.ArrayType))
        {
            VerifyError(binding.Pattern.Span.Value.Script, 185, binding.Pattern.Span.Value, new DiagnosticArguments {});
        }
    }
}