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
    private void Fragmented_VerifyNamespaceAliasDefinition(Ast.NamespaceAliasDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase2)
        {
            defn.SemanticSurroundingFrame = m_Frame;
            m_ImportOrAliasDirectives.Add(defn);
        }
        else if (phase == VerifyPhase.ImportOrAliasPhase1)
        {
            // if successful, remove directive from 'm_ImportOrAliasDirectives'.
            // do not report diagnostics.
            var previousFrame = m_Frame;
            m_Frame = defn.SemanticSurroundingFrame;
            Fragmented_VerifyNamespaceAliasDefinition1(defn);
            m_Frame = previousFrame;
        }
        else if (phase == VerifyPhase.ImportOrAliasPhase2)
        {
            // report any diagnostics.
            var previousFrame = m_Frame;
            m_Frame = defn.SemanticSurroundingFrame;
            Fragmented_VerifyNamespaceAliasDefinition2(defn);
            m_Frame = previousFrame;
        }
    }

    private void Fragmented_VerifyNamespaceAliasDefinition1(Ast.NamespaceAliasDefinition defn)
    {
        var right = VerifyConstantExp(defn.Expression, false);
        if (right == null)
        {
            return;
        }
        m_ImportOrAliasDirectives.Remove(defn);
        if (!(right is Namespace))
        {
            // VerifyError: not a namespace
            VerifyError(null, 222, defn.Expression.Span.Value, new DiagnosticArguments {});
            return;
        }
        var previousDefinition = m_Frame.Properties[defn.Id.Name];
        if (previousDefinition != null)
        {
            if (m_Options.AllowDuplicates && previousDefinition is Alias && previousDefinition.AliasToSymbol is Namespace)
            {
                defn.SemanticAlias = previousDefinition;
            }
            else
            {
                // VerifyError: duplicate
                VerifyError(null, 139, defn.Id.Span.Value, new DiagnosticArguments {["name"] = defn.Id.Name});
            }
        }
        else
        {
            var alias = m_ModelCore.Factory.Alias(defn.Id.Name, right);
            alias.Visibility = defn.SemanticVisibility;
            m_Frame.Properties[alias.Name] = alias;
            defn.SemanticAlias = alias;
        }
    }

    private void Fragmented_VerifyNamespaceAliasDefinition2(Ast.NamespaceAliasDefinition defn)
    {
        var right = VerifyConstantExp(defn.Expression, true);
        if (right == null)
        {
            return;
        }
        if (!(right is Namespace))
        {
            // VerifyError: not a namespace
            VerifyError(null, 222, defn.Expression.Span.Value, new DiagnosticArguments {});
            return;
        }
        var previousDefinition = m_Frame.Properties[defn.Id.Name];
        if (previousDefinition != null)
        {
            if (m_Options.AllowDuplicates && previousDefinition is Alias && previousDefinition.AliasToSymbol is Namespace)
            {
                defn.SemanticAlias = previousDefinition;
            }
            else
            {
                // VerifyError: duplicate
                VerifyError(null, 139, defn.Id.Span.Value, new DiagnosticArguments {["name"] = defn.Id.Name});
            }
        }
        else
        {
            var alias = m_ModelCore.Factory.Alias(defn.Id.Name, right);
            alias.Visibility = defn.SemanticVisibility;
            m_Frame.Properties[alias.Name] = alias;
            defn.SemanticAlias = alias;
        }
    }
}