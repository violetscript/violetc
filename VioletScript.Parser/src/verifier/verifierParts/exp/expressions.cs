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
    private Symbol VerifyExp
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
        else if (exp is Ast.TypeBinaryExpression tBinaryExp)
        {
            r = VerifyTypeBinaryExp(tBinaryExp);
        }
        else if (exp is Ast.DefaultExpression defaultExp)
        {
            r = VerifyDefaultExp(defaultExp);
        }
        else if (exp is Ast.FunctionExpression fnExp)
        {
            r = VerifyFunctionExp(fnExp, expectedType);
        }
        else if (exp is Ast.ObjectInitializer objInit)
        {
            r = VerifyObjectInitialiser(objInit, expectedType);
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

    private Symbol VerifyExpAsValue
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
        var r = VerifyExp(exp, expectedType, instantiatingGeneric, writting);
        if (r == null)
        {
            return null;
        }
        if (!(r is Value))
        {
            VerifyError(null, 180, exp.Span.Value, new DiagnosticArguments {});
            exp.SemanticSymbol = null;
            return null;
        }
        return r;
    } // VerifyExpAsValue

    private Symbol LimitExpType
    (
        Ast.Expression exp,
        Symbol expectedType
    )
    {
        if (exp.SemanticExpResolved)
        {
            return exp.SemanticSymbol;
        }
        var r = VerifyExpAsValue(exp, expectedType);
        if (r == null)
        {
            return null;
        }
        if (r.StaticType == expectedType)
        {
            return r;
        }
        var conversion = TypeConversions.ConvertImplicit(r, expectedType);
        if (conversion == null)
        {
            VerifyError(null, 168, exp.Span.Value, new DiagnosticArguments {["expected"] = expectedType, ["got"] = r.StaticType});
        }
        exp.SemanticSymbol = conversion;
        return exp.SemanticSymbol;
    } // LimitExpType

    private Symbol LimitStrictExpType
    (
        Ast.Expression exp,
        Symbol expectedType
    )
    {
        if (exp.SemanticExpResolved)
        {
            return exp.SemanticSymbol;
        }
        var r = VerifyExpAsValue(exp, expectedType);
        if (r == null)
        {
            return null;
        }
        if (r.StaticType == expectedType)
        {
            return r;
        }
        VerifyError(null, 168, exp.Span.Value, new DiagnosticArguments {["expected"] = expectedType, ["got"] = r.StaticType});
        exp.SemanticSymbol = null;
        return exp.SemanticSymbol;
    } // LimitStrictExpType

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
            var generatorType = CurrentMethodSlot.StaticType.FunctionReturnType;
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
            if (InheritedProxies.Find(operand.Base.StaticType, Operator.ProxyToDeleteIndex) == null)
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
        if (exp.Operator == Operator.In)
        {
            return In_VerifyBinaryExp(exp, expectedType);
        }
        if (exp.Operator == Operator.Equals || exp.Operator == Operator.NotEquals)
        {
            return Eq_VerifyBinaryExp(exp);
        }
        if (exp.Operator == Operator.StrictEquals || exp.Operator == Operator.StrictNotEquals)
        {
            return StrictEq_VerifyBinaryExp(exp);
        }
        if (exp.Operator == Operator.LogicalAnd
        ||  exp.Operator == Operator.LogicalXor
        ||  exp.Operator == Operator.LogicalOr)
        {
            return Logical_VerifyBinaryExp(exp, expectedType);
        }
        if (exp.Operator == Operator.NullCoalescing)
        {
            return NullCoalescing_VerifyBinaryExp(exp, expectedType);
        }
        return ProxySupported_VerifyBinaryExp(exp, expectedType);
    } // binary expression

    private Symbol In_VerifyBinaryExp(Ast.BinaryExpression exp, Symbol expectedType)
    {
        var @base = VerifyExpAsValue(exp.Right);
        if (@base == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var proxy = InheritedProxies.Find(@base.StaticType, Operator.In);
        if (proxy == null)
        {
            VerifyError(null, 178, exp.Span.Value, new DiagnosticArguments {["t"] = @base.StaticType, ["op"] = Operator.In});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        LimitExpType(exp.Left, proxy.FunctionRequiredParameters[0].Type);
        exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.BooleanType);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // binary expression ("in")

    private Symbol Eq_VerifyBinaryExp(Ast.BinaryExpression exp)
    {
        var left = VerifyExpAsValue(exp.Left);
        if (left == null)
        {
            VerifyExpAsValue(exp.Right);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        LimitExpType(exp.Right, left.StaticType);
        exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.BooleanType);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // binary expression (equals or not equals)

    private Symbol StrictEq_VerifyBinaryExp(Ast.BinaryExpression exp)
    {
        var left = VerifyExpAsValue(exp.Left);
        if (left == null)
        {
            VerifyExpAsValue(exp.Right);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        LimitStrictExpType(exp.Right, left.StaticType);
        exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.BooleanType);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // binary expression (strict equals or not equals)

    private Symbol Logical_VerifyBinaryExp(Ast.BinaryExpression exp, Symbol expectedType)
    {
        var left = VerifyExpAsValue(exp.Left, expectedType);
        if (left == null)
        {
            VerifyExpAsValue(exp.Right);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        LimitExpType(exp.Right, left.StaticType);
        exp.SemanticSymbol = m_ModelCore.Factory.Value(left.StaticType);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // binary expression (logical and, xor or or)

    private Symbol NullCoalescing_VerifyBinaryExp(Ast.BinaryExpression exp, Symbol expectedType)
    {
        var left = VerifyExpAsValue(exp.Left, expectedType);
        if (left == null)
        {
            VerifyExpAsValue(exp.Right);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var leftNonNullType = left.StaticType.ToNonNullableType();
        LimitExpType(exp.Right, leftNonNullType);
        exp.SemanticSymbol = m_ModelCore.Factory.Value(leftNonNullType);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // binary expression (null coalescing)

    private Symbol ProxySupported_VerifyBinaryExp(Ast.BinaryExpression exp, Symbol expectedType)
    {
        var left = VerifyExpAsValue(exp.Left, expectedType);
        if (left == null)
        {
            VerifyExpAsValue(exp.Right);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var proxy = InheritedProxies.Find(left.StaticType, exp.Operator);
        if (proxy == null)
        {
            VerifyError(null, 178, exp.Span.Value, new DiagnosticArguments {["t"] = left.StaticType, ["op"] = Operator.In});
            VerifyExpAsValue(exp.Right);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        LimitExpType(exp.Right, proxy.FunctionRequiredParameters[1].Type);
        exp.SemanticSymbol = m_ModelCore.Factory.Value(proxy.FunctionReturnType);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // binary expression (proxy-supported)

    private Symbol VerifyTypeBinaryExp(Ast.TypeBinaryExpression exp)
    {
        var left = VerifyExpAsValue(exp.Left);
        if (left == null)
        {
            VerifyTypeExp(exp.Right);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var right = VerifyTypeExp(exp.Right);
        if (right == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        if (exp.Operator == Operator.As || exp.Operator == Operator.AsStrict)
        {
            return As_VerifyTypeBinaryExp(exp, left, right);
        }
        else
        {
            return Is_VerifyTypeBinaryExp(exp, left, right);
        }
    } // binary expression ("as"/"instanceof"/"is")

    private Symbol As_VerifyTypeBinaryExp(Ast.TypeBinaryExpression exp, Symbol left, Symbol right)
    {
        var strict = exp.Operator == Operator.AsStrict;
        var conversion = TypeConversions.ConvertExplicit(left, right, !strict);
        if (conversion == null)
        {
            VerifyError(null, 168, exp.Span.Value, new DiagnosticArguments {["expected"] = right, ["got"] = left.StaticType});
        }
        exp.SemanticSymbol = conversion;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // binary expression ("as")

    private Symbol Is_VerifyTypeBinaryExp(Ast.TypeBinaryExpression exp, Symbol left, Symbol right)
    {
        if (exp.BindsTo != null)
        {
            VerifyError(null, 184, exp.BindsTo.Span.Value, new DiagnosticArguments {});
        }
        if (right == m_ModelCore.AnyType)
        {
            Warn(null, 182, exp.Span.Value, new DiagnosticArguments {});
        }
        else if (right == left.StaticType)
        {
            Warn(null, 183, exp.Span.Value, new DiagnosticArguments {["right"] = right});
        }
        else if (!right.IsSubtypeOf(left.StaticType))
        {
            Warn(null, 181, exp.Span.Value, new DiagnosticArguments {["right"] = right});
        }
        exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.BooleanType);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // binary expression ("instanceof"/"is")

    private Symbol VerifyDefaultExp(Ast.DefaultExpression exp)
    {
        var t = VerifyTypeExp(exp.Type);
        if (t == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var defaultValue = t.DefaultValue;
        if (defaultValue == null)
        {
            VerifyError(null, 159, exp.Span.Value, new DiagnosticArguments {["t"] = t});
        }
        exp.SemanticSymbol = defaultValue;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // default expression

    private Symbol VerifyFunctionExp(Ast.FunctionExpression exp, Symbol expectedType)
    {
        var common = exp.Common;
        Symbol inferType = null;
        if (expectedType is UnionType)
        {
            var functionTypes = expectedType.UnionMemberTypes.Where(t => t is FunctionType);
            inferType = functionTypes.FirstOrDefault();
        }
        inferType = inferType ?? (expectedType is FunctionType ? expectedType : null);

        Symbol prevActivation = m_Frame.FindActivation();
        Symbol activation = m_ModelCore.Factory.ActivationFrame();
        common.SemanticActivation = activation;
        // inherit "this"
        activation.ActivationThisOrThisAsStaticType = prevActivation?.ActivationThisOrThisAsStaticType;

        // define identifier partially.
        // the identifier's static type is resolved before
        // the body of the function is resolved.
        if (exp.Id != null)
        {
            var variable = m_ModelCore.Factory.VariableSlot(exp.Id.Name, true, null);
            exp.Id.SemanticSymbol = variable;
            activation.Properties[exp.Id.Name] = variable;
        }

        Symbol methodSlot = m_ModelCore.Factory.MethodSlot("", null,
                (common.UsesAwait ? MethodSlotFlags.UsesAwait : 0)
            |   (common.UsesYield ? MethodSlotFlags.UsesYield : 0));

        bool valid = true;
        Symbol resultType = null;

        // resolve common before pushing to method slot stack,
        // since its type is unknown.
        List<NameAndTypePair> resultType_params = null;
        List<NameAndTypePair> resultType_optParams = null;
        NameAndTypePair? resultType_restParam = null;
        Symbol resultType_returnType = null;
        if (common.Params != null)
        {
            resultType_params = new List<NameAndTypePair>();
            var actualCount = common.Params.Count();
            for (int i = 0; i < actualCount; ++i)
            {
                var binding = common.Params[i];
                NameAndTypePair? paramInferNameAndType = inferType != null && inferType.FunctionHasRequiredParameters && i < inferType.FunctionCountOfRequiredParameters ? inferType.FunctionRequiredParameters[i] : null;
                FRequiredParam_VerifyVariableBinding(binding, activation.Properties, paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Type : null);
                var name = binding.Pattern is Ast.BindPattern p ? p.Name : paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Name : "_";
                resultType_params.Add(new NameAndTypePair(name, binding.Pattern.SemanticProperty.StaticType));
            }
        }
        if (common.OptParams != null)
        {
            resultType_optParams = new List<NameAndTypePair>();
            var actualCount = common.OptParams.Count();
            for (int i = 0; i < actualCount; ++i)
            {
                var binding = common.OptParams[i];
                NameAndTypePair? paramInferNameAndType = inferType != null && inferType.FunctionHasOptParameters && i < inferType.FunctionCountOfOptParameters ? inferType.FunctionOptParameters[i] : null;
                FOptParam_VerifyVariableBinding(binding, activation.Properties, paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Type : null);
                var name = binding.Pattern is Ast.BindPattern p ? p.Name : paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Name : "_";
                resultType_optParams.Add(new NameAndTypePair(name, binding.Pattern.SemanticProperty.StaticType));
            }
        }
        // type of a rest parameter must be * or an Array
        if (common.RestParam != null)
        {
            var binding = common.RestParam;
            NameAndTypePair? paramInferNameAndType = inferType != null ? inferType.FunctionRestParameter : null;
            FRestParam_VerifyVariableBinding(binding, activation.Properties, paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Type : null);
            var name = binding.Pattern is Ast.BindPattern p ? p.Name : paramInferNameAndType.HasValue ? paramInferNameAndType.Value.Name : "_";
            resultType_restParam = new NameAndTypePair(name, binding.Pattern.SemanticProperty.StaticType);
        }
        if (common.ReturnType != null)
        {
            resultType_returnType = VerifyTypeExp(common.ReturnType);
            if (resultType_returnType == null)
            {
                valid = false;
                resultType_returnType = m_ModelCore.AnyType;
            }
        }
        else
        {
            resultType_returnType = inferType?.FunctionReturnType ?? m_ModelCore.AnyType;
        }

        // ignore "throws" clause
        if (common.ThrowsType != null)
        {
            VerifyTypeExp(common.ThrowsType);
        }

        // if there is an inferred type and parameters were omitted,
        // add them to the resulting type if applicable.
        // this is done except if the expected type was an union with
        // more than one function type.
        int nOfInferFunctionTypes = expectedType is UnionType
            ? expectedType.UnionMemberTypes.Select(t => t is FunctionType).Count()
            : expectedType is FunctionType
            ? 1
            : 0;
        if (nOfInferFunctionTypes == 1)
        {
            var r = FunctionExp_AddMissingParameters
            (
                resultType_params,
                resultType_optParams,
                resultType_restParam,
                inferType
            );
            resultType_params = r.Item1;
            resultType_optParams = r.Item2;
            resultType_restParam = r.Item3;
        }

        // - if the function uses 'await', automatically wrap its return to Promise.
        // - if the function uses 'yield', automatically wrap its return to Generator.
        if (common.UsesAwait && !resultType_returnType.IsInstantiationOf(m_ModelCore.PromiseType))
        {
            resultType_returnType = m_ModelCore.Factory.InstantiatedType(m_ModelCore.PromiseType, new Symbol[]{resultType_returnType});
        }
        else if (common.UsesYield && !resultType_returnType.IsInstantiationOf(m_ModelCore.GeneratorType))
        {
            resultType_returnType = m_ModelCore.Factory.InstantiatedType(m_ModelCore.GeneratorType, new Symbol[]{resultType_returnType});
        }

        // get result type
        resultType = m_ModelCore.Factory.FunctionType
        (
            resultType_params?.ToArray(),
            resultType_optParams?.ToArray(),
            resultType_restParam,
            resultType_returnType
        );

        // if identifier was defined, assign its static type.
        if (exp.Id != null)
        {
            exp.Id.SemanticSymbol.StaticType = resultType;
        }

        EnterFrame(activation);
        m_MethodSlotStack.Push(methodSlot);

        // resolve body.
        VerifyFunctionBody(common.Body, methodSlot);

        m_MethodSlotStack.Pop();
        ExitFrame();

        exp.SemanticSymbol = valid ? m_ModelCore.Factory.FunctionExpValue(resultType) : null;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // function expression

    private
    (
        List<NameAndTypePair>,
        List<NameAndTypePair>,
        NameAndTypePair?
    )
    FunctionExp_AddMissingParameters
    (
        List<NameAndTypePair> resultType_params,
        List<NameAndTypePair> resultType_optParams,
        NameAndTypePair? resultType_restParam,
        Symbol inferType
    )
    {
        var mixedResultParams = RequiredOrOptOrRestParam.FromLists
        (
            resultType_params,
            resultType_optParams,
            resultType_restParam
        );
        var mixedInferParams = RequiredOrOptOrRestParam.FromType(inferType);
        var compatible = mixedResultParams.Count() <= mixedInferParams.Count();
        if (compatible)
        {
            for (int i = 0; i < mixedResultParams.Count(); ++i)
            {
                if (mixedResultParams[i].Kind != mixedInferParams[i].Kind)
                {
                    compatible = false;
                    break;
                }
            }
            if (compatible)
            {
                for (int i = mixedResultParams.Count(); i < mixedInferParams.Count(); ++i)
                {
                    mixedResultParams.Add(mixedInferParams[i]);
                }
            }
        }

        return RequiredOrOptOrRestParam.SeparateKinds(mixedResultParams);
    } // FunctionExp_AddMissingParameters

    private Symbol VerifyObjectInitialiser(Ast.ObjectInitializer exp, Symbol expectedType)
    {
        //
    } // object initializer
}