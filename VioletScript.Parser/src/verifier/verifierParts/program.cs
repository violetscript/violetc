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
    private void VerifyPrograms(List<Ast.Program> programs)
    {
        foreach (var program in programs)
        {
            foreach (var packageDefn in program.Packages)
            {
                var pckg = m_ModelCore.GlobalPackage.FindOrCreateDeepSubpackage(packageDefn.Id);
                packageDefn.SemanticPackage = pckg;
                packageDefn.SemanticFrame = m_ModelCore.Factory.PackageFrame(pckg);
            }
            doFooBarQuxBaz();
        }
    }
}