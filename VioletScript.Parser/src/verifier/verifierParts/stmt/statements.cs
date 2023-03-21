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
        // if statement
        else if (stmt is Ast.IfStatement ifStmt)
        {
            VerifyIfStatement(ifStmt);
        }
        // do statement
        else if (stmt is Ast.DoStatement doStmt)
        {
            VerifyDoStatement(doStmt);
        }
        // while statement
        else if (stmt is Ast.WhileStatement whileStmt)
        {
            VerifyWhileStatement(whileStmt);
        }
        // break statement
        else if (stmt is Ast.BreakStatement breakStmt)
        {
        }
        // continue statement
        else if (stmt is Ast.ContinueStatement contStmt)
        {
        }
        // empty statement
        else if (stmt is Ast.EmptyStatement)
        {
        }
        // return statement
        else if (stmt is Ast.ReturnStatement ret)
        {
            VerifyReturnStatement(ret);
        }
        // throw statement
        else if (stmt is Ast.ThrowStatement throwStmt)
        {
            VerifyExpAsValue(throwStmt.Expression);
        }
        // try statement
        else if (stmt is Ast.TryStatement tryStmt)
        {
            VerifyTryStatement(tryStmt);
        }
        // labeled statement
        else if (stmt is Ast.LabeledStatement labeled)
        {
            VerifyStatement(labeled.Statement);
        }
        // for statement
        else if (stmt is Ast.ForStatement forStmt)
        {
            VerifyForStatement(forStmt);
        }
        // for in statement
        else if (stmt is Ast.ForInStatement forInStmt)
        {
            VerifyForInStatement(forInStmt);
        }
        // switch statement
        else if (stmt is Ast.SwitchStatement switchStmt)
        {
            VerifySwitchStatement(switchStmt);
        }
        // switch statement
        else if (stmt is Ast.SwitchTypeStatement switchTStmt)
        {
            VerifySwitchTypeStatement(switchTStmt);
        }
        // include directive
        else if (stmt is Ast.IncludeStatement incStmt)
        {
            VerifyStatementSeq(incStmt.InnerStatements);
        }
        // use namespace directive
        else if (stmt is Ast.UseNamespaceStatement usens)
        {
            VerifyUseNamespaceDirective(usens);
        }
        // use resource statement
        else if (stmt is Ast.UseResourceStatement useres)
        {
            VerifyUseResourceStatement(useres);
        }
        // with statement
        else if (stmt is Ast.WithStatement withStmt)
        {
            VerifyWithStatement(withStmt);
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
                VerifyError(null, 215, stmt.Span.Value, new DiagnosticArguments {});
                return;
            }
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
            first = false;
        }
        if (stmt.Wildcard && !(imported is Package))
        {
            VerifyError(null, 216, stmt.Span.Value, new DiagnosticArguments {});
            return;
        }
        else if (!stmt.Wildcard && imported is Package)
        {
            VerifyError(null, 217, stmt.Span.Value, new DiagnosticArguments {});
            return;
        }

        if (stmt.Alias == null && stmt.Wildcard)
        {
            m_Frame.OpenNamespace(imported);
        }
        else if (stmt.Alias != null)
        {
            // alias item or package
            if (m_Frame.Properties.Has(stmt.Alias.Name))
            {
                VerifyError(null, 139, stmt.Alias.Span.Value, new DiagnosticArguments {["name"] = stmt.Alias.Name});
            }
            else
            {
                m_Frame.Properties[stmt.Alias.Name] = imported;
            }
        }
        else
        {
            // alias item
            if (m_Frame.Properties.Has(imported.Name))
            {
                VerifyError(null, 139, stmt.Span.Value, new DiagnosticArguments {["name"] = imported.Name});
            }
            else
            {
                m_Frame.Properties[imported.Name] = imported;
            }
        }
    } // import statement

    private void VerifyIfStatement(Ast.IfStatement stmt)
    {
        VerifyExpAsValue(stmt.Test);
        VerifyStatement(stmt.Consequent);
        if (stmt.Alternative != null)
        {
            VerifyStatement(stmt.Alternative);
        }
    } // if statement

    private void VerifyDoStatement(Ast.DoStatement stmt)
    {
        VerifyStatement(stmt.Body);
        VerifyExpAsValue(stmt.Test);
    } // do statement

    private void VerifyWhileStatement(Ast.WhileStatement stmt)
    {
        VerifyExpAsValue(stmt.Test);
        VerifyStatement(stmt.Body);
    } // while statement

    private void VerifyReturnStatement(Ast.ReturnStatement stmt)
    {
        var method = CurrentMethodSlot;
        if (method == null)
        {
            if (stmt.Expression != null)
            {
                VerifyExpAsValue(stmt.Expression);
            }
            return;
        }
        if (method.UsesYield || method.StaticType.FunctionReturnType == m_ModelCore.UndefinedType)
        {
            if (stmt.Expression != null)
            {
                LimitExpType(stmt.Expression, m_ModelCore.UndefinedType);
            }
            return;
        }
        if (method.UsesAwait)
        {
            var resolvedType = method.StaticType.FunctionReturnType.ArgumentTypes[0];
            if (stmt.Expression != null)
            {
                LimitExpType(stmt.Expression, resolvedType);
            }
            else if (resolvedType != m_ModelCore.UndefinedType)
            {
                // VerifyError: return must not be empty
                VerifyError(null, 218, stmt.Span.Value, new DiagnosticArguments {});
            }
            return;
        }

        var returnType = method.StaticType.FunctionReturnType;
        if (stmt.Expression != null)
        {
            LimitExpType(stmt.Expression, returnType);
        }
        else if (returnType != m_ModelCore.UndefinedType)
        {
            // VerifyError: return must not be empty
            VerifyError(null, 218, stmt.Span.Value, new DiagnosticArguments {});
        }
    } // return statement

    private void VerifyTryStatement(Ast.TryStatement stmt)
    {
        VerifyStatement(stmt.Block);

        // verify catch clauses
        foreach (var catchClause in stmt.CatchClauses)
        {
            catchClause.SemanticFrame = m_ModelCore.Factory.Frame();
            VerifyDestructuringPattern(catchClause.Pattern, false, catchClause.SemanticFrame.Properties, Visibility.Public, null);
            EnterFrame(catchClause.SemanticFrame);
            VerifyStatement(catchClause.Block);
            ExitFrame();
        }

        if (stmt.FinallyBlock != null)
        {
            VerifyStatement(stmt.FinallyBlock);
        }
    } // try statement

    private void VerifyForStatement(Ast.ForStatement stmt)
    {
        stmt.SemanticFrame = m_ModelCore.Factory.Frame();
        EnterFrame(stmt.SemanticFrame);
        if (stmt.Init is Ast.SimpleVariableDeclaration)
        {
            VerifySimpleVariableDeclaration((Ast.SimpleVariableDeclaration) stmt.Init);
        }
        else if (stmt.Init != null)
        {
            VerifyExpAsValue((Ast.Expression) stmt.Init);
        }
        if (stmt.Test != null)
        {
            VerifyExpAsValue(stmt.Test);
        }
        if (stmt.Update != null)
        {
            VerifyExpAsValue(stmt.Update);
        }
        VerifyStatement(stmt.Body);
        ExitFrame();
    } // for statement

    private void VerifyForInStatement(Ast.ForInStatement stmt)
    {
        if (!stmt.IteratesKeys)
        {
            VerifyForEachInStatement(stmt);
            return;
        }
        var right = VerifyExpAsValue(stmt.Right);
        if (right == null)
        {
            if (stmt.Left is Ast.Expression)
            {
                VerifyExpAsValue((Ast.Expression) stmt.Left, null, false, true);
            }
            VerifyStatement(stmt.Body);
            return;
        }
        var proxy = InheritedProxies.Find(right.StaticType, Operator.ProxyToIterateKeys);
        if (proxy == null)
        {
            // VerifyError: cannot iterate type
            VerifyError(null, 219, stmt.Right.Span.Value, new DiagnosticArguments {["t"] = right.StaticType});
            if (stmt.Left is Ast.Expression)
            {
                VerifyExpAsValue((Ast.Expression) stmt.Left, null, false, true);
            }
            VerifyStatement(stmt.Body);
            return;
        }
        var itemType = proxy.StaticType.FunctionReturnType.ArgumentTypes[0];
        stmt.SemanticFrame = m_ModelCore.Factory.Frame();
        EnterFrame(stmt.SemanticFrame);
        if (stmt.Left is Ast.SimpleVariableDeclaration)
        {
            VerifySimpleVariableDeclaration((Ast.SimpleVariableDeclaration) stmt.Left, itemType);
            var pattern = ((Ast.SimpleVariableDeclaration) stmt.Left).Bindings[0].Pattern;
            if (pattern.SemanticProperty.StaticType != itemType)
            {
                VerifyError
                (
                    null, 168, stmt.Left.Span.Value,
                    new DiagnosticArguments
                    {
                        ["expected"] = itemType, ["got"] = pattern.SemanticProperty.StaticType
                    }
                );
            }
        }
        else
        {
            LimitStrictExpType((Ast.Expression) stmt.Left, itemType, true);
        }
        VerifyStatement(stmt.Body);
        ExitFrame();
    } // for in statement

    private void VerifyForEachInStatement(Ast.ForInStatement stmt)
    {
        var right = VerifyExpAsValue(stmt.Right);
        if (right == null)
        {
            if (stmt.Left is Ast.Expression)
            {
                VerifyExpAsValue((Ast.Expression) stmt.Left, null, false, true);
            }
            VerifyStatement(stmt.Body);
            return;
        }
        var iteratorItemType = right.StaticType.GetIteratorItemType();
        Symbol proxy = iteratorItemType == null
            ? InheritedProxies.Find(right.StaticType, Operator.ProxyToIterateValues)
            : null;
        if (iteratorItemType == null && proxy == null)
        {
            // VerifyError: cannot iterate type
            VerifyError(null, 220, stmt.Right.Span.Value, new DiagnosticArguments {["t"] = right.StaticType});
            if (stmt.Left is Ast.Expression)
            {
                VerifyExpAsValue((Ast.Expression) stmt.Left, null, false, true);
            }
            VerifyStatement(stmt.Body);
            return;
        }
        var itemType = iteratorItemType == null
            ? proxy.StaticType.FunctionReturnType.ArgumentTypes[0]
            : iteratorItemType;

        stmt.SemanticFrame = m_ModelCore.Factory.Frame();
        EnterFrame(stmt.SemanticFrame);
        if (stmt.Left is Ast.SimpleVariableDeclaration)
        {
            VerifySimpleVariableDeclaration((Ast.SimpleVariableDeclaration) stmt.Left, itemType);
            var pattern = ((Ast.SimpleVariableDeclaration) stmt.Left).Bindings[0].Pattern;
            if (pattern.SemanticProperty.StaticType != itemType)
            {
                VerifyError
                (
                    null, 168, stmt.Left.Span.Value,
                    new DiagnosticArguments
                    {
                        ["expected"] = itemType, ["got"] = pattern.SemanticProperty.StaticType
                    }
                );
            }
        }
        else
        {
            LimitStrictExpType((Ast.Expression) stmt.Left, itemType, true);
        }
        VerifyStatement(stmt.Body);
        ExitFrame();
    } // for each statement

    private void VerifySwitchStatement(Ast.SwitchStatement stmt)
    {
        var discriminant = VerifyExpAsValue(stmt.Discriminant);
        foreach (var swCase in stmt.Cases)
        {
            if (swCase.Test != null)
            {
                LimitExpType(swCase.Test, discriminant?.StaticType ?? m_ModelCore.AnyType);
            }
            VerifyStatementSeq(swCase.Consequent);
        }
    } // switch statement

    private void VerifySwitchTypeStatement(Ast.SwitchTypeStatement stmt)
    {
        var discriminant = VerifyExpAsValue(stmt.Discriminant);
        foreach (var swCase in stmt.Cases)
        {
            swCase.SemanticFrame = m_ModelCore.Factory.Frame();
            EnterFrame(swCase.SemanticFrame);
            if (swCase.Pattern != null)
            {
                VerifyDestructuringPattern(swCase.Pattern, false, m_Frame.Properties, Visibility.Public, null);
                var gotType = swCase.Pattern.SemanticProperty.StaticType;
                if (discriminant != null && !gotType.CanBeASubtypeOf(discriminant.StaticType))
                {
                    Warn(null, 221, swCase.Pattern.Span.Value, new DiagnosticArguments {["t"] = gotType});
                }
            }
            VerifyStatement(swCase.Block);
            ExitFrame();
        }
    } // switch type statement

    private void VerifyUseNamespaceDirective(Ast.UseNamespaceStatement drtv)
    {
        var ns = VerifyConstantExp(drtv.Expression, true);
        if (ns == null)
        {
            return;
        }
        if (!(ns is Namespace))
        {
            VerifyError(null, 222, drtv.Expression.Span.Value, new DiagnosticArguments {});
            return;
        }
        m_Frame.OpenNamespace(ns);
    } // use namespace directive

    private void VerifyUseResourceStatement(Ast.UseResourceStatement stmt)
    {
        stmt.SemanticFrame = m_ModelCore.Factory.Frame();
        foreach (var binding in stmt.Bindings)
        {
            VerifyVariableBinding(binding, true, stmt.SemanticFrame.Properties, Visibility.Public);
        }
        EnterFrame(stmt.SemanticFrame);
        VerifyStatement(stmt.Block);
        ExitFrame();
    } // use resource statement

    private void VerifyWithStatement(Ast.WithStatement stmt)
    {
        var obj = VerifyExpAsValue(stmt.Object);
        if (obj == null)
        {
            VerifyStatement(stmt.Body);
            return;
        }
        stmt.SemanticFrame = m_ModelCore.Factory.WithFrame(obj);
        EnterFrame(stmt.SemanticFrame);
        VerifyStatement(stmt.Body);
        ExitFrame();
    } // with statement
}