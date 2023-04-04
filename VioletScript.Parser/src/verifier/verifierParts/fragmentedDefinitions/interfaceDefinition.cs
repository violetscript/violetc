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
    private void Fragmented_VerifyInterfaceDefinition(Ast.InterfaceDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            Fragmented_VerifyInterfaceDefinition1(defn);
        }
        else if (phase == VerifyPhase.Phase2)
        {
            doFooBarQuxBaz();
        }
        else if (phase == VerifyPhase.Phase3)
        {
            doFooBarQuxBaz();
        }
        else if (phase == VerifyPhase.Phase4)
        {
            doFooBarQuxBaz();
        }
        else if (phase == VerifyPhase.Phase5)
        {
            doFooBarQuxBaz();
        }
        else if (phase == VerifyPhase.Phase6)
        {
            doFooBarQuxBaz();
        }
        // VerifyPhase.Phase7
        else
        {
            doFooBarQuxBaz();
        }
    }

    private void Fragmented_VerifyInterfaceDefinition1(Ast.InterfaceDefinition defn)
    {
        Properties outputProps =
            m_Frame.NamespaceFromFrame != null ? m_Frame.NamespaceFromFrame.Properties
            : m_Frame.PackageFromFrame != null ? m_Frame.PackageFromFrame.Properties
            : m_Frame.Properties;
        Symbol type = null;

        var previousDuplicate = outputProps[defn.Id.Name];
        if (previousDuplicate != null)
        {
            if (m_Options.AllowDuplicates && previousDuplicate is InterfaceType)
            {
                type = previousDuplicate;
            }
            else
            {
                VerifyError(null, 139, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = defn.Id.Name });
            }
        }
        else
        {
            type = m_ModelCore.Factory.InterfaceType(defn.Id.Name);
            type.ParentDefinition = m_Frame.NamespaceFromFrame ?? m_Frame.PackageFromFrame;
            outputProps[defn.Id.Name] = type;
        }

        defn.SemanticType = type;

        if (type != null)
        {
            defn.SemanticFrame = m_ModelCore.Factory.InterfaceFrame(type);

            // type parameters (if duplicate interface, re-use them)
            doFooBarQuxBaz();

            EnterFrame(defn.SemanticFrame);
            Fragmented_VerifyStatementSeq(defn.Block.Statements, VerifyPhase.Phase1);
            ExitFrame();
        }
    }
}