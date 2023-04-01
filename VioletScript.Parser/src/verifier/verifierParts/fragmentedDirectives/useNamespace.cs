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
    private void Fragmented_VerifyUseNamespaceDirective(Ast.UseNamespaceStatement drtv, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase2)
        {
            drtv.SemanticSurroundingFrame = m_Frame;
            m_ImportOrAliasDirectives.Add(drtv);
        }
        else if (phase == VerifyPhase.ImportOrAliasPhase1)
        {
            // if successful, remove directive from 'm_ImportOrAliasDirectives'.
            // do not report diagnostics.
            Fragmented_VerifyUseNamespaceDirective1(drtv);
        }
        else if (phase == VerifyPhase.ImportOrAliasPhase2)
        {
            // report any diagnostics.
            Fragmented_VerifyUseNamespaceDirective2(drtv);
        }
    }

    private void Fragmented_VerifyUseNamespaceDirective1(Ast.UseNamespaceStatement drtv)
    {
        var ns = VerifyConstantExp(drtv.Expression, false);
        if (ns == null)
        {
            return;
        }
        if (!(ns is Namespace))
        {
            return;
        }
        m_ImportOrAliasDirectives.Remove(drtv);
        m_Frame.OpenNamespace(ns);
        drtv.SemanticOpenedNamespace = ns;
    }

    private void Fragmented_VerifyUseNamespaceDirective2(Ast.UseNamespaceStatement drtv)
    {
        var ns = VerifyConstantExp(drtv.Expression, true);
        if (ns == null)
        {
            return;
        }
        if (!(ns is Namespace))
        {
            VerifyError(null, 222, drtv.Expression.Span.Value, new DiagnosticArguments {});
            return;
        }
        m_Frame.OpenNamespace(ns);
        drtv.SemanticOpenedNamespace = ns;
    }
}