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
    private void Fragmented_VerifyClassDefinition(Ast.ClassDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            Fragmented_VerifyClassDefinition1(defn);
        }
        else if (phase == VerifyPhase.Phase2)
        {
            Fragmented_VerifyClassDefinition2(defn);
        }
        else if (phase == VerifyPhase.Phase3 || phase == VerifyPhase.Phase4)
        {
            if (defn.SemanticFrame != null)
            {
                EnterFrame(defn.SemanticFrame);
                Fragmented_VerifyStatementSeq(defn.Block.Statements, phase);
                ExitFrame();
            }
        }
        else if (phase == VerifyPhase.Phase5)
        {
            Fragmented_VerifyClassDefinition5(defn);
        }
        else if (defn.SemanticFrame != null)
        {
            EnterFrame(defn.SemanticFrame);
            Fragmented_VerifyStatementSeq(defn.Block.Statements, phase);
            ExitFrame();
        }
    }

    private void Fragmented_VerifyClassDefinition1(Ast.ClassDefinition defn)
    {
        Properties outputProps =
            m_Frame.NamespaceFromFrame != null ? m_Frame.NamespaceFromFrame.Properties
            : m_Frame.PackageFromFrame != null ? m_Frame.PackageFromFrame.Properties
            : m_Frame.Properties;
        Symbol type = null;

        var previousDuplicate = outputProps[defn.Id.Name];
        if (previousDuplicate != null)
        {
            if (m_Options.AllowDuplicates && previousDuplicate is ClassType)
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
            type = m_ModelCore.Factory.ClassType(defn.Id.Name, defn.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Final), defn.IsValue);
            type.DontInit = defn.DontInit;
            type.ParentDefinition = m_Frame.NamespaceFromFrame ?? m_Frame.PackageFromFrame;
            outputProps[defn.Id.Name] = type;
        }

        defn.SemanticType = type;

        if (type != null)
        {
            defn.SemanticFrame = m_ModelCore.Factory.ClassFrame(type);
            if (defn.Generics != null)
            {
                type.TypeParameters = FragmentedA_VerifyTypeParameters(defn.Generics, defn.SemanticFrame.Properties, type);
            }

            EnterFrame(defn.SemanticFrame);
            Fragmented_VerifyStatementSeq(defn.Block.Statements, VerifyPhase.Phase1);
            ExitFrame();
        }
    }

    private void Fragmented_VerifyClassDefinition2(Ast.ClassDefinition defn)
    {
        var type = defn.SemanticType;
        if (type == null)
        {
            return;
        }

        EnterFrame(defn.SemanticFrame);

        if (defn.ExtendsType != null)
        {
            var xt = VerifyTypeExp(defn.ExtendsType);
            if (xt != null)
            {
                if (!xt.IsClassType)
                {
                    VerifyError(null, 232, defn.ExtendsType.Span.Value, new DiagnosticArguments { ["t"] = xt });
                }
                else if (xt.IsFinal)
                {
                    VerifyError(null, 233, defn.ExtendsType.Span.Value, new DiagnosticArguments { ["t"] = xt });
                }
                else if (type == xt || xt.IsSubtypeOf(type))
                {
                    VerifyError(null, 231, defn.Id.Span.Value, new DiagnosticArguments {});
                }
                else
                {
                    type.SuperType = xt;
                    if (xt != m_ModelCore.ObjectType)
                    {
                        xt.AddLimitedKnownSubtype(type);
                    }
                }
            }
        }

        if (defn.ImplementsList != null)
        {
            foreach (var tx in defn.ImplementsList)
            {
                var implT = VerifyTypeExp(tx);
                if (implT == null)
                {
                    continue;
                }
                if (!implT.IsInterfaceType)
                {
                    VerifyError(null, 230, tx.Span.Value, new DiagnosticArguments { ["t"] = implT });
                }
                else if (type.IsSubtypeOf(implT))
                {
                    VerifyError(null, 234, tx.Span.Value, new DiagnosticArguments { ["c"] = type, ["i"] = implT });
                }
                else
                {
                    type.AddImplementedInterface(implT);
                }
            }
        }

        if (defn.Generics != null)
        {
            FragmentedB_VerifyTypeParameters(type.TypeParameters, defn.Generics, defn.SemanticFrame.Properties);
        }

        Fragmented_VerifyStatementSeq(defn.Block.Statements, VerifyPhase.Phase2);
        ExitFrame();
    }

    private void Fragmented_VerifyClassDefinition5(Ast.ClassDefinition defn)
    {
        var type = defn.SemanticType;
        if (type == null)
        {
            return;
        }

        EnterFrame(defn.SemanticFrame);

        // verify interface implementations
        foreach (var itrfc in type.ImplementsInterfaces)
        {
            InterfaceImpl.VerifyImpl(type, itrfc,
                // VerifyError! missing method
                name =>
                {
                    VerifyError(null, 235, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = name, ["itrfc"] = itrfc });
                },
                // VerifyError! missing getter
                name =>
                {
                    VerifyError(null, 236, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = name, ["itrfc"] = itrfc });
                },
                // VerifyError! missing setter
                name =>
                {
                    VerifyError(null, 237, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = name, ["itrfc"] = itrfc });
                },
                // VerifyError! requirement must be method
                name =>
                {
                    VerifyError(null, 238, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = name, ["itrfc"] = itrfc });
                },
                // VerifyError! requirement must be virtual property
                name =>
                {
                    VerifyError(null, 239, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = name, ["itrfc"] = itrfc });
                },
                // VerifyError! wrong method signature
                (name, expectedSignature) =>
                {
                    VerifyError(null, 240, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = name, ["s"] = expectedSignature, ["itrfc"] = itrfc });
                },
                // VerifyError! wrong getter signature
                (name, expectedSignature) =>
                {
                    VerifyError(null, 241, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = name, ["s"] = expectedSignature, ["itrfc"] = itrfc });
                },
                // VerifyError! wrong setter signature
                (name, expectedSignature) =>
                {
                    VerifyError(null, 242, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = name, ["s"] = expectedSignature, ["itrfc"] = itrfc });
                },
                // VerifyError! required generics do not match
                name =>
                {
                    VerifyError(null, 243, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = name, ["itrfc"] = itrfc });
                }
            );
        }

        Fragmented_VerifyStatementSeq(defn.Block.Statements, VerifyPhase.Phase5);
        ExitFrame();
    }
}