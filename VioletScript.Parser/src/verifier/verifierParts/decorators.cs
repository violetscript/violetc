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
    private void VerifyTypeDefinitionDecorators(Ast.AnnotatableDefinition defn)
    {
        if (defn.Decorators == null)
        {
            return;
        }
        foreach (var d in defn.Decorators)
        {
            VerifyTypeAttachedDecorator(d);
        }
    }

    private void VerifyTypeAttachedDecorator(Ast.Expression decorator)
    {
        LimitExpType(decorator, m_ModelCore.TypeAttachedDecoratorType);
    }
}