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
        // empty statement
        else if (stmt is Ast.EmptyStatement)
        {
        }
        else
        {
            throw new Exception("Unimplemented directive or statement");
        }
    } // statement

    private void VerifyBlock(Ast.Block block)
    {
        int nOfVarShadows = 0;
        foreach (var stmt in block.Statements)
        {
            VerifyStatement(stmt);
            if (stmt is Ast.VariableDefinition varDefn && varDefn.SemanticShadowFrame != null)
            {
                EnterFrame(varDefn.SemanticShadowFrame);
                ++nOfVarShadows;
            }
        }
        ExitNFrames(nOfVarShadows);
    } // block statement

    private void VerifyVariableDefinition(Ast.VariableDefinition defn)
    {
        // create shadow frame
        doFooBarQuxBaz();
    }
    // variable definition
}