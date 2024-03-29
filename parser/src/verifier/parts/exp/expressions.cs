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
        bool writting = false,
        bool checkUndesiredAssign = true
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
        else if (exp is Ast.ArrayInitializer arrInit)
        {
            r = VerifyArrayInitialiser(arrInit, expectedType);
        }
        else if (exp is Ast.MarkupInitializer markupInit)
        {
            r = VerifyMarkupInitializer(markupInit);
        }
        else if (exp is Ast.MarkupListInitializer markupListInit)
        {
            r = VerifyMarkupListInitializer(markupListInit, expectedType);
        }
        else if (exp is Ast.IndexExpression idxExp)
        {
            r = VerifyIndexExp(idxExp);
        }
        else if (exp is Ast.CallExpression callExp)
        {
            r = VerifyCallExp(callExp);
        }
        else if (exp is Ast.ThisLiteral thisLiteral)
        {
            r = VerifyThisLiteral(thisLiteral);
        }
        else if (exp is Ast.StringLiteral strLiteral)
        {
            r = VerifyStringLiteral(strLiteral, expectedType);
        }
        else if (exp is Ast.NullLiteral)
        {
            throw new Exception("Constant NullLiteral should never fail");
        }
        else if (exp is Ast.BooleanLiteral)
        {
            throw new Exception("Constant BooleanLiteral should never fail");
        }
        else if (exp is Ast.NumericLiteral)
        {
            throw new Exception("Constant NumericLiteral should never fail");
        }
        else if (exp is Ast.RegExpLiteral reLiteral)
        {
            r = VerifyRegExpLiteral(reLiteral);
        }
        else if (exp is Ast.ConditionalExpression condExp)
        {
            r = VerifyConditionalExp(condExp, expectedType);
        }
        else if (exp is Ast.ParensExpression parensExp)
        {
            r = VerifyParensExp(parensExp, expectedType, instantiatingGeneric, writting, checkUndesiredAssign);
        }
        else if (exp is Ast.ListExpression listExp)
        {
            r = VerifyListExp(listExp, expectedType);
        }
        else if (exp is Ast.ExpressionWithTypeArguments gie)
        {
            r = VerifyExpWithTypeArgs(gie);
        }
        else if (exp is Ast.AssignmentExpression assignExp)
        {
            if (checkUndesiredAssign)
            {
                VerifyError(null, 270, exp.Span.Value, new DiagnosticArguments {});
            }
            r = VerifyAssignmentExp(assignExp);
        }
        else if (exp is Ast.NewExpression newExp)
        {
            r = VerifyNewExp(newExp);
        }
        else if (exp is Ast.SuperExpression superExp)
        {
            r = VerifySuperExp(superExp);
        }
        else if (exp is Ast.OptChainingExpression optChaining)
        {
            r = VerifyOptChainingExp(optChaining, expectedType, instantiatingGeneric);
        }
        else if (exp is Ast.OptionalChainingPlaceholder)
        {
            if (exp.SemanticSymbol == null)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.NullConstantValue(m_ModelCore.NullType);
            }
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        else
        {
            throw new Exception("Unimplemented expression");
        }

        if (writting && r != null && r.ReadOnly)
        {
            // writing to read-only variable from 'this' is allowed in
            // in constructors.
            var ctorWritableVar = m_Frame is ActivationFrame
                && r is ReferenceValue
                && r.Base == m_Frame.ActivationThisOrThisAsStaticType
                && r.Property is VariableSlot
                && CurrentMethodSlot != null
                && CurrentMethodSlot.MethodFlags.HasFlag(MethodSlotFlags.Constructor);

            if (!ctorWritableVar)
            {
                VerifyError(null, 175, exp.Span.Value, new DiagnosticArguments {});
                exp.SemanticSymbol = null;
            }
        }
        else if (!writting && r != null && r.WriteOnly)
        {
            VerifyError(null, 174, exp.Span.Value, new DiagnosticArguments {});
            exp.SemanticSymbol = null;
        }

        if (r is Value && r.StaticType == null)
        {
            VerifyError(null, 199, exp.Span.Value, new DiagnosticArguments {});
            exp.SemanticSymbol = null;
        }

        return exp.SemanticSymbol;
    } // VerifyExp

    private Symbol VerifyExpAsValue
    (
        Ast.Expression exp,
        Symbol expectedType = null,
        bool instantiatingGeneric = false,
        bool writting = false,
        bool checkUndesiredAssign = true
    )
    {
        if (exp.SemanticExpResolved)
        {
            return exp.SemanticSymbol;
        }
        var r = VerifyExp(exp, expectedType, instantiatingGeneric, writting, checkUndesiredAssign);
        if (r == null)
        {
            return null;
        }
        if (r is Type && r.TypeParameters == null)
        {
            r = m_ModelCore.Factory.TypeAsValue(r);
        }
        else if (r is Namespace)
        {
            r = m_ModelCore.Factory.NamespaceAsValue(r);
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
        Symbol expectedType,
        bool writting = false,
        bool checkUndesiredAssign = true
    )
    {
        if (exp.SemanticExpResolved)
        {
            return exp.SemanticSymbol;
        }
        var r = VerifyExpAsValue(exp, expectedType, false, writting, checkUndesiredAssign);
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
        Symbol expectedType,
        bool writting = false,
        bool checkUndesiredAssign = true
    )
    {
        if (exp.SemanticExpResolved)
        {
            return exp.SemanticSymbol;
        }
        var r = VerifyExpAsValue(exp, expectedType, false, writting, checkUndesiredAssign);
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
            ReportNameNotFound(id.Name, exp.Span.Value, null);
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
            if (r == null || !(r is Alias && r.IsGenericTypeOrMethod))
            {
                r = r?.EscapeAlias();
            }
            // VerifyError: unargumented generic type or function
            if (!instantiatingGeneric && r.IsGenericTypeOrMethod)
            {
                VerifyError(null, 132, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }

            // extend variable life
            if (r is ReferenceValueFromFrame && r.Base.FindActivation() != m_Frame.FindActivation())
            {
                r.Base.FindActivation()?.AddExtendedLifeVariable(r.Property);
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
        Ast.Expression exp = memb;
        var @base = VerifyExp(memb.Base, null, false);
        if (@base == null)
        {
            if (memb.Id.Name == "fooQuxxzx")
            {
                //Console.WriteLine(memb.Base.SemanticSymbol == null);
            }
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var r = @base.ResolveProperty(memb.Id.Name);
        if (r == null)
        {
            // VerifyError: undefined reference
            ReportNameNotFound(memb.Id.Name, memb.Id.Span.Value, @base);
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
            if (r == null || !(r is Alias && r.IsGenericTypeOrMethod))
            {
                r = r?.EscapeAlias();
            }
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

            exp.SemanticSymbol = r;
            exp.SemanticExpResolved = true;
            return r;
        }
    } // member expression

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
            if (!generatorType.IsArgumentationOf(m_ModelCore.IteratorType))
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
            if (!operand.StaticType.IsArgumentationOf(m_ModelCore.PromiseType))
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
            // unary operator of the any type (*)
            if (operand.StaticType == this.m_ModelCore.AnyType)
            {
                exp.SemanticSymbol = this.m_ModelCore.Factory.Value(this.m_ModelCore.AnyType);
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }

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

            // non-null over an indexing will yield the same index value
            // symbol from the base, however will mutate its static type
            // to not include null or undefined.
            if (operand is IndexValue)
            {
                operand.StaticType = operand.StaticType.ToNonNullableType();
                exp.SemanticSymbol = operand;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
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
        LimitExpType(exp.Left, proxy.StaticType.FunctionRequiredParameters[0].Type);
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
        var ttFrames = exp.Operator == Operator.LogicalAnd ? exp.Left.GetTypeTestFrames() : new List<Symbol>();
        EnterFrames(ttFrames);
        LimitExpType(exp.Right, left.StaticType);
        ExitNFrames(ttFrames.Count());
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

        LimitExpType(exp.Right, left.StaticType);
        exp.SemanticSymbol = m_ModelCore.Factory.Value(left.StaticType);
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

        // binary operator of the any type (*)
        if (left.StaticType == this.m_ModelCore.AnyType)
        {
            LimitExpType(exp.Right, this.m_ModelCore.AnyType);
            exp.SemanticSymbol = m_ModelCore.Factory.Value(this.m_ModelCore.AnyType);
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        var proxy = InheritedProxies.Find(left.StaticType, exp.Operator);
        if (proxy == null)
        {
            VerifyError(null, 178, exp.Span.Value, new DiagnosticArguments {["t"] = left.StaticType, ["op"] = exp.Operator});
            VerifyExpAsValue(exp.Right);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        LimitExpType(exp.Right, proxy.StaticType.FunctionRequiredParameters[1].Type);
        exp.SemanticSymbol = m_ModelCore.Factory.Value(proxy.StaticType.FunctionReturnType);
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
            VerifyError(null, 205, exp.Span.Value, new DiagnosticArguments {["from"] = left.StaticType, ["to"] = right});
        }
        exp.SemanticSymbol = conversion;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // binary expression ("as")

    private Symbol Is_VerifyTypeBinaryExp(Ast.TypeBinaryExpression exp, Symbol left, Symbol right)
    {
        if (right == m_ModelCore.AnyType)
        {
            Warn(null, 182, exp.Span.Value, new DiagnosticArguments {});
        }
        else if (right == left.StaticType)
        {
            Warn(null, 183, exp.Span.Value, new DiagnosticArguments {["right"] = right});
        }
        else if (!right.CanBeASubtypeOf(left.StaticType))
        {
            Warn(null, 181, exp.Span.Value, new DiagnosticArguments {["right"] = right});
        }
        if (exp.BindsTo != null)
        {
            var nextFrame = m_ModelCore.Factory.Frame();
            var nextVar = m_ModelCore.Factory.VariableSlot(exp.BindsTo.Name, false, right);
            nextFrame.Properties[nextVar.Name] = nextVar;
            exp.BindsTo.SemanticSymbol = nextVar;
            EnterFrame(nextFrame);
            ExitFrame();
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

    private Symbol VerifyMarkupInitializer(Ast.MarkupInitializer exp)
    {
        var type = VerifyExp(exp.Id);
        if (type == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var initialisable = type.IsClassType && type.ClassHasParameterlessConstructor && !type.DontInit;
        if (!initialisable)
        {
            VerifyError(null, 193, exp.Id.Span.Value, new DiagnosticArguments {["t"] = type});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var resultValue = m_ModelCore.Factory.Value(type);
        foreach (var attr in exp.Attributes)
        {
            MarkupInitializer_VerifyAttr(attr, resultValue);
        }
        // ensure IMarkupContainer is implemented.
        Symbol childType = type.GetIMarkupContainerChildType();
        if (childType == null)
        {
            VerifyError(null, 194, exp.Id.Span.Value, new DiagnosticArguments {["t"] = type});
        }
        childType ??= this.m_ModelCore.AnyType;
        if (exp.Children != null)
        {
            foreach (var child in exp.Children)
            {
                // limit spread type to [ChildType]
                if (child is Ast.Spread spread) {
                    LimitExpType(spread.Expression, this.m_ModelCore.Factory.TypeWithArguments(this.m_ModelCore.ArrayType, new Symbol[]{childType}));
                    continue;
                }
                // limit child type to ChildType
                LimitExpType(child, childType);
            }
        }
        exp.SemanticSymbol = resultValue;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // markup initializer

    // verify a markup attribute. ensure:
    // - it is writable.
    // - it is of Boolean type if it omits its value.
    private void MarkupInitializer_VerifyAttr(Ast.MarkupAttribute attr, Symbol markupResult)
    {
        var r = markupResult.ResolveProperty(attr.Id.Name);
        if (r == null)
        {
            // VerifyError: undefined property
            ReportNameNotFound(attr.Id.Name, attr.Id.Span.Value, markupResult);
            return;
        }
        if (!r.PropertyIsVisibleTo(m_Frame))
        {
            // VerifyError: accessing private property
            VerifyError(null, 130, attr.Id.Span.Value, new DiagnosticArguments { ["name"] = attr.Id.Name });
            return;
        }
        r = r?.EscapeAlias();
        // VerifyError: unargumented generic type or function
        if (r.IsGenericTypeOrMethod)
        {
            VerifyError(null, 132, attr.Id.Span.Value, new DiagnosticArguments { ["name"] = attr.Id.Name });
            return;
        }
        if (r is Type)
        {
            r = m_ModelCore.Factory.TypeAsValue(r);
        }
        else if (r is Namespace)
        {
            r = m_ModelCore.Factory.NamespaceAsValue(r);
        }
        if (!(r is Value))
        {
            VerifyError(null, 180, attr.Id.Span.Value, new DiagnosticArguments {});
            return;
        }
        if (!(r is ReferenceValue && (r.Property is VariableSlot || r.Property is VirtualSlot)))
        {
            VerifyError(null, 196, attr.Id.Span.Value, new DiagnosticArguments {});
            return;
        }
        if (r is Value && r.StaticType == null)
        {
            VerifyError(null, 199, attr.Id.Span.Value, new DiagnosticArguments {});
            return;
        }
        if (r.ReadOnly)
        {
            VerifyError(null, 147, attr.Id.Span.Value, new DiagnosticArguments {["name"] = attr.Id.Name});
        }
        if (attr.Value == null)
        {
            if (r.StaticType != m_ModelCore.BooleanType)
            {
                // VerifyError: property type isn't Boolean
                VerifyError(null, 197, attr.Id.Span.Value, new DiagnosticArguments {["name"] = attr.Id.Name, ["t"] = r.StaticType});
            }
        }
        else
        {
            LimitExpType(attr.Value, r.StaticType);
        }
    } // markup attribute

    private Symbol VerifyMarkupListInitializer(Ast.MarkupListInitializer exp, Symbol type)
    {
        if (type == null || !type.IsArgumentationOf(m_ModelCore.ArrayType))
        {
            VerifyError(null, 200, exp.Span.Value, new DiagnosticArguments {});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var elementType = type.ArgumentTypes[0];
        foreach (var child in exp.Children)
        {
            // limit spread type to [ChildType]
            if (child is Ast.Spread spread) {
                LimitExpType(spread.Expression, this.m_ModelCore.Factory.TypeWithArguments(this.m_ModelCore.ArrayType, new Symbol[]{elementType}));
                continue;
            }
            LimitExpType(child, elementType);
        }
        exp.SemanticSymbol = m_ModelCore.Factory.Value(type);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // markup list

    private Symbol VerifyIndexExp(Ast.IndexExpression exp)
    {
        var @base = VerifyExpAsValue(exp.Base, null);
        if (@base == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var baseNonNullType = @base.StaticType.ToNonNullableType();
        if (baseNonNullType is TupleType)
        {
            var tupleType = baseNonNullType;
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
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var proxy = InheritedProxies.Find(@base.StaticType, Operator.ProxyToGetIndex);
        if (proxy == null)
        {
            VerifyError(null, 201, exp.Span.Value, new DiagnosticArguments {["t"] = @base.StaticType});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        LimitExpType(exp.Key, proxy.StaticType.FunctionRequiredParameters[0].Type);
        exp.SemanticSymbol = m_ModelCore.Factory.IndexValue(@base, proxy.StaticType.FunctionReturnType);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // index expression

    private Symbol VerifyCallExp(Ast.CallExpression exp)
    {
        var @base = VerifyExp(exp.Base, null);
        if (@base == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        Symbol r = null;
        // call expression works as:
        // - a function call.
        // - a class constructor call, equivalent to 'new' expression.
        // - an explicit enumeration conversion.
        if (@base.StaticType is FunctionType)
        {
            VerifyFunctionCall(exp.ArgumentsList, exp.Span.Value, @base.StaticType);
            r = m_ModelCore.Factory.Value(@base.StaticType.FunctionReturnType);
        }
        else if (@base.IsClassType)
        {
            var constructorDefinition = @base.InheritConstructorDefinition();
            if (constructorDefinition == null)
            {
                throw new Exception("The Object built-in must have a constructor definition");
            }
            VerifyFunctionCall(exp.ArgumentsList, exp.Span.Value, constructorDefinition.StaticType);
            r = m_ModelCore.Factory.Value(@base);
        }
        else if (@base is EnumType)
        {
            if (exp.ArgumentsList.Count() != 1)
            {
                VerifyError(null, 204, exp.Span.Value, new DiagnosticArguments {});
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
            r = VerifyExpAsValue(exp.ArgumentsList[0], @base);
            if (r == null)
            {
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
            var conversion = TypeConversions.ConvertExplicit(r, @base, false);
            if (r == null)
            {
                VerifyError(null, 205, exp.Span.Value, new DiagnosticArguments {["from"] = r.StaticType, ["to"] = @base});
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
            r = conversion;
        }
        else if (@base is Value && @base.StaticType == m_ModelCore.FunctionType)
        {
            var arrayOfAny = m_ModelCore.Factory.TypeWithArguments(m_ModelCore.ArrayType, new Symbol[]{m_ModelCore.AnyType});
            var functionTakingAny = m_ModelCore.Factory.FunctionType(null, null, new NameAndTypePair("_", arrayOfAny), m_ModelCore.AnyType);
            VerifyFunctionCall(exp.ArgumentsList, exp.Span.Value, functionTakingAny);
            r = m_ModelCore.Factory.Value(m_ModelCore.AnyType);
        }
        else if (@base is Value)
        {
            // VerifyError: non callable type
            VerifyError(null, 207, exp.Span.Value, new DiagnosticArguments {["t"] = @base.StaticType});
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

        exp.SemanticSymbol = r;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // call expression

    private Symbol VerifyThisLiteral(Ast.ThisLiteral exp)
    {
        var thisValue = m_Frame.FindActivation()?.ActivationThisOrThisAsStaticType;
        if (thisValue == null)
        {
            VerifyError(null, 208, exp.Span.Value, new DiagnosticArguments {});
        }
        exp.SemanticSymbol = thisValue;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // this literal

    private Symbol VerifyStringLiteral(Ast.StringLiteral exp, Symbol expectedType)
    {
        var enumType = expectedType is EnumType ? expectedType : null;
        if (enumType != null)
        {
            object matchingVariant = enumType.EnumGetVariantNumberByString(exp.Value);
            if (matchingVariant == null)
            {
                VerifyError(null, 164, exp.Span.Value, new DiagnosticArguments {["et"] = enumType, ["name"] = exp.Value});
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
            exp.SemanticSymbol = m_ModelCore.Factory.EnumConstantValue(matchingVariant, enumType);
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        exp.SemanticSymbol = m_ModelCore.Factory.StringConstantValue(exp.Value);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // string literal

    private Symbol VerifyRegExpLiteral(Ast.RegExpLiteral exp)
    {
        exp.SemanticSymbol = m_ModelCore.Factory.Value(m_ModelCore.RegExpType);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // regular expression

    // verifies a conditional expression.
    // attempts to convert in two different ways:
    // - either implicitly converts consequent type to alternative type.
    // - either implicitly converts alternative type to consequent type.
    // if none of the conversions succeed, throw a VerifyError.
    private Symbol VerifyConditionalExp(Ast.ConditionalExpression exp, Symbol expectedType)
    {
        VerifyExpAsValue(exp.Test);
        var ttFrames = exp.Test.GetTypeTestFrames();
        EnterFrames(ttFrames);
        var conseq = VerifyExpAsValue(exp.Consequent, expectedType);
        ExitNFrames(ttFrames.Count());
        if (conseq == null)
        {
            VerifyExpAsValue(exp.Alternative);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var alt = VerifyExpAsValue(exp.Alternative, expectedType);
        if (alt == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var c2a = TypeConversions.ConvertImplicit(conseq, alt.StaticType);
        if (c2a != null)
        {
            exp.SemanticConseqToAltConv = c2a;
            exp.SemanticSymbol = m_ModelCore.Factory.Value(alt.StaticType);
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        var a2c = TypeConversions.ConvertImplicit(alt, conseq.StaticType);
        if (a2c != null)
        {
            exp.SemanticAltToConseqConv = a2c;
            exp.SemanticSymbol = m_ModelCore.Factory.Value(conseq.StaticType);
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        VerifyError(null, 209, exp.Span.Value, new DiagnosticArguments {["c"] = conseq.StaticType, ["a"] = alt.StaticType});
        exp.SemanticSymbol = null;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // conditional expression

    private Symbol VerifyParensExp(Ast.ParensExpression exp, Symbol expectedType, bool instantiatingGeneric, bool writting, bool checkUndesiredAssign)
    {
        exp.SemanticSymbol = VerifyExp(exp.Expression, expectedType, instantiatingGeneric, writting, checkUndesiredAssign);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // parentheses expression

    private Symbol VerifyListExp(Ast.ListExpression exp, Symbol expectedType)
    {
        Symbol r = null;
        bool valid = true;
        foreach (var subExpr in exp.Expressions)
        {
            r = VerifyExp(subExpr, expectedType);
            valid = valid && r != null;
        }
        exp.SemanticSymbol = valid ? r : null;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // list expression

    private Symbol VerifyExpWithTypeArgs(Ast.ExpressionWithTypeArguments exp)
    {
        var @base = VerifyExp(exp.Base, null, true);
        if (@base == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        if (@base is Type && @base.IsGenericTypeOrMethod)
        {
            exp.SemanticSymbol = VerifyGenericTypeArguments(exp.Span.Value, @base, exp.ArgumentsList);
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        else if (@base is Type)
        {
            VerifyError(null, 133, exp.Span.Value, new DiagnosticArguments {["t"] = @base});
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        // verify generic method
        else if (@base.IsGenericTypeOrMethod)
        {
            var method = @base.Property;
            var instantiatedMethod = VerifyGenericTypeArguments(exp.Span.Value, method, exp.ArgumentsList);
            if (instantiatedMethod == null)
            {
                exp.SemanticSymbol = null;
                exp.SemanticExpResolved = true;
                return exp.SemanticSymbol;
            }
            if (@base is ReferenceValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.ReferenceValue(@base.Base, instantiatedMethod, @base.PropertyDefinedByType);
            }
            else if (@base is ReferenceValueFromType)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.ReferenceValueFromType(@base.Base, instantiatedMethod, @base.PropertyDefinedByType);
            }
            else if (@base is ReferenceValueFromNamespace)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.ReferenceValueFromNamespace(@base.Base, instantiatedMethod);
            }
            else
            {
                exp.SemanticSymbol = m_ModelCore.Factory.ReferenceValueFromFrame(@base.Base, instantiatedMethod);
            }
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        else
        {
            // VerifyError: cannot instantiate item
            ReportNotGeneric(@base, exp.Span.Value);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
    } // expression with type arguments

    private Symbol VerifyAssignmentExp(Ast.AssignmentExpression exp)
    {
        if (exp.Left is Ast.DestructuringPattern)
        {
            return VerifyDestructuringAssignmentExp(exp);
        }
        var left = VerifyExpAsValue((Ast.Expression) exp.Left, null, false, true);
        if (left == null)
        {
            VerifyExpAsValue(exp.Right, null, false, false, false);
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        Symbol right = null;

        // =
        // &&=
        // ||=
        // ^^=
        if (exp.Compound == null || exp.Compound == Operator.LogicalAnd || exp.Compound == Operator.LogicalXor || exp.Compound == Operator.LogicalOr)
        {
            right = LimitExpType(exp.Right, left.StaticType, false, false);
            exp.SemanticSymbol = right != null ? m_ModelCore.Factory.Value(left.StaticType) : null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }

        var proxy = InheritedProxies.Find(left.StaticType, exp.Compound);
        if (proxy == null)
        {
            // unsupported operator
            VerifyError(null, 178, exp.Span.Value, new DiagnosticArguments {["t"] = left.StaticType, ["op"] = exp.Compound});

            VerifyExpAsValue(exp.Right, null, false, false, false);

            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        LimitExpType(exp.Right, proxy.StaticType.FunctionRequiredParameters[1].Type, false, false);
        exp.SemanticSymbol = proxy == null ? null : m_ModelCore.Factory.Value(proxy.StaticType.FunctionReturnType);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // assignment expression

    private Symbol VerifyDestructuringAssignmentExp(Ast.AssignmentExpression exp)
    {
        var right = VerifyExpAsValue(exp.Right, null);
        if (right == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        VerifyAssignmentDestructuringPattern((Ast.DestructuringPattern) exp.Left, right.StaticType);
        exp.SemanticSymbol = m_ModelCore.Factory.Value(right.StaticType);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // assignment expression (destructuring)

    private Symbol VerifyNewExp(Ast.NewExpression exp)
    {
        var @base = VerifyExp(exp.Base);
        if (@base == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticExpResolved = true;
            return exp.SemanticSymbol;
        }
        Symbol r = null;
        if (@base.IsClassType)
        {
            var constructorDefinition = @base.InheritConstructorDefinition();
            if (constructorDefinition == null)
            {
                throw new Exception("The Object built-in must have a constructor definition");
            }
            VerifyFunctionCall(exp.ArgumentsList, exp.Span.Value, constructorDefinition.StaticType);
            r = m_ModelCore.Factory.Value(@base);
        }
        else if (@base is Type)
        {
            VerifyError(null, 211, exp.Span.Value, new DiagnosticArguments {["t"] = @base});
        }
        else
        {
            VerifyError(null, 212, exp.Base.Span.Value, new DiagnosticArguments {});
        }
        exp.SemanticSymbol = r;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // new expression

    private Symbol VerifySuperExp(Ast.SuperExpression exp)
    {
        var thisValue = m_Frame.FindActivation()?.ActivationThisOrThisAsStaticType;
        if (thisValue == null || !(thisValue is ThisValue) || !(thisValue.StaticType.IsClassType) || (thisValue.StaticType.SuperType == null))
        {
            VerifyError(null, 213, exp.Span.Value, new DiagnosticArguments {});
            thisValue = null;
        }
        exp.SemanticSymbol = thisValue != null ? m_ModelCore.Factory.Value(thisValue.StaticType.SuperType): null;
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // super expression
}