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
    // a list of type aliases and namespace aliases
    // and re-arranges them in a new list for resolving
    // them in the right order.
    //
    // here is an example program where aliases have to be re-arranged
    // as one may rely on another:
    //
    // ```violetscript
    // type TA = TB; // TB is still undefined at this point
    // type TB = Number;

    // namespace F = Q; // Q is still undefined at this point
    // namespace Q = B;
    // ```
    //
    // 'import' and 'use namespace' directives are also re-arranged as neccessary.
    //
    // for namespace aliases, complex constant expressions do not have to be tested
    // as users mostly only use lexical references and member expressions as the
    // right expression.
    //
    public void VerifyPrograms(List<Ast.Program> programs)
    {
        var rootFrame = m_ModelCore.Factory.PackageFrame(m_ModelCore.GlobalPackage);
        EnterFrame(rootFrame);

        foreach (var program in programs)
        {
            foreach (var packageDefn in program.Packages)
            {
                var pckg = m_ModelCore.GlobalPackage.FindOrCreateDeepSubpackage(packageDefn.Id);
                packageDefn.SemanticPackage = pckg;
                packageDefn.SemanticFrame = m_ModelCore.Factory.PackageFrame(pckg);
            }
            if (program.Statements != null)
            {
                program.SemanticFrame = m_ModelCore.Factory.Frame();
            }
        }
        var phases = new VerifyPhase[] {
            VerifyPhase.Phase1,
            VerifyPhase.Phase2,
            VerifyPhase.Phase3,
            VerifyPhase.Phase4,
            VerifyPhase.Phase5,
        };
        m_GenericInstantiationsAsTypeExp = new List<Ast.GenericInstantiationTypeExpression>();
        m_ImportOrAliasDirectives = new List<Ast.Statement>();
        foreach (var phase in phases)
        {
            foreach (var program in programs)
            {
                foreach (var packageDefn in program.Packages)
                {
                    // verify package definition
                    EnterFrame(packageDefn.SemanticFrame);
                    Fragmented_VerifyStatementSeq(packageDefn.Block.Statements, phase);
                    ExitFrame();
                }
                // verify main program's directives if any
                if (program.Statements != null)
                {
                    EnterFrame(program.SemanticFrame);
                    Fragmented_VerifyStatementSeq(program.Statements, phase);
                    ExitFrame();
                }
            }
            // phase 2 = rearrange and resolve various directives.
            // - how to determine where the directive node is from?
            // look at the node's surrounding frame. for example, it can be a PackageFrame.
            if (phase == VerifyPhase.Phase2)
            {
                // re-arrange directives
                doFooBarQuxBaz();

                // resolve directives
                foreach (var drtv in m_ImportOrAliasDirectives)
                {
                    Fragmented_VerifyStatement(drtv, VerifyPhase.ImportOrAliasPhase1);
                }
                m_ImportOrAliasDirectives.Clear();
            }
        }

        // apply constraints to generic item instantiations in type expressions.
        VerifyAllGenericInstAsTypeExps();
    }

    private void VerifyAllGenericInstAsTypeExps()
    {
        foreach (var gi in m_GenericInstantiationsAsTypeExp)
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
        m_GenericInstantiationsAsTypeExp.Clear();
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
            if (phase == VerifyPhase.Phase5)
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
    /// </summary>
    Phase1,
    /// <summary>
    /// Phase in which alias definitions, <c>import</c> directives and
    /// <c>use namespace</c> directives, including <c>type</c> and <c>namespace</c>,
    /// are gathered into a list together with their lexical frames,
    /// are re-arranged into the best order based on how
    /// one directive depends on the other, and then resolved with the phase
    /// <c>ImportOrAliasPhase1</c>.
    /// </summary>
    Phase2,
    Phase3,
    Phase4,
    Phase5,

    /// <summary>
    /// Phase in which nodes gathered from the phase <c>Phase2</c> are fully verified.
    /// </summary>
    ImportOrAliasPhase1,
}