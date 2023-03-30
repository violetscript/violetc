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
    // program resolution is fragmented in phases.
    public void VerifyPrograms(List<Ast.Program> programs)
    {
        foreach (var program in programs)
        {
            foreach (var packageDefn in program.Packages)
            {
                var pckg = m_ModelCore.GlobalPackage.FindOrCreateDeepSubpackage(packageDefn.Id);
                packageDefn.SemanticPackage = pckg;
                packageDefn.SemanticFrame = m_ModelCore.Factory.PackageFrame(pckg);
            }
        }
        var phases = new VerifyPhase[] {
            VerifyPhase.Phase1,
            VerifyPhase.Phase2,
            VerifyPhase.Phase3,
            VerifyPhase.Phase4,
            VerifyPhase.Phase5,
        };
        foreach (var phase in phases)
        {
            foreach (var program in programs)
            {
                foreach (var packageDefn in program.Packages)
                {
                    // verify package definition
                    doFooBarQuxBaz();
                }
                // verify program's statements
                doFooBarQuxBaz();
            }
        }
    }
}

public enum VerifyPhase
{
    Phase1,
    Phase2,
    Phase3,
    Phase4,
    Phase5,
}