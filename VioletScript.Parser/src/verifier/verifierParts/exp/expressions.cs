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
    public Symbol VerifyExp
    (
        Ast.Expression exp,
        Symbol expectedType = null,
        bool instantiatingGeneric = false,
        bool writting = false
    )
    {
        if (exp.SemanticExpResolved)
        {
            return exp.SemanticSymbol;
        }
        var r = VerifyConstantExp(exp, false, expectedType, instantiatingGeneric);
        if (r != null)
        {
            exp.SemanticExpResolved = true;
            return r;
        }
        if (exp is Ast.Identifier id)
        {
            r = VerifyLexicalReference(id, expectedType, instantiatingGeneric);
        }
        else if (exp is Ast.MemberExpression memb)
        {
            r = VerifyMemberExp(memb, expectedType, instantiatingGeneric);
        }
        else if (exp is Ast.ImportMetaExpression importMeta)
        {
            r = VerifyImportMeta(importMeta);
        }
        else if (exp is Ast.EmbedExpression embedExp)
        {
            r = VerifyEmbedExp(embedExp, expectedType);
        }
        else if (exp is Ast.UnaryExpression unaryExp)
        {
            r = VerifyUnaryExp(unaryExp, expectedType, writting);
        }
        else if (exp is Ast.BinaryExpression binaryExp)
        {
            r = VerifyBinaryExp(binaryExp, expectedType);
        }
        else
        {
            throw new Exception("Unimplemented expression");
        }

        if (writting && r.ReadOnly)
        {
            VerifyError(null, 175, exp.Span.Value, new DiagnosticArguments {});
            exp.SemanticSymbol = null;
        }
        else if (!writting && r.WriteOnly)
        {
            VerifyError(null, 174, exp.Span.Value, new DiagnosticArguments {});
            exp.SemanticSymbol = null;
        }

        return exp.SemanticSymbol;
    } // VerifyExp

    // verifies lexical reference; ensure
    // - it is not undefined.
    // - it is not an ambiguous reference.
    // - it is lexically visible.
    // - if it is a non-argumented generic type or function, throw a VerifyError.
    private Symbol VerifyLexicalReference
    (
        Ast.Identifier id,
        Symbol expectedType = null,
        bool instantiatingGeneric = false
    )
    {
        var exp = id;
        var r = m_Frame.ResolveProperty(id.Name);
        if (r == null)
        {
            // VerifyError: undefined reference
            VerifyError(null, 128, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        else if (r is AmbiguousReferenceIssue)
        {
            // VerifyError: ambiguous reference
            VerifyError(null, 129, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        else
        {
            if (!r.PropertyIsVisibleTo(m_Frame))
            {
                // VerifyError: accessing private property
                VerifyError(null, 130, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
            r = r is Alias ? r.AliasToSymbol : r;
            // VerifyError: unargumented generic type or function
            if (!instantiatingGeneric && r.TypeParameters != null)
            {
                VerifyError(null, 132, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }

            // extend variable life
            if (r is ReferenceValueFromFrame && r.Base.FindActivation() != m_Frame.FindActivation())
            {
                r.Base.FindActivation().AddExtendedLifeVariable(r.Property);
            }

            if (id.Type != null)
            {
                VerifyError(null, 152, exp.Span.Value, new DiagnosticArguments {});
                VerifyTypeExp(id.Type);
            }

            exp.SemanticSymbol = r;
            exp.SemanticExpResolved = true;
            return r;
        }
    } // lexical reference

    // verifies member; ensure
    // - it is not undefined.
    // - it is lexically visible.
    // - if it is a non-argumented generic type or function, throw a VerifyError.
    // - it is a compile-time value.
    private Symbol VerifyMemberExp
    (
        Ast.MemberExpression memb,
        Symbol expectedType,
        bool instantiatingGeneric
    )
    {
        if (memb.Optional)
        {
            return VerifyOptMemberExp(memb, expectedType, instantiatingGeneric);
        }
        Ast.Expression exp = memb;
        var @base = VerifyExp(memb.Base, null, false);
        if (@base == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var r = @base.ResolveProperty(memb.Id.Name);
        if (r == null)
        {
            // VerifyError: undefined reference
            VerifyError(null, 128, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
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
            r = r is Alias ? r.AliasToSymbol : r;
            // VerifyError: unargumented generic type or function
            if (!instantiatingGeneric && r.TypeParameters != null)
            {
                VerifyError(null, 132, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }

            // extend variable life
            if (r is ReferenceValueFromFrame && r.Base.FindActivation() != m_Frame.FindActivation())
            {
                r.Base.FindActivation().AddExtendedLifeVariable(r.Property);
            }

            exp.SemanticSymbol = r;
            exp.SemanticExpResolved = true;
            return r;
        }
    } // member expression

    // verifies member; ensure
    // - it is not undefined.
    // - it is lexically visible.
    // - if it is a non-argumented generic type or function, throw a VerifyError.
    // - it is a compile-time value.
    private Symbol VerifyOptMemberExp
    (
        Ast.MemberExpression memb,
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

        var throwawayNonNullBase = m_ModelCore.Factory.Value(baseType.ToNonNullableType());
        memb.SemanticThrowawayNonNullBase = throwawayNonNullBase;

        var r = throwawayNonNullBase.ResolveProperty(memb.Id.Name);
        memb.SemanticOptNonNullUnifiedSymbol = r;

        if (r == null)
        {
            // VerifyError: undefined reference
            VerifyError(null, 128, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
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
            r = r is Alias ? r.AliasToSymbol : r;
            // VerifyError: unargumented generic type or function
            if (!instantiatingGeneric && r.TypeParameters != null)
            {
                VerifyError(null, 132, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }

            // extend variable life
            if (r is ReferenceValueFromFrame && r.Base.FindActivation() != m_Frame.FindActivation())
            {
                r.Base.FindActivation().AddExtendedLifeVariable(r.Property);
            }

            exp.SemanticExpResolved = true;

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

            return exp.SemanticSymbol;
        }
    } // member expression (?.)

    private Symbol VerifyImportMeta(Ast.ImportMetaExpression exp)
    {
        exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.InternRecordType(new NameAndTypePair[]
        {
            new NameAndTypePair("url", m_ModelCore.StringType),
        }));
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // import meta

    private Symbol VerifyEmbedExp(Ast.EmbedExpression exp, Symbol expectedType)
    {
        Symbol type = null;
        if (exp.Type != null)
        {
            type = VerifyTypeExp(exp.Type);
            if (type == null || !(type == m_ModelCore.StringType || type == m_ModelCore.ByteArrayType))
            {
                if (type != null)
                {
                    VerifyError(null, 171, exp.Span.Value, new DiagnosticArguments { ["t"] = type });
                }
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
        }
        else
        {
            type = expectedType == m_ModelCore.ByteArrayType
                || expectedType == m_ModelCore.StringType ? expectedType : null;
            if (type == null)
            {
                VerifyError(null, 172, exp.Span.Value, new DiagnosticArguments {});
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
        }
        exp.SemanticSymbol = m_ModelCore.Factory.Value(type);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // embed expression

    private Symbol VerifyUnaryExp(Ast.UnaryExpression exp, Symbol expectedType, bool writting)
    {
        Symbol operand = null;

        if (exp.Operator == Operator.Yield)
        {
            var generatorType = CurrentFunction.StaticType.FunctionReturnType;
            if (!generatorType.IsInstantiationOf(m_ModelCore.GeneratorType))
            {
                throw new Exception("Internal verify error");
            }
            operand = LimitExpType(exp.Operand, generatorType.ArgumentTypes[0]);
            exp.SemanticSymbol = m_ModelCore.Factory.UndefinedConstantValue();
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        bool isIncrementOrDecrementOperator =
            exp.Operator == Operator.PreIncrement || exp.Operator == Operator.PreDecrement
        ||  exp.Operator == Operator.PostIncrement || exp.Operator == Operator.PostDecrement;

        operand = VerifyExpAsValue(exp.Operand, expectedType, false, exp.Operator == Operator.NonNull ? writting : isIncrementOrDecrementOperator);
        if (operand == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        if (exp.Operator == Operator.Await)
        {
            if (!operand.StaticType.IsInstantiationOf(m_ModelCore.PromiseType))
            {
                VerifyError(null, 173, exp.Operand.Span.Value, new DiagnosticArguments {});
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
            exp.SemanticSymbol = m_ModelCore.Factory.Value(operand.StaticType.ArgumentTypes[0]);
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        if (exp.Operator == Operator.Delete)
        {
            // - ensure the operand is a brackets operation.
            // - ensure the operand type has a delete proxy.
            if (!(operand is IndexValue))
            {
                VerifyError(null, 176, exp.Operand.Span.Value, new DiagnosticArguments {});
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
            if (InheritedProxies.Find(operand.Base.StaticType, Operator.ProxyToSetIndex) == null)
            {
                VerifyError(null, 177, exp.Span.Value, new DiagnosticArguments {["t"] = operand.Base.StaticType});
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
            exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.BooleanType);
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        if (exp.Operator == Operator.Typeof)
        {
            exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.StringType);
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        if (exp.Operator == Operator.Void)
        {
            exp.SemanticSymbol = m_ModelCore.Factory.UndefinedConstantValue();
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        if (exp.Operator == Operator.LogicalNot)
        {
            exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.BooleanType);
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        if (exp.Operator == Operator.Positive
        ||  exp.Operator == Operator.Negate
        ||  exp.Operator == Operator.BitwiseNot)
        {
            var proxy = InheritedProxies.Find(operand.StaticType, exp.Operator);
            if (proxy == null)
            {
                // unsupported operator
                VerifyError(null, 178, exp.Span.Value, new DiagnosticArguments {["t"] = operand.StaticType, ["op"] = exp.Operator});
            }
            exp.SemanticSymbol = proxy == null ? null : m_ModelCore.Factory.Value(proxy.StaticType.FunctionReturnType);
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        if (exp.Operator == Operator.NonNull)
        {
            if (!(operand.StaticType.IncludesNull || operand.StaticType.IncludesUndefined))
            {
                VerifyError(null, 170, exp.Span.Value, new DiagnosticArguments {["t"] = operand.StaticType});
            }
            exp.SemanticSymbol = m_ModelCore.Factory.Value(operand.StaticType.ToNonNullableType());
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        if (isIncrementOrDecrementOperator)
        {
            if (!m_ModelCore.IsNumericType(operand.StaticType))
            {
                VerifyError(null, 179, exp.Span.Value, new DiagnosticArguments {["t"] = operand.StaticType});
            }
            exp.SemanticSymbol = m_ModelCore.Factory.Value(operand.StaticType);
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        throw new Exception("Unimplemented");
    } // unary expression

    private Symbol VerifyBinaryExp(Ast.BinaryExpression exp, Symbol expectedType)
    {
    } // binary expression
}