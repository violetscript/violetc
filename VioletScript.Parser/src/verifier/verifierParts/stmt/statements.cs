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
    private void VerifyStatement(Ast.Statement stmt)
    {
        // expression statement
        if (stmt is Ast.ExpressionStatement expStmt)
        {
            VerifyExp(expStmt.Expression);
        }
        // block statement
        else if (stmt is Ast.Block block)
        {
            VerifyBlock(block);
        }
        // variable definition
        else if (stmt is Ast.VariableDefinition varDefn)
        {
            VerifyVariableDefinition(varDefn);
        }
        // super statement
        else if (stmt is Ast.SuperStatement supStmt)
        {
            VerifySuperStatement(supStmt);
        }
        // import directive
        else if (stmt is Ast.ImportStatement importStmt)
        {
            VerifyImportDirective(importStmt);
        }
        // empty statement
        else if (stmt is Ast.EmptyStatement)
        {
        }
        else
        {
            throw new Exception("Unimplemented directive or statement");
        }
    } // statement

    private void VerifyStatementSeq(List<Ast.Statement> seq)
    {
        int nOfVarShadows = 0;
        foreach (var stmt in seq)
        {
            VerifyStatement(stmt);
            if (stmt is Ast.VariableDefinition varDefn && varDefn.SemanticShadowFrame != null)
            {
                EnterFrame(varDefn.SemanticShadowFrame);
                ++nOfVarShadows;
            }
        }
        ExitNFrames(nOfVarShadows);
    } // statement sequence

    private void VerifyBlock(Ast.Block block)
    {
        VerifyStatementSeq(block.Statements);
    } // block statement

    private void VerifyVariableDefinition(Ast.VariableDefinition defn)
    {
        // create shadow frame
        var shadowFrame = m_ModelCore.Factory.Frame();
        defn.SemanticShadowFrame = shadowFrame;
        foreach (var binding in defn.Bindings)
        {
            VerifyVariableBinding(binding, defn.ReadOnly, shadowFrame.Properties, Visibility.Public);
        }
    } // variable definition

    private void VerifySuperStatement(Ast.SuperStatement stmt)
    {
        var constructorDefinition = m_Frame.FindClassFrame();
        VerifyFunctionCall(stmt.ArgumentsList, stmt.Span.Value, constructorDefinition.StaticType);
    } // super statement

    private void VerifyImportDirective(Ast.ImportStatement stmt)
    {
        var imported = m_ModelCore.GlobalPackage;
        foreach (var name in stmt.ImportName)
        {
            var imported2 = imported.ResolveProperty(name);
            if (imported2 == null)
            {
                if (imported is Package)
                {
                    VerifyError(null, 214, stmt.Span.Value, new DiagnosticArguments {["p"] = imported, ["name"] = name});
                }
                else
                {
                    VerifyError(null, 128, stmt.Span.Value, new DiagnosticArguments {["name"] = name});
                }
                return;
            }
            imported = imported2;
        }
        if (stmt.Wildcard && !(imported is Package))
        {
            doFooBarQuxBaz();
        }
        else if (!stmt.Wildcard && imported is Package)
        {
            doFooBarQuxBaz();
        }
        doFooBarQuxBaz();
    } // import statatement
}