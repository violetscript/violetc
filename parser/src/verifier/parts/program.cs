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
    // and tries to resolve them until exhausted.
    //
    public void VerifyPrograms(List<Ast.Program> programs)
    {
        var rootFrame = m_ModelCore.Factory.PackageFrame(m_ModelCore.GlobalPackage);
        EnterFrame(rootFrame);

        var packageDefinitions = new List<Ast.PackageDefinition>();

        foreach (var program in programs)
        {
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

            // unused
            // VerifyPhase.Phase6,

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
        this.m_ImportOrAliasDirectivesStack.Push(new List<Ast.Statement>());

        foreach (var phase in phases)
        {
            foreach (var packageDefn in packageDefinitions)
            {
                // verify package definition
                EnterFrame(packageDefn.SemanticFrame);
                this.m_StrictnessFlags.Push(packageDefn.Block.StrictnessFlags);
                Fragmented_VerifyStatementSeq(packageDefn.Block.Statements, phase);
                this.m_StrictnessFlags.Pop();
                ExitFrame();
            }
            // phase 1 = resolve import and alias directives.
            if (phase == VerifyPhase.Phase1)
            {
                this.resolveImportsAndAliasesUntilExhausted();
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
            program.SemanticFrame = m_ModelCore.Factory.Frame();
        }

        this.m_TypeExpsWithArguments = new List<Ast.TypeExpressionWithArguments>();
        this.m_ImportOrAliasDirectivesStack.Push(new List<Ast.Statement>());

        foreach (var phase in phases)
        {
            foreach (var program in programs)
            {
                EnterFrame(program.SemanticFrame);
                this.m_StrictnessFlags.Push(program.StrictnessFlags);
                Fragmented_VerifyStatementSeq(program.Statements, phase);
                this.m_StrictnessFlags.Pop();
                ExitFrame();
            }
            // phase 1 = resolve import and alias directives.
            if (phase == VerifyPhase.Phase1)
            {
                this.resolveImportsAndAliasesUntilExhausted();
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
                r.AddRange(collectPackageDefinitionsFromDirectives(incDrtv.InnerStatements));
            }
            else if (drtv is Ast.PackageDefinition pkgDefn)
            {
                r.Add(pkgDefn);
                r.AddRange(collectPackageDefinitionsFromDirectives(pkgDefn.Block.Statements));
            }
        }
        return r;
    }

    // attempts to resolve imports and aliases until a limit.
    // this is useful in case one import or alias relies on another.
    private void resolveImportsAndAliasesUntilExhausted()
    { 
        int i = 0;
        while (m_ImportOrAliasDirectives.Count() != 0 && i != 9)
        {
            for (int j = 0; j < m_ImportOrAliasDirectives.Count(); ++j)
            {
                var drtv = m_ImportOrAliasDirectives[j];
                Fragmented_VerifyStatement(drtv, VerifyPhase.ImportOrAliasPhase1);
            }
            i += 1;
        }
        if (m_ImportOrAliasDirectives.Count() != 0)
        {
            for (int j = 0; j < m_ImportOrAliasDirectives.Count(); ++j)
            {
                var drtv = m_ImportOrAliasDirectives[j];
                Fragmented_VerifyStatement(drtv, VerifyPhase.ImportOrAliasPhase2);
            }
        }
        m_ImportOrAliasDirectives.Clear();
        m_ImportOrAliasDirectivesStack.Pop();
    }

    private void Fragmented_VerifyStatementSeq(List<Ast.Statement> seq, VerifyPhase phase)
    {
        int nOfVarShadows = 0;
        foreach (var stmt in seq)
        {
            Fragmented_VerifyStatement(stmt, phase);
            if (stmt is Ast.VariableDefinition varDefn && varDefn.SemanticShadowFrame != null)
            {
                EnterFrame(varDefn.SemanticShadowFrame);
                ++nOfVarShadows;
            }
        }
        ExitNFrames(nOfVarShadows);
    } // statement sequence

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
            this.m_StrictnessFlags.Push(incDrtv.StrictnessFlags);
            Fragmented_VerifyStatementSeq(incDrtv.InnerStatements, phase);
            this.m_StrictnessFlags.Pop();
        }
        else if (!(stmt is Ast.AnnotatableDefinition))
        {
            // verify statement in last phase
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
        else if (stmt is Ast.PackageDefinition)
        {
            // ignore package in this context
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

    /// <summary>
    /// Future reserved phase. Unused. When necessary,
    /// include it in the sequence of phases, otherwise
    /// it won't be iterated.
    /// </summary>
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