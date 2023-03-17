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
    public Symbol VerifyConstantExp
    (
        Ast.Expression exp,
        bool faillible,
        Symbol expectedType = null,
        bool instantiatingGeneric = false
    )
    {
        if (exp.SemanticConstantExpResolved)
        {
            return exp.SemanticSymbol;
        }
        // verify identifier; ensure
        // - it is not undefined.
        // - it is not an ambiguous reference.
        // - it is lexically visible.
        // - if it is a non-argumented generic type or function, throw a VerifyError.
        // - it is a compile-time value.
        if (exp is Ast.Identifier id)
        {
            var r = m_Frame.ResolveProperty(id.Name);
            if (r == null)
            {
                // VerifyError: undefined reference
                if (faillible)
                {
                    VerifyError(null, 128, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (r is AmbiguousReferenceIssue)
            {
                // VerifyError: ambiguous reference
                if (faillible)
                {
                    VerifyError(null, 129, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else
            {
                if (!r.PropertyIsVisibleTo(m_Frame))
                {
                    // VerifyError: accessing private property
                    if (faillible)
                    {
                        VerifyError(null, 130, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }
                r = r is Alias ? r.AliasToSymbol : r;
                // VerifyError: unargumented generic type or function
                if (!instantiatingGeneric && r.TypeParameters != null)
                {
                    if (faillible)
                    {
                        VerifyError(null, 132, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }

                // extend variable life
                if (r is ReferenceValueFromFrame && r.Base.FindActivation() != m_Frame.FindActivation())
                {
                    r.Base.FindActivation().AddExtendedLifeVariable(r.Property);
                }

                // use initial constant value if any.
                if ((r is ReferenceValue || r is ReferenceValueFromNamespace || r is ReferenceValueFromType) && r.Property.InitValue is ConstantValue)
                {
                    r = r.Property.InitValue;
                }

                if  (!(r is Type || r is ConstantValue || r is Namespace))
                {
                    if (faillible)
                    {
                        VerifyError(null, 151, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }

                if (id.Type != null)
                {
                    VerifyError(null, 152, exp.Span.Value, new DiagnosticArguments {});
                    VerifyTypeExp(id.Type);
                }

                // implicitly convert NaN, +Infinity and -Infinity to numeric types other
                // than Number.
                if (r is NumberConstantValue && double.IsNaN(r.NumberValue) || !double.IsFinite(r.NumberValue) && expectedType != null && expectedType.ToNonNullableType() != m_ModelCore.NumberType && m_ModelCore.IsNumericType(expectedType.ToNonNullableType()))
                {
                    var nonNullableType = expectedType.ToNonNullableType();
                    if (nonNullableType == m_ModelCore.DecimalType)
                    {
                        return m_ModelCore.Factory.DecimalConstantValue((decimal) r.NumberValue, expectedType);
                    }
                    if (nonNullableType == m_ModelCore.ByteType)
                    {
                        return m_ModelCore.Factory.ByteConstantValue(double.IsNaN(r.NumberValue) ? ((byte) 0) : r.NumberValue == double.PositiveInfinity ? byte.MaxValue: byte.MinValue, expectedType);
                    }
                    if (nonNullableType == m_ModelCore.ShortType)
                    {
                        return m_ModelCore.Factory.ShortConstantValue(double.IsNaN(r.NumberValue) ? (short) (0) : r.NumberValue == double.PositiveInfinity ? short.MaxValue : short.MinValue, expectedType);
                    }
                    if (nonNullableType == m_ModelCore.IntType)
                    {
                        return m_ModelCore.Factory.IntConstantValue(double.IsNaN(r.NumberValue) ? 0 : r.NumberValue == double.PositiveInfinity ? int.MaxValue : int.MinValue, expectedType);
                    }
                    if (nonNullableType == m_ModelCore.LongType)
                    {
                        return m_ModelCore.Factory.LongConstantValue(double.IsNaN(r.NumberValue) ? 0 : r.NumberValue == double.PositiveInfinity ? long.MaxValue : long.MinValue, expectedType);
                    }
                    if (nonNullableType == m_ModelCore.BigIntType && double.IsNaN(r.NumberValue))
                    {
                        return m_ModelCore.Factory.BigIntConstantValue(0, expectedType);
                    }
                }

                exp.SemanticSymbol = r;
                exp.SemanticConstantExpResolved = true;
                return r;
            }
        } // Identifier
        // verify member; ensure
        // - it is not undefined.
        // - it is lexically visible.
        // - if it is a non-argumented generic type or function, throw a VerifyError.
        // - it is a compile-time value.
        if (exp is Ast.MemberExpression memb && !memb.Optional)
        {
            var @base = VerifyConstantExp(memb.Base, faillible, null, false);
            if (@base == null)
            {
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            var r = @base.ResolveProperty(memb.Id.Name);
            if (r == null)
            {
                // VerifyError: undefined reference
                if (faillible)
                {
                    VerifyError(null, 128, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else
            {
                if (!r.PropertyIsVisibleTo(m_Frame))
                {
                    // VerifyError: accessing private property
                    if (faillible)
                    {
                        VerifyError(null, 130, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }
                r = r is Alias ? r.AliasToSymbol : r;
                // VerifyError: unargumented generic type or function
                if (!instantiatingGeneric && r.TypeParameters != null)
                {
                    if (faillible)
                    {
                        VerifyError(null, 132, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }

                // extend variable life
                if (r is ReferenceValueFromFrame && r.Base.FindActivation() != m_Frame.FindActivation())
                {
                    r.Base.FindActivation().AddExtendedLifeVariable(r.Property);
                }

                // use initial constant value if any.
                if ((r is ReferenceValue || r is ReferenceValueFromNamespace || r is ReferenceValueFromType) && r.Property.InitValue is ConstantValue)
                {
                    r = r.Property.InitValue;
                }

                if  (!(r is Type || r is ConstantValue || r is Namespace))
                {
                    if (faillible)
                    {
                        VerifyError(null, 151, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }

                exp.SemanticSymbol = r;
                exp.SemanticConstantExpResolved = true;
                return r;
            }
        } // MemberExpression
        else if (exp is Ast.UnaryExpression unaryExp)
        {
            return VerifyConstantUnaryExp(unaryExp, faillible, expectedType);
        } // UnaryExpression
        else if (exp is Ast.BinaryExpression binaryExp)
        {
            return VerifyConstantBinaryExp(binaryExp, faillible, expectedType);
        } // BinaryExpression
        else
        {
            if (faillible)
            {
                VerifyError(null, 150, exp.Span.Value, new DiagnosticArguments {});
            }
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
    }

    private Symbol VerifyConstantUnaryExp
    (
        Ast.UnaryExpression exp,
        bool faillible,
        Symbol expectedType = null
    )
    {
        var operand = VerifyConstantExp(exp.Operand, faillible, expectedType);
        if (operand == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }

        // logical not (!)
        if (exp.Operator == Operator.LogicalNot)
        {
            if (operand is Type || operand is Namespace)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(false);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is UndefinedConstantValue || operand is NullConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(true);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is BooleanConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(!operand.BooleanValue);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is NumberConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(double.IsNaN(operand.NumberValue) || operand.NumberValue == 0);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is DecimalConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(operand.DecimalValue == 0);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is ByteConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(operand.ByteValue == 0);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is ShortConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(operand.ShortValue == 0);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is IntConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(operand.IntValue == 0);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is LongConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(operand.LongValue == 0);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is BigIntConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(operand.BigIntValue == 0);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is StringConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(operand.StringValue.Count() == 0);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else
            {
                if (!(operand is EnumConstantValue))
                {
                    throw new Exception("Unimplemented constant logical not");
                }
                exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(EnumConstHelpers.HasZeroFlags(operand.EnumConstValue));
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
        } // logical not
        else if (exp.Operator == Operator.Positive)
        {
            if (operand is NumberConstantValue || operand is DecimalConstantValue
            ||  operand is ByteConstantValue || operand is ShortConstantValue
            ||  operand is IntConstantValue || operand is LongConstantValue
            ||  operand is BigIntConstantValue)
            {
                exp.SemanticSymbol = operand;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else
            {
                if (faillible)
                {
                    VerifyError(null, 154, exp.Span.Value, new DiagnosticArguments {});
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
        } // positive
        else if (exp.Operator == Operator.Negate)
        {
            if (operand is NumberConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.NumberConstantValue(-operand.NumberValue, operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is DecimalConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.DecimalConstantValue(-operand.DecimalValue, operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is ByteConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.ByteConstantValue((byte) (-operand.ByteValue), operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is ShortConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.ShortConstantValue((short) (-operand.ShortValue), operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is IntConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.IntConstantValue(-operand.IntValue, operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is LongConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.LongConstantValue(-operand.LongValue, operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is BigIntConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BigIntConstantValue(-operand.BigIntValue, operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else
            {
                if (faillible)
                {
                    VerifyError(null, 154, exp.Span.Value, new DiagnosticArguments {});
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
        } // negate
        else if (exp.Operator == Operator.BitwiseNot)
        {
            if (operand is NumberConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.NumberConstantValue((double) (~((int) operand.NumberValue)), operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is DecimalConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.DecimalConstantValue((decimal) (~((int) operand.DecimalValue)), operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is ByteConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.ByteConstantValue((byte) (~operand.ByteValue), operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is ShortConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.ShortConstantValue((short) (~operand.ShortValue), operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is IntConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.IntConstantValue(~operand.IntValue, operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is LongConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.LongConstantValue(~operand.LongValue, operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (operand is BigIntConstantValue)
            {
                exp.SemanticSymbol = m_ModelCore.Factory.BigIntConstantValue(~operand.BigIntValue, operand.StaticType);
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else
            {
                if (faillible)
                {
                    VerifyError(null, 154, exp.Span.Value, new DiagnosticArguments {});
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
        } // bitwise not
        else
        {
            if (faillible)
            {
                VerifyError(null, 153, exp.Span.Value, new DiagnosticArguments {["op"] = exp.Operator});
            }
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
    }

    private Symbol VerifyConstantBinaryExp
    (
        Ast.BinaryExpression exp,
        bool faillible,
        Symbol expectedType = null
    )
    {
        if (exp.Operator == Operator.In)
        {
            return VerifyConstantInBinaryExp(exp, faillible);
        }
        var left = VerifyConstantExp(exp.Left, faillible, expectedType);
        if (left == null)
        {
            VerifyConstantExp(exp.Right, faillible);
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        var right = VerifyConstantExp(exp.Right, faillible, exp.Operator == Operator.StrictEquals || exp.Operator == Operator.StrictNotEquals ? null : left.StaticType);
    }

    // verification for compile-time "in" operator.
    // works for flags enumeration only, currently.
    private Symbol VerifyConstantInBinaryExp(Ast.BinaryExpression exp, bool faillible)
    {
        var @base = VerifyConstantExp(exp.Right, faillible);
        if (@base == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        if (!(@base is EnumConstantValue && @base.StaticType.ToNonNullableType().IsFlagsEnum))
        {
            if (faillible)
            {
                VerifyError(null, 155, exp.Span.Value, new DiagnosticArguments {});
            }
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        Symbol flagsType = @base.StaticType.ToNonNullableType();
        Symbol k = LimitConstantExpType(exp.Left, flagsType, faillible);
        if (k == null || !(k is EnumConstantValue))
        {
            if (!(k is EnumConstantValue) && faillible)
            {
                VerifyError(null, 156, exp.Span.Value, new DiagnosticArguments {});
            }
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        var inc = EnumConstHelpers.Includes(flagsType.NumericType, @base.EnumConstValue, k.EnumConstValue);
        exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(inc);
        exp.SemanticConstantExpResolved = true;
        return exp.SemanticSymbol;
    }
}