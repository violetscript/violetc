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
            Fragmented_VerifyInterfaceDefinition2(defn);
        }
        else if (defn.SemanticFrame != null)
        {
            EnterFrame(defn.SemanticFrame);
            Fragmented_VerifyStatementSeq(defn.Block.Statements, phase);
            ExitFrame();
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
            type.TypeParameters = FragmentedA_VerifyTypeParameters(defn.Generics, defn.SemanticFrame.Properties, type);

            EnterFrame(defn.SemanticFrame);
            Fragmented_VerifyStatementSeq(defn.Block.Statements, VerifyPhase.Phase1);
            ExitFrame();
        }
    }

    // - verifies 'extends' clause if any.
    //   - add the interface itself as a limited known subtype of each
    //     extended type.
    // - finish verifying generics if any by calling `FragmentedB_VerifyTypeParameters`.
    // - visit block.
    private void Fragmented_VerifyInterfaceDefinition2(Ast.InterfaceDefinition defn)
    {
        var type = defn.SemanticType;
        if (type == null)
        {
            return;
        }

        EnterFrame(defn.SemanticFrame);

        if (defn.ExtendsList != null)
        {
            foreach (var tx in defn.ExtendsList)
            {
                var type2 = VerifyTypeExp(tx);
                if (type2 == null)
                {
                    continue;
                }
                if (!type2.IsInterfaceType)
                {
                    VerifyError(null, 230, tx.Span.Value, new DiagnosticArguments { ["t"] = type2 });
                    continue;
                }
                if (type == type2 || type2.IsSubtypeOf(type))
                {
                    VerifyError(null, 231, defn.Id.Span.Value, new DiagnosticArguments {});
                    continue;
                }
                type.AddExtendedInterface(type2);
                type2.AddLimitedKnownSubtype(type);
            }
        }

        if (defn.Generics != null)
        {
            FragmentedB_VerifyTypeParameters(type.TypeParameters, defn.Generics, defn.SemanticFrame.Properties);
        }

        Fragmented_VerifyStatementSeq(defn.Block.Statements, VerifyPhase.Phase2);
        ExitFrame();
    }
}