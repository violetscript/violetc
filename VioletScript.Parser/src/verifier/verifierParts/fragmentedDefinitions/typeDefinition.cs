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
        if (phase == VerifyPhase.Phase2)
        {
            defn.SemanticSurroundingFrame = m_Frame;
            m_ImportOrAliasDirectives.Add(defn);
        }
        else if (phase == VerifyPhase.ImportOrAliasPhase1)
        {
            // if successful, remove directive from 'm_ImportOrAliasDirectives'.
            // do not report diagnostics.
            Fragmented_VerifyTypeDefinition1(defn);
        }
        else if (phase == VerifyPhase.ImportOrAliasPhase2)
        {
            // report any diagnostics.
            Fragmented_VerifyTypeDefinition2(defn);
        }
    }

    private void Fragmented_VerifyTypeDefinition1(Ast.TypeDefinition defn)
    {
        defn.SemanticRightFrame = m_ModelCore.Factory.Frame();
        Symbol[] typeParameters = null;
        if (defn.Generics != null)
        {
            typeParameters = VerifyTypeParameters(defn.Generics, defn.SemanticRightFrame.Properties);
        }
        EnterFrame(defn.SemanticRightFrame);
        var right = VerifyTypeExp(defn.Type, false, false);
        ExitFrame();

        if (right == null)
        {
            return;
        }

        m_ImportOrAliasDirectives.Remove(defn);

        var previousDefinition = m_Frame.Properties[defn.Id.Name];

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
            m_Frame.Properties[alias.Name] = alias;
        }
    }

    private void Fragmented_VerifyTypeDefinition2(Ast.TypeDefinition defn)
    {
        defn.SemanticRightFrame = m_ModelCore.Factory.Frame();
        Symbol[] typeParameters = null;
        if (defn.Generics != null)
        {
            typeParameters = VerifyTypeParameters(defn.Generics, defn.SemanticRightFrame.Properties);
        }
        EnterFrame(defn.SemanticRightFrame);
        var right = VerifyTypeExp(defn.Type, false);
        ExitFrame();

        if (right == null)
        {
            return;
        }

        var previousDefinition = m_Frame.Properties[defn.Id.Name];

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
            m_Frame.Properties[alias.Name] = alias;
        }
    }
}