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
        method.StaticType = this.Fragmented_ResolveFunctionSignature(defn.Common, defn.Id.Span.Value);

        this.ExitFrame();
    }

    private Symbol Fragmented_ResolveFunctionSignature(Ast.FunctionCommon common, Span idSpan)
    {
        List<NameAndTypePair> @params = null;
        List<NameAndTypePair> optParams = null;
        NameAndTypePair? restParam = null;
        Symbol returnType = null;
        if (common.Params != null)
        {
            @params = new List<NameAndTypePair>();
            foreach (var binding in common.Params)
            {
                var name = binding.Pattern is Ast.NondestructuringPattern p ? p.Name : "_";
                if (binding.Pattern.Type == null)
                {
                    VerifyError(binding.Pattern.Span.Value.Script, 138, binding.Pattern.Span.Value, new DiagnosticArguments {});
                }
                @params.Add(new NameAndTypePair(name, binding.Pattern.Type == null ? this.m_ModelCore.AnyType : this.VerifyTypeExp(binding.Pattern.Type) ?? this.m_ModelCore.AnyType));
            }
        }
        else if (common.OptParams != null)
        {
            @params = new List<NameAndTypePair>();
            foreach (var binding in common.OptParams)
            {
                var name = binding.Pattern is Ast.NondestructuringPattern p ? p.Name : "_";
                if (binding.Pattern.Type == null)
                {
                    VerifyError(binding.Pattern.Span.Value.Script, 138, binding.Pattern.Span.Value, new DiagnosticArguments {});
                }
                @params.Add(new NameAndTypePair(name, binding.Pattern.Type == null ? this.m_ModelCore.AnyType : this.VerifyTypeExp(binding.Pattern.Type) ?? this.m_ModelCore.AnyType));
            }
        }
        else if (common.RestParam != null)
        {
            var binding = common.RestParam;
            var name = binding.Pattern is Ast.NondestructuringPattern p ? p.Name : "_";
            var type = binding.Pattern.Type == null ? this.m_ModelCore.AnyType : this.VerifyTypeExp(binding.Pattern.Type) ?? this.m_ModelCore.AnyType;
            if (binding.Pattern.Type == null)
            {
                VerifyError(binding.Pattern.Span.Value.Script, 138, binding.Pattern.Span.Value, new DiagnosticArguments {});
            }
            if (binding.Pattern.SemanticProperty != null && binding.Pattern.SemanticProperty.StaticType != m_ModelCore.AnyType && !binding.Pattern.SemanticProperty.StaticType.IsInstantiationOf(m_ModelCore.ArrayType))
            {
                VerifyError(binding.Pattern.Span.Value.Script, 185, binding.Pattern.Span.Value, new DiagnosticArguments {});
                type = this.m_ModelCore.AnyType;
            }
            restParam = new NameAndTypePair(name, type);
        }
        if (common.ReturnType == null)
        {
            Warn(idSpan.Script, 250, idSpan, new DiagnosticArguments {});
        }
        else
        {
            returnType = this.VerifyTypeExp(common.ReturnType);
        }
        returnType ??= this.m_ModelCore.AnyType;
        return this.m_ModelCore.Factory.FunctionType(@params?.ToArray(), optParams?.ToArray(), restParam, returnType);
    }
}