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
    private void Fragmented_VerifyImportDirective(Ast.ImportStatement drtv, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            drtv.SemanticSurroundingFrame = m_Frame;
            m_ImportOrAliasDirectives.Add(drtv);
        }
        else if (phase == VerifyPhase.ImportOrAliasPhase1)
        {
            // if successful, remove directive from 'm_ImportOrAliasDirectives'.
            // do not report diagnostics.
            var previousFrame = m_Frame;
            m_Frame = drtv.SemanticSurroundingFrame;
            Fragmented_VerifyImportDirective1Or2(drtv, false);
            m_Frame = previousFrame;
        }
        else if (phase == VerifyPhase.ImportOrAliasPhase2)
        {
            // report any diagnostics.
            var previousFrame = m_Frame;
            m_Frame = drtv.SemanticSurroundingFrame;
            Fragmented_VerifyImportDirective1Or2(drtv, true);
            m_Frame = previousFrame;
        }
    }

    private void Fragmented_VerifyImportDirective1Or2(Ast.ImportStatement stmt, bool lastAttempt)
    {
        var imported = m_ModelCore.GlobalPackage;
        bool first = true;
        foreach (var name in stmt.ImportName)
        {
            // the 'global' identifier may be used to alias a property
            // from the global package.
            if (first && name == "global")
            {
                first = false;
                continue;
            }
            if (!(imported is Package))
            {
                if (lastAttempt)
                {
                    VerifyError(null, 215, stmt.Span.Value, new DiagnosticArguments {});
                }
                return;
            }
            var imported2 = imported.ResolveProperty(name);
            if (imported2 == null)
            {
                if (lastAttempt)
                {
                    if (imported is Package)
                    {
                        VerifyError(null, 214, stmt.Span.Value, new DiagnosticArguments {["p"] = imported, ["name"] = name});
                    }
                    else
                    {
                        VerifyError(null, 128, stmt.Span.Value, new DiagnosticArguments {["name"] = name});
                    }
                }
                return;
            }
            imported = imported2;
            first = false;
        }
        if (stmt.Wildcard && !(imported is Package))
        {
            if (lastAttempt)
            {
                VerifyError(null, 216, stmt.Span.Value, new DiagnosticArguments {});
            }
            return;
        }
        else if (!stmt.Wildcard && imported is Package)
        {
            if (lastAttempt)
            {
                VerifyError(null, 217, stmt.Span.Value, new DiagnosticArguments {});
            }
            return;
        }

        if (stmt.Alias == null && stmt.Wildcard)
        {
            m_Frame.OpenNamespace(imported);
            stmt.SemanticImportee = imported;
            m_ImportOrAliasDirectives.Remove(stmt);
        }
        else if (stmt.Alias != null)
        {
            // alias item or package
            if (m_Frame.Properties.Has(stmt.Alias.Name))
            {
                if (lastAttempt)
                {
                    VerifyError(null, 139, stmt.Alias.Span.Value, new DiagnosticArguments {["name"] = stmt.Alias.Name});
                }
            }
            else
            {
                m_Frame.Properties[stmt.Alias.Name] = imported;
                stmt.SemanticImportee = imported;
                m_ImportOrAliasDirectives.Remove(stmt);
            }
        }
        else
        {
            // alias item
            if (m_Frame.Properties.Has(imported.Name))
            {
                if (lastAttempt)
                {
                    VerifyError(null, 139, stmt.Span.Value, new DiagnosticArguments {["name"] = imported.Name});
                }
            }
            else
            {
                m_Frame.Properties[imported.Name] = imported;
                stmt.SemanticImportee = imported;
                m_ImportOrAliasDirectives.Remove(stmt);
            }
        }
    }
}