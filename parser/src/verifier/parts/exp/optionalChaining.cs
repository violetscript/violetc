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
    // verifies member; ensure
    // - it is not undefined.
    // - it is lexically visible.
    // - if it is a non-argumented generic type or function, throw a VerifyError.
    // - it is a compile-time value.
    private Symbol VerifyOptMemberExp
    (
        Ast.OptMemberExpression memb,
        Symbol expectedType,
        bool instantiatingGeneric
    )
    {
        Ast.Expression exp = memb;
        var @base = VerifyExp(memb.Base, null, false);
        if (@base == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        if (!(@base is Value))
        {
            // VerifyError: optional member must have a value base
            VerifyError(null, 169, memb.Base.Span.Value, new DiagnosticArguments {});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var baseType = @base.StaticType;
        if (!baseType.IncludesNull && !baseType.IncludesUndefined)
        {
            // VerifyError: optional member base must possibly be undefined
            // or null.
            VerifyError(null, 170, memb.Base.Span.Value, new DiagnosticArguments {["t"] = baseType});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        var nonNullBase = m_ModelCore.Factory.Value(baseType.ToNonNullableType());
        memb.SemanticNonNullBase = nonNullBase;

        var r = nonNullBase.ResolveProperty(memb.Id.Name);
        memb.SemanticOptNonNullUnifiedSymbol = r;

        var chainingPlaceholder = memb.OptChain.FindOptionalChainingPlaceholder();
        chainingPlaceholder.SemanticSymbol = memb.SemanticNonNullBase;
        chainingPlaceholder.SemanticExpResolved = true;
        toDo();

        if (r == null)
        {
            // VerifyError: undefined reference
            ReportNameNotFound(memb.Id.Name, memb.Id.Span.Value, nonNullBase);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        else
        {
            if (!r.PropertyIsVisibleTo(m_Frame))
            {
                // VerifyError: accessing private property
                VerifyError(null, 130, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
            r = r?.EscapeAlias();
            // VerifyError: unargumented generic type or function
            if (!instantiatingGeneric && r.IsGenericTypeOrMethod)
            {
                VerifyError(null, 132, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }

            // extend variable life
            if (r is ReferenceValueFromFrame && r.Base.FindActivation() != m_Frame.FindActivation())
            {
                r.Base.FindActivation()?.AddExtendedLifeVariable(r.Property);
            }

            exp.SemanticExpResolved = true;

            if (r is Value && r.StaticType == null)
            {
                VerifyError(null, 199, memb.Id.Span.Value, new DiagnosticArguments {});
                exp.SemanticSymbol = null;
            }
            else if (baseType.IncludesNull && !baseType.IncludesUndefined)
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

            return exp.SemanticSymbol;
        }
    } // member expression (?.)

    private Symbol VerifyOptIndexExp(Ast.OptIndexExpression exp)
    {
        var @base = VerifyExpAsValue(exp.Base, null);
        if (@base == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        var baseType = @base.StaticType;
        if (!baseType.IncludesNull && !baseType.IncludesUndefined)
        {
            // VerifyError: optional member base must possibly be undefined
            // or null.
            VerifyError(null, 170, exp.Base.Span.Value, new DiagnosticArguments {["t"] = baseType});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        var nonNullBase = m_ModelCore.Factory.Value(baseType.ToNonNullableType());
        exp.SemanticNonNullBase = nonNullBase;

        toDo();

        // optional tuple access
        if (nonNullBase.StaticType is TupleType)
        {
            var tupleType = nonNullBase.StaticType;
            if (!(exp.Key is Ast.NumericLiteral))
            {
                VerifyError(null, 247, exp.Span.Value, new DiagnosticArguments {});
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
            var idx = (int) ((Ast.NumericLiteral) exp.Key).Value;
            if (idx < 0 || idx >= tupleType.CountOfTupleElements)
            {
                VerifyError(null, 248, exp.Span.Value, new DiagnosticArguments {["type"] = tupleType});
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }

            exp.SemanticSymbol = this.m_ModelCore.Factory.TupleElementValue(@base, idx, tupleType);
            if (baseType.IncludesNull && !baseType.IncludesUndefined)
            {
                exp.SemanticSymbol.StaticType = m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.NullType, exp.SemanticSymbol.StaticType});
            }
            else if (!baseType.IncludesNull && baseType.IncludesUndefined)
            {
                exp.SemanticSymbol.StaticType = m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.UndefinedType, exp.SemanticSymbol.StaticType});
            }
            else
            {
                exp.SemanticSymbol.StaticType = m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.UndefinedType, m_ModelCore.NullType, exp.SemanticSymbol.StaticType});
            }
        } // end optional tuple access
        // optional proxy indexing
        else
        {
            var proxy = InheritedProxies.Find(nonNullBase.StaticType, Operator.ProxyToGetIndex);

            if (proxy == null)
            {
                VerifyError(null, 201, exp.Span.Value, new DiagnosticArguments {["t"] = baseType.ToNonNullableType()});
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }

            LimitExpType(exp.Key, proxy.StaticType.FunctionRequiredParameters[0].Type);

            if (baseType.IncludesNull && !baseType.IncludesUndefined)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.NullType, proxy.StaticType.FunctionReturnType}));
            }
            else if (!baseType.IncludesNull && baseType.IncludesUndefined)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.UndefinedType, proxy.StaticType.FunctionReturnType}));
            }
            else
            {
                exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.UndefinedType, m_ModelCore.NullType, proxy.StaticType.FunctionReturnType}));
            }
        } // end optional proxy indexing

        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // index expression (optional)

    private Symbol VerifyOptCallExp(Ast.OptCallExpression exp)
    {
        var @base = VerifyExpAsValue(exp.Base, null);
        if (@base == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        Symbol r = null;

        var baseType = @base.StaticType;
        if (!baseType.IncludesNull && !baseType.IncludesUndefined)
        {
            // VerifyError: optional member base must possibly be undefined
            // or null.
            VerifyError(null, 170, exp.Base.Span.Value, new DiagnosticArguments {["t"] = baseType});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        var nonNullBase = m_ModelCore.Factory.Value(baseType.ToNonNullableType());
        exp.SemanticNonNullBase = nonNullBase;

        toDo();

        if (nonNullBase.StaticType is FunctionType)
        {
            VerifyFunctionCall(exp.ArgumentsList, exp.Span.Value, nonNullBase.StaticType);
            r = m_ModelCore.Factory.Value(nonNullBase.StaticType.FunctionReturnType);
        }
        else if (nonNullBase is Value && nonNullBase.StaticType == m_ModelCore.FunctionType)
        {
            var arrayOfAny = m_ModelCore.Factory.TypeWithArguments(m_ModelCore.ArrayType, new Symbol[]{m_ModelCore.AnyType});
            var functionTakingAny = m_ModelCore.Factory.FunctionType(null, null, new NameAndTypePair("_", arrayOfAny), m_ModelCore.AnyType);
            VerifyFunctionCall(exp.ArgumentsList, exp.Span.Value, functionTakingAny);
            r = m_ModelCore.Factory.Value(m_ModelCore.AnyType);
        }
        else if (nonNullBase is Value)
        {
            // VerifyError: non callable type
            VerifyError(null, 207, exp.Span.Value, new DiagnosticArguments {["t"] = nonNullBase.StaticType});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        else
        {
            // VerifyError: not callable
            VerifyError(null, 206, exp.Span.Value, new DiagnosticArguments {});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        if (baseType.IncludesNull && !baseType.IncludesUndefined)
        {
            r = m_ModelCore.Factory.Value(m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.NullType, r.StaticType}));
        }
        else if (!baseType.IncludesNull && baseType.IncludesUndefined)
        {
            r = m_ModelCore.Factory.Value(m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.UndefinedType, r.StaticType}));
        }
        else
        {
            r = m_ModelCore.Factory.Value(m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.UndefinedType, m_ModelCore.NullType, r.StaticType}));
        }

        exp.SemanticSymbol = r;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // call expression (optional)
}