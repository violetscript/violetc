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
    private void Fragmented_VerifyTypeDefinition(Ast.TypeDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            defn.SemanticSurroundingFrame = m_Frame;
            m_ImportOrAliasDirectives.Add(defn);
        }
        else if (phase == VerifyPhase.Phase2)
        {
            Fragmented_VerifyTypeDefinition3(defn);
        }
        else if (phase == VerifyPhase.ImportOrAliasPhase1)
        {
            // if successful, remove directive from 'm_ImportOrAliasDirectives'.
            // do not report diagnostics.
            var previousFrame = m_Frame;
            m_Frame = defn.SemanticSurroundingFrame;
            Fragmented_VerifyTypeDefinition1(defn);
            m_Frame = previousFrame;
        }
        else if (phase == VerifyPhase.ImportOrAliasPhase2)
        {
            // report any diagnostics.
            var previousFrame = m_Frame;
            m_Frame = defn.SemanticSurroundingFrame;
            Fragmented_VerifyTypeDefinition2(defn);
            m_Frame = previousFrame;
        }
    }

    private void Fragmented_VerifyTypeDefinition1(Ast.TypeDefinition defn)
    {
        Symbol[] typeParameters = null;
        if (defn.Generics != null && defn.SemanticRightFrame == null)
        {
            defn.SemanticRightFrame = m_ModelCore.Factory.Frame();
            typeParameters = FragmentedA_VerifyTypeParameters(defn.Generics, defn.SemanticRightFrame.Properties);
        }
        defn.SemanticRightFrame ??= m_ModelCore.Factory.Frame();
        EnterFrame(defn.SemanticRightFrame);
        var right = VerifyTypeExp(defn.Type, false, false);
        ExitFrame();

        if (right == null)
        {
            return;
        }

        m_ImportOrAliasDirectives.Remove(defn);

        Properties outputProps =
            m_Frame.NamespaceFromFrame != null ? m_Frame.NamespaceFromFrame.Properties
            : m_Frame.PackageFromFrame != null ? m_Frame.PackageFromFrame.Properties
            : m_Frame.Properties;

        var previousDefinition = outputProps[defn.Id.Name];

        if (previousDefinition != null && m_Options.AllowDuplicates && previousDefinition is Alias && previousDefinition.AliasToSymbol is Type)
        {
            defn.SemanticAlias = previousDefinition;
            return;
        }

        var alias = m_ModelCore.Factory.Alias(defn.Id.Name, right);
        alias.TypeParameters = typeParameters;
        defn.SemanticAlias = alias;

        if (previousDefinition != null)
        {
            VerifyError(null, 139, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = alias.Name });
        }
        else
        {
            outputProps[alias.Name] = alias;
        }
    }

    private void Fragmented_VerifyTypeDefinition2(Ast.TypeDefinition defn)
    {
        Symbol[] typeParameters = null;
        if (defn.Generics != null && defn.SemanticRightFrame == null)
        {
            defn.SemanticRightFrame = m_ModelCore.Factory.Frame();
            typeParameters = FragmentedA_VerifyTypeParameters(defn.Generics, defn.SemanticRightFrame.Properties);
        }
        defn.SemanticRightFrame ??= m_ModelCore.Factory.Frame();
        EnterFrame(defn.SemanticRightFrame);
        var right = VerifyTypeExp(defn.Type, false);
        ExitFrame();

        if (right == null)
        {
            return;
        }

        Properties outputProps =
            m_Frame.NamespaceFromFrame != null ? m_Frame.NamespaceFromFrame.Properties
            : m_Frame.PackageFromFrame != null ? m_Frame.PackageFromFrame.Properties
            : m_Frame.Properties;

        var previousDefinition = outputProps[defn.Id.Name];

        if (previousDefinition != null && m_Options.AllowDuplicates && previousDefinition is Alias && previousDefinition.AliasToSymbol is Type)
        {
            defn.SemanticAlias = previousDefinition;
            return;
        }

        var alias = m_ModelCore.Factory.Alias(defn.Id.Name, right);
        alias.TypeParameters = typeParameters;
        defn.SemanticAlias = alias;

        if (previousDefinition != null)
        {
            VerifyError(null, 139, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = alias.Name });
        }
        else
        {
            outputProps[alias.Name] = alias;
        }
    }

    private void Fragmented_VerifyTypeDefinition3(Ast.TypeDefinition defn)
    {
        var alias = defn.SemanticAlias;
        if (alias != null && defn.Generics != null)
        {
            FragmentedB_VerifyTypeParameters(alias.TypeParameters, defn.Generics, defn.SemanticRightFrame.Properties);
        }
    }
}