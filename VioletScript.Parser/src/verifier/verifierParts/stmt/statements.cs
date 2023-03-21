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
    } // statement

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
        doFooBarQuxBaz();
    } // super statement
}