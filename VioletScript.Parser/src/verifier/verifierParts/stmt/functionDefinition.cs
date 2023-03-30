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
    private void VerifyFunctionDefinition(Ast.FunctionDefinition defn)
    {
        if (defn.Generics != null)
        {
            VerifyError(null, 226, defn.Id.Span.Value, new DiagnosticArguments {});
        }

        var common = defn.Common;

        Symbol prevActivation = m_Frame.FindActivation();
        Symbol activation = m_ModelCore.Factory.ActivationFrame();
        common.SemanticActivation = activation;
        // inherit "this"
        activation.ActivationThisOrThisAsStaticType = prevActivation?.ActivationThisOrThisAsStaticType;

        // define identifier partially.
        // the identifier's static type is resolved before
        // the body of the function is resolved.
        var thisFunctionVariable = m_ModelCore.Factory.VariableSlot(defn.Id.Name, true, null);
        defn.Id.SemanticSymbol = thisFunctionVariable;
        activation.Properties[defn.Id.Name] = thisFunctionVariable;

        Symbol methodSlot = m_ModelCore.Factory.MethodSlot(defn.Id.Name, null,
                (common.UsesAwait ? MethodSlotFlags.UsesAwait : 0)
            |   (common.UsesYield ? MethodSlotFlags.UsesYield : 0));

        bool valid = true;
        Symbol signatureType = null;

        // resolve common before pushing to method slot stack,
        // since its type is unknown.
        List<NameAndTypePair> signatureType_params = null;
        List<NameAndTypePair> signatureType_optParams = null;
        NameAndTypePair? signatureType_restParam = null;
        Symbol signatureType_returnType = null;
        if (common.Params != null)
        {
            signatureType_params = new List<NameAndTypePair>();
            var actualCount = common.Params.Count();
            for (int i = 0; i < actualCount; ++i)
            {
                var binding = common.Params[i];
                FRequiredParam_VerifyVariableBinding(binding, activation.Properties, null);
                var name = binding.Pattern is Ast.BindPattern p ? p.Name : "_";
                signatureType_params.Add(new NameAndTypePair(name, binding.Pattern.SemanticProperty.StaticType));
            }
        }
        if (common.OptParams != null)
        {
            signatureType_optParams = new List<NameAndTypePair>();
            var actualCount = common.OptParams.Count();
            for (int i = 0; i < actualCount; ++i)
            {
                var binding = common.OptParams[i];
                FOptParam_VerifyVariableBinding(binding, activation.Properties, null);
                var name = binding.Pattern is Ast.BindPattern p ? p.Name : "_";
                signatureType_optParams.Add(new NameAndTypePair(name, binding.Pattern.SemanticProperty.StaticType));
            }
        }
        // type of a rest parameter must be * or an Array
        if (common.RestParam != null)
        {
            var binding = common.RestParam;
            FRestParam_VerifyVariableBinding(binding, activation.Properties, null);
            var name = binding.Pattern is Ast.BindPattern p ? p.Name : "_";
            signatureType_restParam = new NameAndTypePair(name, binding.Pattern.SemanticProperty.StaticType);
        }
        if (common.ReturnType != null)
        {
            signatureType_returnType = VerifyTypeExp(common.ReturnType);
            if (signatureType_returnType == null)
            {
                valid = false;
                signatureType_returnType = m_ModelCore.AnyType;
            }
        }
        else
        {
            signatureType_returnType = m_ModelCore.AnyType;
        }

        // - if the function uses 'await', automatically wrap its return to Promise.
        // - if the function uses 'yield', automatically wrap its return to Generator.
        if (common.UsesAwait && !signatureType_returnType.IsInstantiationOf(m_ModelCore.PromiseType))
        {
            signatureType_returnType = m_ModelCore.Factory.InstantiatedType(m_ModelCore.PromiseType, new Symbol[]{signatureType_returnType});
        }
        else if (common.UsesYield && !signatureType_returnType.IsInstantiationOf(m_ModelCore.GeneratorType))
        {
            signatureType_returnType = m_ModelCore.Factory.InstantiatedType(m_ModelCore.GeneratorType, new Symbol[]{signatureType_returnType});
        }

        // ignore "throws" clause
        if (common.ThrowsType != null)
        {
            VerifyTypeExp(common.ThrowsType);
        }

        // get result type
        signatureType = m_ModelCore.Factory.FunctionType
        (
            signatureType_params?.ToArray(),
            signatureType_optParams?.ToArray(),
            signatureType_restParam,
            signatureType_returnType
        );

        defn.Id.SemanticSymbol.StaticType = signatureType;

        EnterFrame(activation);
        m_MethodSlotStack.Push(methodSlot);

        // resolve body.
        VerifyFunctionBody(defn.Id.Span.Value, common.Body, methodSlot);

        m_MethodSlotStack.Pop();
        ExitFrame();

        doFooBarQuxBaz();
    } // function definition
}