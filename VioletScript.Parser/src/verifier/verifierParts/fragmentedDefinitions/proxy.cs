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
    private void Fragmented_VerifyProxyDefinition(Ast.ProxyDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            Fragmented_VerifyProxyDefinition1(defn);
        }
        else if (phase == VerifyPhase.Phase2)
        {
            Fragmented_VerifyProxyDefinition2(defn);
        }
        else if (phase == VerifyPhase.Phase3)
        {
            Fragmented_VerifyProxyDefinition3(defn);
        }
        // VerifyPhase.Phase7
        else if (phase == VerifyPhase.Phase7)
        {
            Fragmented_VerifyProxyDefinition7(defn);
        }
    }

    private void Fragmented_VerifyProxyDefinition1(Ast.ProxyDefinition defn)
    {
        // conversion proxies are defined in phase 2
        if (defn.Operator.IsConversionProxy)
        {
            return;
        }
        var type = m_Frame.TypeFromFrame;
        // do not allow duplicate proxy
        Symbol prevProxy = type.Delegate.Proxies.ContainsKey(defn.Operator) ? type.Delegate.Proxies[defn.Operator] : null;
        if (prevProxy == null)
        {
            var proxy = m_ModelCore.Factory.MethodSlot("_", null, defn.SemanticFlags(type));
            proxy.ParentDefinition = type;
            type.Delegate.Proxies[defn.Operator] = proxy;
            defn.SemanticMethodSlot = proxy;
            defn.Common.SemanticActivation = this.m_ModelCore.Factory.ActivationFrame();
            // set `this`
            defn.Common.SemanticActivation.ActivationThisOrThisAsStaticType = defn.Operator.ProxyUsesThisLiteral ? this.m_ModelCore.Factory.ThisValue(type) : null;
        }
        else
        {
            // ERROR: duplicate
            VerifyError(null, 258, defn.Id.Span.Value, new DiagnosticArguments {});
        }
    }

    private void Fragmented_VerifyProxyDefinition2(Ast.ProxyDefinition defn)
    {
        // define conversion proxies.
        if (defn.Operator.IsConversionProxy)
        {
            this.Fragmented_VerifyProxyDefinition2Conversion(defn);
            return;
        }
        var method = defn.SemanticMethodSlot;
        var type = m_Frame.TypeFromFrame;
        if (method == null)
        {
            return;
        }
        method.StaticType = this.Fragmented_ResolveFunctionSignature(defn.Common, defn.Id.Span.Value);
        if (!method.StaticType.IsValidProxySignature(defn.Operator, type))
        {
            // ERROR: illegal proxy signature
            VerifyError(null, 259, defn.Id.Span.Value, new DiagnosticArguments {});
            method.StaticType = null;
            type.Delegate.Proxies[defn.Operator] = null;
            defn.SemanticMethodSlot = null;
            return;
        }
    }

    private void Fragmented_VerifyProxyDefinition2Conversion(Ast.ProxyDefinition defn)
    {
        var targetType = m_Frame.TypeFromFrame;

        // resolve signature
        var conversionSignature = this.Fragmented_ResolveFunctionSignature(defn.Common, defn.Id.Span.Value);

        // validate conversion signature
        Symbol fromType = null;
        if (!conversionSignature.IsValidProxyConversionSignature(targetType))
        {
            // ERROR: illegal proxy signature
            VerifyError(null, 259, defn.Id.Span.Value, new DiagnosticArguments {});
            return;
        }

        // do not allow duplicate and create method and add it
        // to set of proxies.
        var proxiesSet = defn.Operator == Operator.ProxyToConvertExplicit ? targetType.Delegate.ExplicitConversionProxies : targetType.Delegate.ImplicitConversionProxies;
        Symbol prevProxy = proxiesSet.ContainsKey(fromType) ? proxiesSet[fromType] : null;
        if (prevProxy == null)
        {
            var proxy = m_ModelCore.Factory.MethodSlot("_", conversionSignature, defn.SemanticFlags(targetType));
            proxy.ParentDefinition = targetType;
            proxiesSet[fromType] = proxy;
            defn.SemanticMethodSlot = proxy;
            defn.Common.SemanticActivation = this.m_ModelCore.Factory.ActivationFrame();
            // set `this`
            defn.Common.SemanticActivation.ActivationThisOrThisAsStaticType = null;
        }
        else
        {
            // ERROR: duplicate
            VerifyError(null, 258, defn.Id.Span.Value, new DiagnosticArguments {});
        }
    }

    private void Fragmented_VerifyProxyDefinition3(Ast.ProxyDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }
        var type = m_Frame.TypeFromFrame;
        // set proxy must be compatible with get proxy
        if (defn.Operator == Operator.ProxyToSetIndex)
        {
            var getIdxProxy = type.Delegate.Proxies.ContainsKey(Operator.ProxyToGetIndex) ? type.Delegate.Proxies[Operator.ProxyToGetIndex] : null;
            if (getIdxProxy != null && method.StaticType.FunctionRequiredParameters[0].Type != getIdxProxy.StaticType.FunctionRequiredParameters[0].Type)
            {
                VerifyError(null, 260, defn.Id.Span.Value, new DiagnosticArguments {["type"] = getIdxProxy.StaticType.FunctionRequiredParameters[0].Type});
            }
        }
        // delete proxy must be compatible with get proxy
        else if (defn.Operator == Operator.ProxyToDeleteIndex)
        {
            var getIdxProxy = type.Delegate.Proxies.ContainsKey(Operator.ProxyToGetIndex) ? type.Delegate.Proxies[Operator.ProxyToGetIndex] : null;
            if (getIdxProxy == null || method.StaticType.FunctionRequiredParameters[0].Type != getIdxProxy.StaticType.FunctionRequiredParameters[0].Type)
            {
                VerifyError(null, 261, defn.Id.Span.Value, new DiagnosticArguments {});
            }
        }
    }

    private void Fragmented_VerifyProxyDefinition7(Ast.ProxyDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }
        var type = m_Frame.TypeFromFrame;
        this.EnterFrame(defn.Common.SemanticActivation);
        this.Fragmented_VerifyFunctionDefinition7Params(defn.Common);
        // ignore "throws" clause
        if (defn.Common.ThrowsType != null)
        {
            this.VerifyTypeExp(defn.Common.ThrowsType);
        }
        this.Fragmented_VerifyFunctionDefinition7Body(defn.Common, method, defn.Id.Span.Value);
        this.ExitFrame();
    }
}