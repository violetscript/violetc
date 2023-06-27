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
            this.Fragmented_VerifyFunctionDefinition3(defn);
        }
        // VerifyPhase.Phase7
        else if (phase == VerifyPhase.Phase7)
        {
            this.Fragmented_VerifyFunctionDefinition7(defn);
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
            defn.Common.SemanticActivation = this.m_ModelCore.Factory.ActivationFrame();
            // set `this`
            defn.Common.SemanticActivation.ActivationThisOrThisAsStaticType = isTypeStatic ? this.m_ModelCore.Factory.ClassStaticThis(parentDefinition) : parentDefinition is Type ? this.m_ModelCore.Factory.ThisValue(parentDefinition) : null;

            if (defn.Generics != null)
            {
                this.EnterFrame(defn.Common.SemanticActivation);
                defn.SemanticMethodSlot.TypeParameters = FragmentedA_VerifyTypeParameters(defn.Generics, defn.Common.SemanticActivation.Properties, defn.SemanticMethodSlot);
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
        this.EnterFrame(defn.Common.SemanticActivation);

        if (defn.Generics != null)
        {
            FragmentedB_VerifyTypeParameters(method.TypeParameters, defn.Generics, defn.Common.SemanticActivation.Properties);
        }

        // resolve signature
        method.StaticType = this.Fragmented_ResolveFunctionSignature(defn.Common, defn.Id.Span.Value);

        this.ExitFrame();
    }

    private Symbol Fragmented_ResolveFunctionSignature(Ast.FunctionCommon common, Span idSpan, bool forConstructor = false)
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
            @optParams = new List<NameAndTypePair>();
            foreach (var binding in common.OptParams)
            {
                var name = binding.Pattern is Ast.NondestructuringPattern p ? p.Name : "_";
                if (binding.Pattern.Type == null)
                {
                    VerifyError(binding.Pattern.Span.Value.Script, 138, binding.Pattern.Span.Value, new DiagnosticArguments {});
                }
                @optParams.Add(new NameAndTypePair(name, binding.Pattern.Type == null ? this.m_ModelCore.AnyType : this.VerifyTypeExp(binding.Pattern.Type) ?? this.m_ModelCore.AnyType));
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
            if (!forConstructor)
            {
                Warn(idSpan.Script, 250, idSpan, new DiagnosticArguments {});
            }
        }
        else
        {
            returnType = this.VerifyTypeExp(common.ReturnType);
        }
        returnType ??= forConstructor ? this.m_ModelCore.UndefinedType : this.m_ModelCore.AnyType;
        return this.m_ModelCore.Factory.FunctionType(@params?.ToArray(), optParams?.ToArray(), restParam, returnType);
    }

    private void Fragmented_VerifyFunctionDefinition3(Ast.FunctionDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }
        // overriding and shadowing
        var subtype = this.m_Frame.TypeFromFrame;
        var superType = subtype?.SuperType;
        if (superType == null)
        {
            return;
        }
        // override method
        if (defn.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Override))
        {
            this.Fragmented_VerifyOverride(defn.Id.Span.Value, subtype, method);
            return;
        }
        var methodName = method.Name;
        if (SingleInheritanceInstancePropertiesHierarchy.HasProperty(superType, methodName))
        {
            this.VerifyError(defn.Id.Span.Value.Script, 246, defn.Id.Span.Value, new DiagnosticArguments {["name"] = methodName});
        }
    }

    private void Fragmented_VerifyOverride(Span idSpan, Symbol subtype, Symbol method)
    {
        var overrideResult = MethodOverride.OverrideSingle(subtype, method);
        if (overrideResult is MustOverrideAMethodIssue mustOverrideIssue)
        {
            this.VerifyError(null, 251, idSpan, new DiagnosticArguments {["name"] = mustOverrideIssue.Name});
        }
        else if (overrideResult is CannotOverrideGenericMethodIssue cantOverrideGenericIssue)
        {
            this.VerifyError(null, 252, idSpan, new DiagnosticArguments {["name"] = cantOverrideGenericIssue.Name});
        }
        else if (overrideResult is IncompatibleOverrideSignatureIssue incompatibleIssue)
        {
            this.VerifyError(null, 253, idSpan, new DiagnosticArguments {["type"] = incompatibleIssue.ExpectedSignature});
        }
        else if (overrideResult is CannotOverrideFinalMethodIssue finalIssue)
        {
            this.VerifyError(null, 254, idSpan, new DiagnosticArguments {["name"] = finalIssue.Name});
        }
        else if (overrideResult != null)
        {
            throw new Exception("Unimplemented.");
        }
    }

    private void Fragmented_VerifyFunctionDefinition7(Ast.FunctionDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }

        var isClassProp = this.m_Frame is ClassFrame && !defn.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Static);
        if (defn.Decorators != null && isClassProp)
        {
            var decoratorFnType = this.m_ModelCore.Factory.FunctionType(new NameAndTypePair[]{new NameAndTypePair("obj", this.m_Frame.TypeFromFrame), new NameAndTypePair("name", this.m_ModelCore.StringType)}, null, null, this.m_ModelCore.UndefinedType);
            foreach (var decorator in defn.Decorators)
            {
                this.LimitExpType(decorator, decoratorFnType);
            }
        }

        this.EnterFrame(defn.Common.SemanticActivation);
        this.Fragmented_VerifyFunctionDefinition7Params(defn.Common);
        this.Fragmented_VerifyFunctionDefinition7Body(defn.Common, method, defn.Id.Span.Value);
        this.ExitFrame();
    }

    private void Fragmented_VerifyFunctionDefinition7Params(Ast.FunctionCommon common)
    {
        Symbol activation = common.SemanticActivation;

        if (common.Params != null)
        {
            foreach (var binding in common.Params)
            {
                this.VerifyVariableBinding(binding, false, activation.Properties, Visibility.Public, null, true);
            }
        }
        if (common.OptParams != null)
        {
            foreach (var binding in common.OptParams)
            {
                this.VerifyVariableBinding(binding, false, activation.Properties, Visibility.Public);
            }
        }
        if (common.RestParam != null)
        {
            this.VerifyVariableBinding(common.RestParam, false, activation.Properties, Visibility.Public, null, true);
        }
    }

    private void Fragmented_VerifyFunctionDefinition7Body(Ast.FunctionCommon common, Symbol method, Span idSpan)
    {
        this.m_MethodSlotStack.Push(method);
        this.VerifyFunctionBody(idSpan, common.Body, method);
        this.m_MethodSlotStack.Pop();
    }
}