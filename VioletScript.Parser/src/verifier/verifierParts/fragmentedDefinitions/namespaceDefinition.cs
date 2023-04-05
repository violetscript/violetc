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
    private void Fragmented_VerifyNamespaceDefinition(Ast.NamespaceDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            Fragmented_VerifyNamespaceDefinition1(defn);
        }
        else if (defn.SemanticFrame != null)
        {
            EnterFrame(defn.SemanticFrame);
            Fragmented_VerifyStatementSeq(defn.Block.Statements, phase);
            ExitFrame();
        }
    }

    private void Fragmented_VerifyNamespaceDefinition1(Ast.NamespaceDefinition defn)
    {
        Properties outputProps =
            m_Frame.NamespaceFromFrame != null ? m_Frame.NamespaceFromFrame.Properties
            : m_Frame.PackageFromFrame != null ? m_Frame.PackageFromFrame.Properties
            : m_Frame.Properties;
        Symbol ns = null;

        var previousDuplicate = outputProps[defn.Id.Name];
        if (previousDuplicate != null)
        {
            if (m_Options.AllowDuplicates && previousDuplicate is Namespace)
            {
                ns = previousDuplicate;
            }
            else
            {
                VerifyError(null, 139, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = defn.Id.Name });
            }
        }
        else
        {
            ns = m_ModelCore.Factory.Namespace(defn.Id.Name);
            ns.ParentDefinition = m_Frame.NamespaceFromFrame ?? m_Frame.PackageFromFrame;
            outputProps[defn.Id.Name] = ns;
        }

        defn.SemanticNamespace = ns;

        if (ns != null)
        {
            defn.SemanticFrame = m_ModelCore.Factory.NamespaceFrame(ns);

            EnterFrame(defn.SemanticFrame);
            Fragmented_VerifyStatementSeq(defn.Block.Statements, VerifyPhase.Phase1);
            ExitFrame();
        }
    }
}