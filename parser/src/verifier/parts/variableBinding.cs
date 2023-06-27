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
    //
    // the `forRequiredOrRestParam` parameter is used for
    // not requiring an initializer for certain bindings,
    // such as a function required parameter or rest parameter.
    private void VerifyVariableBinding
    (
        Ast.VariableBinding binding,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol inferType = null,
        bool forRequiredOrRestParam = false,
        bool canShadow = false
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
            if (binding.Init == null)
            {
                if (!forRequiredOrRestParam && inferType == null)
                {
                    VerifyError(binding.Pattern.Span.Value.Script, 138, binding.Pattern.Span.Value, new DiagnosticArguments {});
                }
            }
            else
            {
                init = VerifyExp(binding.Init);
            }
            VerifyDestructuringPattern(binding.Pattern, readOnly, output, visibility, inferType != null ? inferType : init != null ? init.StaticType : m_ModelCore.AnyType, canShadow);
        }
        else
        {
            VerifyDestructuringPattern(binding.Pattern, readOnly, output, visibility, null, canShadow);
            if (binding.Init != null && binding.Pattern.SemanticProperty != null)
            {
                init = LimitExpType(binding.Init, binding.Pattern.SemanticProperty.StaticType);
            }
        }

        // constant initialiser
        if (init is ConstantValue && binding.Pattern.SemanticProperty != null && init.StaticType == binding.Pattern.SemanticProperty.StaticType)
        {
            binding.Pattern.SemanticProperty.InitValue = init;
        }

        if (binding.Pattern.SemanticProperty != null && !forRequiredOrRestParam)
        {
            // if not in class frame,
            // the binding must have a constant initial value
            // or initializer.
            var notInClass = !(this.m_Frame is ClassFrame);
            var noInitialValueOrInit = binding.Pattern.SemanticProperty.InitValue == null && binding.Init == null;
            if (notInClass && noInitialValueOrInit)
            {
                VerifyError(binding.Pattern.Span.Value.Script, 244, binding.Span.Value, new DiagnosticArguments {});
            }
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
            // WARNING: untyped
            if (inferType == null)
            {
                Warn(binding.Pattern.Span.Value.Script, 249, binding.Pattern.Span.Value, new DiagnosticArguments {});
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
            init = VerifyConstantExpAsValue(binding.Init, true);
            VerifyDestructuringPattern(binding.Pattern, false, output, Visibility.Public, init?.StaticType ?? inferType ?? m_ModelCore.AnyType);
        }
        else
        {
            VerifyDestructuringPattern(binding.Pattern, false, output, Visibility.Public, inferType ?? m_ModelCore.AnyType);
            if (binding.Pattern.SemanticProperty != null)
            {
                LimitConstantExpType(binding.Init, binding.Pattern.SemanticProperty.StaticType);
            }
        }

        // constant initialiser
        if (init is ConstantValue && binding.Pattern.SemanticProperty != null && init.StaticType == binding.Pattern.SemanticProperty.StaticType)
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
            // WARNING: untyped
            if (inferType == null)
            {
                Warn(binding.Pattern.Span.Value.Script, 249, binding.Pattern.Span.Value, new DiagnosticArguments {});
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

    /// <summary>
    /// Defines or re-uses a variable. <c>type</c> can be null
    /// for partially defined variables.
    /// Returns null if the variable is a duplicate.
    /// </summary>
    private Symbol DefineOrReuseVariable(string name, Properties output, Symbol type, Span span, bool readOnly, Visibility visibility, bool canShadow = false)
    {
        Symbol newDefinition = null;
        var previousDefinition = output[name];

        if (previousDefinition != null)
        {
            newDefinition = previousDefinition is VariableSlot ? previousDefinition : null;

            // ERROR: duplicate definition
            if (!m_Options.AllowDuplicates || newDefinition == null)
            {
                VerifyError(null, 139, span, new DiagnosticArguments { ["name"] = name });
                newDefinition = null;
            }
        }
        else
        {
            newDefinition = m_ModelCore.Factory.VariableSlot(name, readOnly, type);
            newDefinition.Visibility = visibility;
            newDefinition.InitValue ??= type?.DefaultValue;
            newDefinition.AllowsShadowing = canShadow;

            // shadowing
            if (newDefinition.ParentDefinition == null && m_Frame != null)
            {
                var shadowed = m_Frame.Properties[name];
                if (shadowed != null && !shadowed.AllowsShadowing)
                {
                    VerifyError(null, 265, span, new DiagnosticArguments {["name"] = name});
                }
            }

            output[name] = newDefinition;
        }
        return newDefinition;
    }
}