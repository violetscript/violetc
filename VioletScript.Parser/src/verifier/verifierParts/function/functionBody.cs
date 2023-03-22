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
    // - verify return code paths.
    // - verify return type.
    private void VerifyFunctionBody(Ast.Node body, Symbol methodSlot)
    {
        var returnType = methodSlot.StaticType.FunctionReturnType;
        if (methodSlot.UsesAwait)
        {
            returnType = methodSlot.FunctionReturnType.ArgumentTypes[0];
        }
        else if (methodSlot.UsesYield)
        {
            returnType = m_ModelCore.UndefinedType;
        }
        if (body is Ast.Expression exprBody)
        {
            LimitExpType(exprBody, returnType);
        }
        else if (body is Ast.Statement stmtBody && returnType != m_ModelCore.UndefinedType && !stmtBody.AllCodePathsReturn)
        {
            // VerifyError: not all code paths return
            doFooBarQuxBaz();
        }
    }
}