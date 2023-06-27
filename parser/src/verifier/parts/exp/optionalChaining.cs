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
    private Symbol VerifyOptChainingExp
    (
        Ast.OptChainingExpression optChaining,
        Symbol expectedType,
        bool instantiatingGeneric
    )
    {
        Ast.Expression exp = optChaining;
        var @base = VerifyExp(optChaining.Base, null, false);
        if (@base == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        if (!(@base is Value))
        {
            // ERROR: optional member must have a value base
            VerifyError(null, 169, optChaining.Base.Span.Value, new DiagnosticArguments {});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var baseType = @base.StaticType;
        if (!baseType.IncludesNull && !baseType.IncludesUndefined)
        {
            // ERROR: base must possibly be undefined or null.
            VerifyError(null, 170, optChaining.Base.Span.Value, new DiagnosticArguments {["t"] = baseType});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        var nonNullBase = m_ModelCore.Factory.Value(baseType.ToNonNullableType());
        optChaining.SemanticNonNullBase = nonNullBase;

        var chainingPlaceholder = optChaining.OptChain.FindOptionalChainingPlaceholder();
        chainingPlaceholder.SemanticSymbol = optChaining.SemanticNonNullBase;
        chainingPlaceholder.SemanticExpResolved = true;

        var r = VerifyExpAsValue(optChaining.OptChain);
        optChaining.SemanticOptNonNullUnifiedResult = r;

        if (r == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        // unify to null and/or undefined
        if (baseType.IncludesNull && !baseType.IncludesUndefined)
        {
            exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.NullType, r.StaticType}));
        }
        else if (!baseType.IncludesNull && baseType.IncludesUndefined)
        {
            exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.UndefinedType, r.StaticType}));
        }
        else
        {
            exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.UndefinedType, m_ModelCore.NullType, r.StaticType}));
        }

        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    }
}