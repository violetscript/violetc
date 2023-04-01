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
            doFooBarQuxBaz();
        }
        else if (phase == VerifyPhase.ImportOrAliasPhase2)
        {
            // report any diagnostics.
            doFooBarQuxBaz();
        }
    }
}