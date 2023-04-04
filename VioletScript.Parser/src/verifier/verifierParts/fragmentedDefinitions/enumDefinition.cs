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
    private void Fragmented_VerifyEnumDefinition(Ast.EnumDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            Fragmented_VerifyEnumDefinition1(defn);
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

    private void Fragmented_VerifyEnumDefinition1(Ast.EnumDefinition defn)
    {
        Properties outputProps =
            m_Frame.NamespaceFromFrame != null ? m_Frame.NamespaceFromFrame.Properties
            : m_Frame.PackageFromFrame != null ? m_Frame.PackageFromFrame.Properties
            : m_Frame.Properties;
        Symbol type = null;

        var wrapsType = defn.NumericType != null ? VerifyTypeExp(defn.NumericType) : m_ModelCore.NumberType;
        wrapsType ??= m_ModelCore.NumberType;

        var previousDuplicate = outputProps[defn.Id.Name];
        if (previousDuplicate != null)
        {
            if (m_Options.AllowDuplicates && previousDuplicate is EnumType)
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
            type = m_ModelCore.Factory.EnumType(defn.Id.Name, defn.IsFlags, wrapsType);
            type.ParentDefinition = m_Frame.NamespaceFromFrame ?? m_Frame.PackageFromFrame;
            type.EnumInitializeMethods();
            outputProps[defn.Id.Name] = type;
        }

        defn.SemanticType = type;

        if (type != null)
        {
            defn.SemanticFrame = m_ModelCore.Factory.EnumFrame(type);
            EnterFrame(defn.SemanticFrame);
            foreach (var drtv in defn.Block.Statements)
            {
                if (!drtv.IsEnumVariantDefinition)
                {
                    Fragmented_VerifyStatement(drtv, VerifyPhase.Phase1);
                }
            }
            ExitFrame();
        }
    }
}