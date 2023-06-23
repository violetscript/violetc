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
    // type aliases and namespace aliases are resolved after
    // original item definitions. the verifier gathers
    // a list of imports, type aliases and namespace aliases
    // and tries to resolve them until 10 attempts.
    //
    public void VerifyPrograms(List<Ast.Program> programs)
    {
        var rootFrame = m_ModelCore.Factory.PackageFrame(m_ModelCore.GlobalPackage);
        EnterFrame(rootFrame);

        var packageDefinitions = new List<Ast.PackageDefinition>();

        foreach (var program in programs)
        {
            packageDefinitions.AddRange(program.Packages);
            packageDefinitions.AddRange(this.collectPackageDefinitionsFromDirectives(program.Statements));
        }

        foreach (var packageDefn in packageDefinitions)
        {
            var pckg = m_ModelCore.GlobalPackage.FindOrCreateDeepSubpackage(packageDefn.Id);
            packageDefn.SemanticPackage = pckg;
            packageDefn.SemanticFrame = m_ModelCore.Factory.PackageFrame(pckg);
        }

        var phases = new VerifyPhase[] {
            VerifyPhase.Phase1,
            VerifyPhase.Phase2,
            VerifyPhase.Phase3,
            VerifyPhase.Phase4,
            VerifyPhase.Phase5,
            VerifyPhase.Phase6,
            VerifyPhase.Phase7,
        };

        this.verifyProgramPackages(packageDefinitions, phases);
        this.verifyProgramDirectives(programs, phases);
    }

    // verifies packages from programs.
    // top-level directives are resolved later in a separate method.
    private void verifyProgramPackages(List<Ast.PackageDefinition> packageDefinitions, VerifyPhase[] phases)
    {
        this.m_TypeExpsWithArguments = new List<Ast.TypeExpressionWithArguments>();
        this.m_ImportOrAliasDirectives = new List<Ast.Statement>();

        foreach (var phase in phases)
        {
            foreach (var packageDefn in packageDefinitions)
            {
                // verify package definition
                EnterFrame(packageDefn.SemanticFrame);
                Fragmented_VerifyStatementSeq(packageDefn.Block.Statements, phase);
                ExitFrame();
            }
            // phase 1 = resolve import and alias directives.
            if (phase == VerifyPhase.Phase1)
            {
                this.resolveImportAndAliasUntilExhausted();
            }
        }

        // apply constraints to generic item instantiations in type expressions.
        this.VerifyAllTypeExpsWithArgs();
    }

    // verifies top-level directives from programs.
    // packages are resolved before in a separate method.
    private void verifyProgramDirectives(List<Ast.Program> programs, VerifyPhase[] phases)
    {
        foreach (var program in programs)
        {
            if (program.Statements != null)
            {
                program.SemanticFrame = m_ModelCore.Factory.Frame();
            }
        }

        this.m_TypeExpsWithArguments = new List<Ast.TypeExpressionWithArguments>();
        this.m_ImportOrAliasDirectives = new List<Ast.Statement>();

        foreach (var phase in phases)
        {
            foreach (var program in programs)
            {
                // verify main program's directives if any
                if (program.Statements != null)
                {
                    EnterFrame(program.SemanticFrame);
                    Fragmented_VerifyStatementSeq(program.Statements, phase);
                    ExitFrame();
                }
            }
            // phase 1 = resolve import and alias directives.
            if (phase == VerifyPhase.Phase1)
            {
                this.resolveImportAndAliasUntilExhausted();
            }
        }

        // apply constraints to generic item instantiations in type expressions.
        this.VerifyAllTypeExpsWithArgs();
    }

    // collects package definitions from include directives.
    private List<Ast.PackageDefinition> collectPackageDefinitionsFromDirectives(List<Ast.Statement> list)
    {
        var r = new List<Ast.PackageDefinition>();
        if (list == null)
        {
            return r;
        }
        foreach (var drtv in list)
        {
            if (drtv is Ast.IncludeStatement incDrtv)
            {
                r.AddRange(incDrtv.InnerPackages);
                r.AddRange(collectPackageDefinitionsFromDirectives(incDrtv.InnerStatements));
            }
        }
        return r;
    }

    // attempts to resolve imports and aliases until a limit.
    // this is useful in case one import or alias relies on another.
    private void resolveImportAndAliasUntilExhausted()
    { 
        int i = 0;
        while (m_ImportOrAliasDirectives.Count() != 0 && i != 9)
        {
            foreach (var drtv in m_ImportOrAliasDirectives)
            {
                Fragmented_VerifyStatement(drtv, VerifyPhase.ImportOrAliasPhase1);
            }
            i += 1;
        }
        if (m_ImportOrAliasDirectives.Count() != 0)
        {
            foreach (var drtv in m_ImportOrAliasDirectives)
            {
                Fragmented_VerifyStatement(drtv, VerifyPhase.ImportOrAliasPhase2);
            }
        }
        m_ImportOrAliasDirectives.Clear();
        m_ImportOrAliasDirectives = null;
    }

    private void VerifyAllTypeExpsWithArgs()
    {
        foreach (var gi in m_TypeExpsWithArguments)
        {
            var typeParameters = gi.Base.SemanticSymbol.TypeParameters;
            var arguments = gi.ArgumentsList.Select(te => te.SemanticSymbol).ToArray();

            for (int i = 0; i < arguments.Count(); ++i)
            {
                var argument = arguments[i];
                var argumentExp = gi.ArgumentsList[i];
                foreach (var @param in typeParameters)
                {
                    foreach (var constraintItrfc in @param.ImplementsInterfaces)
                    {
                        // VerifyError: missing interface constraint
                        if (!argument.IsSubtypeOf(constraintItrfc))
                        {
                            VerifyError(null, 136, argumentExp.Span.Value, new DiagnosticArguments { ["t"] = constraintItrfc });
                        }
                    }
                    // VerifyError: missing class constraint
                    if (argument.SuperType != null && !argument.IsSubtypeOf(@param.SuperType))
                    {
                        VerifyError(null, 136, argumentExp.Span.Value, new DiagnosticArguments { ["t"] = @param.SuperType });
                    }
                }
            }
        }
        m_TypeExpsWithArguments.Clear();
        m_TypeExpsWithArguments = null;
    }

    private void Fragmented_VerifyStatementSeq(List<Ast.Statement> seq, VerifyPhase phase)
    {
        foreach (var stmt in seq)
        {
            Fragmented_VerifyStatement(stmt, phase);
        }
    }

    private void Fragmented_VerifyStatement(Ast.Statement stmt, VerifyPhase phase)
    {
        if (stmt is Ast.ImportStatement importDrtv)
        {
            Fragmented_VerifyImportDirective(importDrtv, phase);
        }
        else if (stmt is Ast.UseNamespaceStatement useNsDrtv)
        {
            Fragmented_VerifyUseNamespaceDirective(useNsDrtv, phase);
        }
        else if (stmt is Ast.IncludeStatement incDrtv)
        {
            Fragmented_VerifyStatementSeq(incDrtv.InnerStatements, phase);
        }
        else if (!(stmt is Ast.AnnotatableDefinition))
        {
            if (phase == VerifyPhase.Phase7)
            {
                VerifyStatement(stmt);
            }
        }
        else if (stmt is Ast.VariableDefinition varDefn)
        {
            Fragmented_VerifyVariableDefinition(varDefn, phase);
        }
        else if (stmt is Ast.NamespaceDefinition nsDefn)
        {
            Fragmented_VerifyNamespaceDefinition(nsDefn, phase);
        }
        else if (stmt is Ast.NamespaceAliasDefinition nsaDefn)
        {
            Fragmented_VerifyNamespaceAliasDefinition(nsaDefn, phase);
        }
        else if (stmt is Ast.FunctionDefinition fnDefn)
        {
            Fragmented_VerifyFunctionDefinition(fnDefn, phase);
        }
        else if (stmt is Ast.ConstructorDefinition ctorDefn)
        {
            Fragmented_VerifyConstructorDefinition(ctorDefn, phase);
        }
        else if (stmt is Ast.ProxyDefinition proxyDefn)
        {
            Fragmented_VerifyProxyDefinition(proxyDefn, phase);
        }
        else if (stmt is Ast.GetterDefinition getterDefn)
        {
            Fragmented_VerifyGetterDefinition(getterDefn, phase);
        }
        else if (stmt is Ast.SetterDefinition setterDefn)
        {
            Fragmented_VerifySetterDefinition(setterDefn, phase);
        }
        else if (stmt is Ast.ClassDefinition classDefn)
        {
            Fragmented_VerifyClassDefinition(classDefn, phase);
        }
        else if (stmt is Ast.InterfaceDefinition itrfcDefn)
        {
            Fragmented_VerifyInterfaceDefinition(itrfcDefn, phase);
        }
        else if (stmt is Ast.EnumDefinition enumDefn)
        {
            Fragmented_VerifyEnumDefinition(enumDefn, phase);
        }
        else if (stmt is Ast.TypeDefinition typeDefn)
        {
            Fragmented_VerifyTypeDefinition(typeDefn, phase);
        }
        else
        {
            throw new Exception("Unimplemented");
        }
    }
}

public enum VerifyPhase
{
    /// <summary>
    /// Phase in which original definitions are partially initialized.
    /// Also alias definitions, <c>import</c> directives and
    /// <c>use namespace</c> directives, including <c>type</c> and <c>namespace</c>,
    /// are gathered into a list together with their lexical frames,
    /// are re-arranged into the best order based on how
    /// one directive depends on the other, and then resolved with the phase
    /// <c>ImportOrAliasPhase1</c> or <c>ImportOrAliasPhase2</c>.
    /// </summary>
    Phase1,
    Phase2,
    Phase3,
    Phase4,
    Phase5,
    Phase6,
    Phase7,

    /// <summary>
    /// Phase in which nodes gathered from the phase <c>Phase2</c> are fully verified.
    /// Different from <c>ImportOrAliasPhase2</c>, this phase will not report diagnostics.
    /// </summary>
    ImportOrAliasPhase1,

    /// <summary>
    /// Phase in which nodes gathered from the phase <c>Phase2</c> are fully verified.
    /// Different from <c>ImportOrAliasPhase1</c>, this phase will report diagnostics.
    /// </summary>
    ImportOrAliasPhase2,
}