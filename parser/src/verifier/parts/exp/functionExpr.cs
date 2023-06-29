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
    private Symbol VerifyFunctionExp(Ast.FunctionExpression exp, Symbol expectedType)
    {
        var common = exp.Common;
        Symbol inferType = null;
        if (expectedType is UnionType)
        {
            var functionTypes = expectedType.UnionMemberTypes.Where(t => t is FunctionType);
            inferType = functionTypes.FirstOrDefault();
        }
        inferType ??= (expectedType is FunctionType ? expectedType : null);

        // if expected type is an union with more than one function type,
        // then do not infer signature types.
        int nOfInferFunctionTypes = expectedType is UnionType
            ? expectedType.UnionMemberTypes.Where(t => t is FunctionType).Count()
            : expectedType is FunctionType
            ? 1
            : 0;
        if (nOfInferFunctionTypes > 1)
        {
            inferType = null;
            nOfInferFunctionTypes = 0;
        }

        Symbol prevActivation = m_Frame.FindActivation();
        Symbol activation = m_ModelCore.Factory.ActivationFrame();
        common.SemanticActivation = activation;
        // inherit "this"
        activation.ActivationThisOrThisAsStaticType = prevActivation?.ActivationThisOrThisAsStaticType;

        // define identifier partially.
        // the identifier's static type is resolved before
        // the body of the function is resolved.
        if (exp.Id != null)
        {
            var variable = m_ModelCore.Factory.VariableSlot(exp.Id.Name, true, null);
            exp.Id.SemanticSymbol = variable;
            activation.Properties[exp.Id.Name] = variable;
        }

        Symbol methodSlot = m_ModelCore.Factory.MethodSlot("", null,
                (common.UsesAwait ? MethodSlotFlags.UsesAwait : 0)
            |   (common.UsesYield ? MethodSlotFlags.UsesYield : 0));

        bool valid = true;
        Symbol resultType = null;

        // resolve common before pushing to method slot stack,
        // since its type is unknown.
        List<NameAndTypePair> resultType_params = null;
        List<NameAndTypePair> resultType_optParams = null;
        NameAndTypePair? resultType_restParam = null;
        Symbol resultType_returnType = null;
        bool sameInfReqParamQty = true;
        if (common.Params != null)
        {
            resultType_params = new List<NameAndTypePair>();
            var actualCount = common.Params.Count();
            if (inferType != null && inferType.FunctionCountOfRequiredParameters != actualCount)
            {
                sameInfReqParamQty = false;
            }
            for (int i = 0; i < actualCount; ++i)
            {
                var binding = common.Params[i];
                NameAndTypePair? paramInferNameAndType = inferType != null && inferType.FunctionHasRequiredParameters && i < inferType.FunctionCountOfRequiredParameters ? inferType.FunctionRequiredParameters[i] : null;
                FRequiredParam_VerifyVariableBinding(binding, activation.Properties, paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Type : null);
                var name = paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Name : binding.Pattern is Ast.NondestructuringPattern p ? p.Name : "_";
                resultType_params.Add(new NameAndTypePair(name, binding.Pattern.SemanticProperty?.StaticType ?? this.m_ModelCore.AnyType));
            }
        }
        else if (inferType != null && inferType.FunctionHasRequiredParameters)
        {
            sameInfReqParamQty = false;
        }
        bool sameInfOptParamQty = true;
        if (common.OptParams != null)
        {
            resultType_optParams = new List<NameAndTypePair>();
            var actualCount = common.OptParams.Count();
            if (inferType != null && inferType.FunctionCountOfOptParameters != actualCount)
            {
                sameInfOptParamQty = false;
            }
            for (int i = 0; i < actualCount; ++i)
            {
                var binding = common.OptParams[i];
                NameAndTypePair? paramInferNameAndType = null;
                if (sameInfReqParamQty && sameInfOptParamQty)
                {
                    paramInferNameAndType = inferType != null && inferType.FunctionHasOptParameters && i < inferType.FunctionCountOfOptParameters ? inferType.FunctionOptParameters[i] : null;
                }
                FOptParam_VerifyVariableBinding(binding, activation.Properties, paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Type : null);
                var name = paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Name : binding.Pattern is Ast.NondestructuringPattern p ? p.Name : "_";
                resultType_optParams.Add(new NameAndTypePair(name, binding.Pattern.SemanticProperty?.StaticType ?? this.m_ModelCore.AnyType));
            }
        }
        else if (inferType != null && inferType.FunctionHasOptParameters)
        {
            sameInfOptParamQty = false;
        }
        // type of a rest parameter must be * or an Array
        if (common.RestParam != null)
        {
            var binding = common.RestParam;
            NameAndTypePair? paramInferNameAndType = null;
            if (sameInfReqParamQty && sameInfOptParamQty)
            {
                paramInferNameAndType = inferType != null ? inferType.FunctionRestParameter : null;
            }
            FRestParam_VerifyVariableBinding(binding, activation.Properties, paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Type : null);
            var name = paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Name : binding.Pattern is Ast.NondestructuringPattern p ? p.Name : "_";
            resultType_restParam = new NameAndTypePair(name, binding.Pattern.SemanticProperty?.StaticType ?? this.m_ModelCore.AnyType);
        }
        if (common.ReturnType != null)
        {
            resultType_returnType = VerifyTypeExp(common.ReturnType);
            if (resultType_returnType == null)
            {
                valid = false;
                resultType_returnType = m_ModelCore.AnyType;
            }
        }
        else
        {
            resultType_returnType = inferType?.FunctionReturnType ?? m_ModelCore.AnyType;
        }

        // if there is an inferred type and parameters were omitted,
        // add them to the resulting type if applicable.
        // this is done except if the expected type was an union with
        // more than one function type.
        if (nOfInferFunctionTypes == 1)
        {
            var r = FunctionExp_AddMissingParameters
            (
                resultType_params,
                resultType_optParams,
                resultType_restParam,
                inferType
            );
            resultType_params = r.Item1;
            resultType_optParams = r.Item2;
            resultType_restParam = r.Item3;
        }

        // - if the function uses 'await', automatically wrap its return to Promise.
        // - if the function uses 'yield', automatically wrap its return to Iterator.
        if (common.UsesAwait && !resultType_returnType.IsArgumentationOf(m_ModelCore.PromiseType))
        {
            resultType_returnType = m_ModelCore.Factory.TypeWithArguments(m_ModelCore.PromiseType, new Symbol[]{resultType_returnType});
        }
        else if (common.UsesYield && !resultType_returnType.IsArgumentationOf(m_ModelCore.IteratorType))
        {
            resultType_returnType = m_ModelCore.Factory.TypeWithArguments(m_ModelCore.IteratorType, new Symbol[]{resultType_returnType});
        }

        // get result type
        resultType = m_ModelCore.Factory.FunctionType
        (
            resultType_params?.ToArray(),
            resultType_optParams?.ToArray(),
            resultType_restParam,
            resultType_returnType
        );

        methodSlot.StaticType = resultType;

        // if identifier was defined, assign its static type.
        if (exp.Id != null)
        {
            exp.Id.SemanticSymbol.StaticType = resultType;
        }

        EnterFrame(activation);
        m_MethodSlotStack.Push(methodSlot);

        // resolve body.
        VerifyFunctionBody(exp.Id != null ? exp.Id.Span.Value : exp.Span.Value, common.Body, methodSlot);

        m_MethodSlotStack.Pop();
        ExitFrame();

        exp.SemanticSymbol = valid ? m_ModelCore.Factory.FunctionExpValue(resultType) : null;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // function expression

    private
    (
        List<NameAndTypePair>,
        List<NameAndTypePair>,
        NameAndTypePair?
    )
    FunctionExp_AddMissingParameters
    (
        List<NameAndTypePair> resultType_params,
        List<NameAndTypePair> resultType_optParams,
        NameAndTypePair? resultType_restParam,
        Symbol inferType
    )
    {
        var mixedResultParams = RequiredOrOptOrRestParam.FromLists
        (
            resultType_params,
            resultType_optParams,
            resultType_restParam
        );
        var mixedInferParams = RequiredOrOptOrRestParam.FromType(inferType);
        var compatible = mixedResultParams.Count() <= mixedInferParams.Count();
        if (compatible)
        {
            for (int i = 0; i < mixedResultParams.Count(); ++i)
            {
                if (mixedResultParams[i].Kind != mixedInferParams[i].Kind)
                {
                    compatible = false;
                    break;
                }
            }
            if (compatible)
            {
                for (int i = mixedResultParams.Count(); i < mixedInferParams.Count(); ++i)
                {
                    mixedResultParams.Add(mixedInferParams[i]);
                }
            }
        }

        return RequiredOrOptOrRestParam.SeparateKinds(mixedResultParams);
    } // FunctionExp_AddMissingParameters
}