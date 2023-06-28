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
    private Symbol VerifyConstantExp
    (
        Ast.Expression exp,
        bool faillible,
        Symbol expectedType = null,
        bool instantiatingGeneric = false
    )
    {
        if (exp.SemanticConstantExpResolved || exp.SemanticExpResolved)
        {
            return exp.SemanticSymbol;
        }
        if (exp is Ast.Identifier id)
        {
            return VerifyConstantLexicalReference(id, faillible, expectedType, instantiatingGeneric);
        }
        else if (exp is Ast.MemberExpression memb)
        {
            return VerifyConstantMemberExp(memb, faillible, expectedType, instantiatingGeneric);
        }
        else if (exp is Ast.UnaryExpression unaryExp)
        {
            return VerifyConstantUnaryExp(unaryExp, faillible, expectedType);
        }
        else if (exp is Ast.BinaryExpression binaryExp)
        {
            return VerifyConstantBinaryExp(binaryExp, faillible, expectedType);
        }
        else if (exp is Ast.DefaultExpression defaultExp)
        {
            return VerifyConstDefaultExp(defaultExp, faillible);
        }
        else if (exp is Ast.ObjectInitializer objInitialiser)
        {
            return VerifyConstantObjectInitializer(objInitialiser, faillible, expectedType);
        }
        else if (exp is Ast.ArrayInitializer arrInitializer)
        {
            return VerifyConstantArrayInitializer(arrInitializer, faillible, expectedType);
        }
        else if (exp is Ast.StringLiteral stringLiteral)
        {
            return VerifyConstantStringLiteral(stringLiteral, faillible, expectedType);
        }
        else if (exp is Ast.NullLiteral nullLiteral)
        {
            return VerifyConstantNullLiteral(nullLiteral, expectedType);
        }
        else if (exp is Ast.BooleanLiteral booleanLiteral)
        {
            return VerifyConstantBooleanLiteral(booleanLiteral);
        }
        else if (exp is Ast.NumericLiteral numericLiteral)
        {
            return VerifyConstantNumericLiteral(numericLiteral, expectedType);
        }
        else if (exp is Ast.ConditionalExpression condExp)
        {
            return VerifyConstantCondExp(condExp, faillible, expectedType);
        }
        else if (exp is Ast.ParensExpression parenExp)
        {
            return VerifyConstantParenExp(parenExp, faillible, expectedType, instantiatingGeneric);
        }
        else if (exp is Ast.ListExpression listExp)
        {
            return VerifyConstantListExp(listExp, faillible, expectedType);
        }
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
    } // VerifyConstantExp

    private Symbol VerifyConstantObjectInitializer
    (
        Ast.ObjectInitializer exp,
        bool faillible,
        Symbol expectedType
    )
    {
        Symbol initType = null;
        if (exp.Type != null)
        {
            initType = VerifyTypeExp(exp.Type);
            if (initType == null)
            {
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
        }
        else {
            initType = expectedType is EnumType && expectedType.IsFlagsEnum ? expectedType : null;
            if (initType == null)
            {
                if (faillible)
                {
                    VerifyError(null, 160, exp.Span.Value, new DiagnosticArguments {});
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
        }
        if (!initType.IsFlagsEnum)
        {
            if (faillible)
            {
                VerifyError(null, 161, exp.Span.Value, new DiagnosticArguments {});
            }
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        bool validated = true;
        object resultFlags = EnumConstHelpers.Zero(initType.NumericType);
        foreach (var fieldOrSpread in exp.Fields)
        {
            if (fieldOrSpread is Ast.Spread spread)
            {
                if (faillible)
                {
                    VerifyError(null, 162, spread.Span.Value, new DiagnosticArguments {});
                    VerifyConstantExp(spread.Expression, faillible);
                }
                validated = false;
                continue;
            }
            var field = (Ast.ObjectField) fieldOrSpread;
            if (!(field.Key is Ast.StringLiteral))
            {
                if (faillible)
                {
                    VerifyError(null, 163, field.Key.Span.Value, new DiagnosticArguments {});
                }
                validated = false;
                continue;
            }
            var fieldName = ((Ast.StringLiteral) field.Key).Value;
            var matchingVariant = initType.EnumGetVariantNumberByString(fieldName);
            if (matchingVariant == null)
            {
                if (faillible)
                {
                    VerifyError(null, 164, field.Key.Span.Value, new DiagnosticArguments {["et"] = initType, ["name"] = fieldName});
                }
                validated = false;
                continue;
            }
            if (field.Value == null)
            {
                if (faillible)
                {
                    VerifyError(null, 165, field.Key.Span.Value, new DiagnosticArguments {});
                }
                validated = false;
                continue;
            }
            var v = LimitConstantExpType(field.Value, m_ModelCore.BooleanType, faillible);
            if (v == null)
            {
                validated = false;
                continue;
            }
            if (!(v is BooleanConstantValue))
            {
                throw new Exception("Internal verify error");
            }
            if (v.BooleanValue)
            {
                resultFlags = EnumConstHelpers.IncludeFlags(resultFlags, matchingVariant);
            }
        }
        exp.SemanticSymbol = validated ? m_ModelCore.Factory.EnumConstantValue(resultFlags, initType) : null;
        exp.SemanticConstantExpResolved = true;
        return exp.SemanticSymbol;
    } // object initializer

    private Symbol VerifyConstantArrayInitializer
    (
        Ast.ArrayInitializer exp,
        bool faillible,
        Symbol expectedType
    )
    {
        Symbol initType = null;
        if (exp.Type != null)
        {
            initType = VerifyTypeExp(exp.Type);
            if (initType == null)
            {
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
        }
        else {
            initType = expectedType is EnumType && expectedType.IsFlagsEnum ? expectedType : null;
            if (initType == null)
            {
                if (faillible)
                {
                    VerifyError(null, 160, exp.Span.Value, new DiagnosticArguments {});
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
        }
        if (!initType.IsFlagsEnum)
        {
            if (faillible)
            {
                VerifyError(null, 161, exp.Span.Value, new DiagnosticArguments {});
            }
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        bool validated = true;
        object resultFlags = EnumConstHelpers.Zero(initType.NumericType);
        foreach (var holeOrSpreadOrItem in exp.Items)
        {
            if (holeOrSpreadOrItem == null)
            {
                continue;
            }
            if (holeOrSpreadOrItem is Ast.Spread spread)
            {
                if (faillible)
                {
                    VerifyError(null, 162, spread.Span.Value, new DiagnosticArguments {});
                }
                validated = false;
                continue;
            }
            if (!(holeOrSpreadOrItem is Ast.StringLiteral))
            {
                if (faillible)
                {
                    VerifyError(null, 166, holeOrSpreadOrItem.Span.Value, new DiagnosticArguments {});
                }
                validated = false;
                continue;
            }
            var variantName = ((Ast.StringLiteral) holeOrSpreadOrItem).Value;
            var matchingVariant = initType.EnumGetVariantNumberByString(variantName);
            if (matchingVariant == null)
            {
                if (faillible)
                {
                    VerifyError(null, 164, holeOrSpreadOrItem.Span.Value, new DiagnosticArguments {["et"] = initType, ["name"] = variantName});
                }
                validated = false;
                continue;
            }
            resultFlags = EnumConstHelpers.IncludeFlags(resultFlags, matchingVariant);
        }
        exp.SemanticSymbol = validated ? m_ModelCore.Factory.EnumConstantValue(resultFlags, initType) : null;
        exp.SemanticConstantExpResolved = true;
        return exp.SemanticSymbol;
    } // array initialiser

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
    } // unary expression

    private Symbol VerifyConstantBinaryExp
    (
        Ast.BinaryExpression exp,
        bool faillible,
        Symbol expectedType = null
    )
    {
        if (exp.Operator == Operator.In)
        {
            return In_VerifyConstantBinaryExp(exp, faillible);
        }
        var left = VerifyConstantExp(exp.Left, faillible, expectedType);
        if (left == null)
        {
            if (faillible)
            {
                VerifyConstantExp(exp.Right, faillible);
            }
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        var right = VerifyConstantExp(exp.Right, faillible, exp.Operator == Operator.StrictEquals || exp.Operator == Operator.StrictNotEquals ? null : left.StaticType);
        if (right == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        if (!(left is ConstantValue && right is ConstantValue))
        {
            if (faillible)
            {
                VerifyError(null, 157, exp.Span.Value, new DiagnosticArguments {});
            }
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        if (left is UndefinedConstantValue && right is UndefinedConstantValue)
        {
            if (exp.Operator == Operator.LogicalAnd)
            {
                return m_ModelCore.Factory.UndefinedConstantValue(expectedType);
            }
            else if (exp.Operator == Operator.LogicalXor)
            {
                return m_ModelCore.Factory.UndefinedConstantValue(expectedType);
            }
            else if (exp.Operator == Operator.LogicalOr)
            {
                return m_ModelCore.Factory.UndefinedConstantValue(expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(true);
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(false);
            }
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
        } // UndefinedConstantValue
        else if (left is NullConstantValue && right is NullConstantValue)
        {
            if (exp.Operator == Operator.LogicalAnd)
            {
                return m_ModelCore.Factory.NullConstantValue(expectedType);
            }
            else if (exp.Operator == Operator.LogicalXor)
            {
                return m_ModelCore.Factory.NullConstantValue(expectedType);
            }
            else if (exp.Operator == Operator.LogicalOr)
            {
                return m_ModelCore.Factory.NullConstantValue(expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(true);
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(false);
            }
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
        } // NullConstantValue
        else if (left is StringConstantValue && right is StringConstantValue)
        {
            if (exp.Operator == Operator.Add)
            {
                return m_ModelCore.Factory.StringConstantValue(left.StringValue + right.StringValue, expectedType);
            }
            else if (exp.Operator == Operator.LogicalAnd)
            {
                return m_ModelCore.Factory.StringConstantValue(left.StringValue == "" ? left.StringValue : right.StringValue, expectedType);
            }
            else if (exp.Operator == Operator.LogicalOr)
            {
                return m_ModelCore.Factory.StringConstantValue(left.StringValue == "" ? right.StringValue : left.StringValue, expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.StringValue == right.StringValue);
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.StringValue != right.StringValue);
            }
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
        } // StringConstantValue
        else if (left is BooleanConstantValue && right is BooleanConstantValue)
        {
            if (exp.Operator == Operator.LogicalAnd)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.BooleanValue && right.BooleanValue, expectedType);
            }
            else if (exp.Operator == Operator.LogicalXor)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.BooleanValue ^ right.BooleanValue, expectedType);
            }
            else if (exp.Operator == Operator.LogicalOr)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.BooleanValue || right.BooleanValue, expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.BooleanValue == right.BooleanValue);
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.BooleanValue != right.BooleanValue);
            }
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
        } // BooleanConstantValue
        else if (left is NumberConstantValue && right is NumberConstantValue)
        {
            if (exp.Operator == Operator.Add)
            {
                return m_ModelCore.Factory.NumberConstantValue(left.NumberValue + right.NumberValue, expectedType);
            }
            else if (exp.Operator == Operator.Subtract)
            {
                return m_ModelCore.Factory.NumberConstantValue(left.NumberValue - right.NumberValue, expectedType);
            }
            else if (exp.Operator == Operator.Multiply)
            {
                return m_ModelCore.Factory.NumberConstantValue(left.NumberValue * right.NumberValue, expectedType);
            }
            else if (exp.Operator == Operator.Divide)
            {
                return m_ModelCore.Factory.NumberConstantValue(left.NumberValue / right.NumberValue, expectedType);
            }
            else if (exp.Operator == Operator.Remainder)
            {
                return m_ModelCore.Factory.NumberConstantValue(left.NumberValue % right.NumberValue, expectedType);
            }
            else if (exp.Operator == Operator.Pow)
            {
                return m_ModelCore.Factory.NumberConstantValue(Math.Pow(left.NumberValue, right.NumberValue), expectedType);
            }
            else if (exp.Operator == Operator.BitwiseAnd)
            {
                return m_ModelCore.Factory.NumberConstantValue((double) (((int) left.NumberValue) & ((int) right.NumberValue)), expectedType);
            }
            else if (exp.Operator == Operator.BitwiseXor)
            {
                return m_ModelCore.Factory.NumberConstantValue((double) (((int) left.NumberValue) ^ ((int) right.NumberValue)), expectedType);
            }
            else if (exp.Operator == Operator.BitwiseOr)
            {
                return m_ModelCore.Factory.NumberConstantValue((double) (((int) left.NumberValue) | ((int) right.NumberValue)), expectedType);
            }
            else if (exp.Operator == Operator.LeftShift)
            {
                return m_ModelCore.Factory.NumberConstantValue((double) (((int) left.NumberValue) << ((int) right.NumberValue)), expectedType);
            }
            else if (exp.Operator == Operator.RightShift)
            {
                return m_ModelCore.Factory.NumberConstantValue((double) (((int) left.NumberValue) >> ((int) right.NumberValue)), expectedType);
            }
            else if (exp.Operator == Operator.UnsignedRightShift)
            {
                return m_ModelCore.Factory.NumberConstantValue((double) (((int) left.NumberValue) >>> ((int) right.NumberValue)), expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.NumberValue == right.NumberValue);
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.NumberValue != right.NumberValue);
            }
            else if (exp.Operator == Operator.Lt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.NumberValue < right.NumberValue);
            }
            else if (exp.Operator == Operator.Gt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.NumberValue > right.NumberValue);
            }
            else if (exp.Operator == Operator.Le)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.NumberValue <= right.NumberValue);
            }
            else if (exp.Operator == Operator.Ge)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.NumberValue >= right.NumberValue);
            }
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
        } // NumberConstantValue
        else if (left is DecimalConstantValue && right is DecimalConstantValue)
        {
            if (exp.Operator == Operator.Add)
            {
                return m_ModelCore.Factory.DecimalConstantValue(left.DecimalValue + right.DecimalValue, expectedType);
            }
            else if (exp.Operator == Operator.Subtract)
            {
                return m_ModelCore.Factory.DecimalConstantValue(left.DecimalValue - right.DecimalValue, expectedType);
            }
            else if (exp.Operator == Operator.Multiply)
            {
                return m_ModelCore.Factory.DecimalConstantValue(left.DecimalValue * right.DecimalValue, expectedType);
            }
            else if (exp.Operator == Operator.Divide)
            {
                return m_ModelCore.Factory.DecimalConstantValue(left.DecimalValue / right.DecimalValue, expectedType);
            }
            else if (exp.Operator == Operator.Remainder)
            {
                return m_ModelCore.Factory.DecimalConstantValue(left.DecimalValue % right.DecimalValue, expectedType);
            }
            else if (exp.Operator == Operator.BitwiseAnd)
            {
                return m_ModelCore.Factory.DecimalConstantValue((decimal) (((int) left.DecimalValue) & ((int) right.DecimalValue)), expectedType);
            }
            else if (exp.Operator == Operator.BitwiseXor)
            {
                return m_ModelCore.Factory.DecimalConstantValue((decimal) (((int) left.DecimalValue) ^ ((int) right.DecimalValue)), expectedType);
            }
            else if (exp.Operator == Operator.BitwiseOr)
            {
                return m_ModelCore.Factory.DecimalConstantValue((decimal) (((int) left.DecimalValue) | ((int) right.DecimalValue)), expectedType);
            }
            else if (exp.Operator == Operator.LeftShift)
            {
                return m_ModelCore.Factory.DecimalConstantValue((decimal) (((int) left.DecimalValue) << ((int) right.DecimalValue)), expectedType);
            }
            else if (exp.Operator == Operator.RightShift)
            {
                return m_ModelCore.Factory.DecimalConstantValue((decimal) (((int) left.DecimalValue) >> ((int) right.DecimalValue)), expectedType);
            }
            else if (exp.Operator == Operator.UnsignedRightShift)
            {
                return m_ModelCore.Factory.DecimalConstantValue((decimal) (((int) left.DecimalValue) >>> ((int) right.DecimalValue)), expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.DecimalValue == right.DecimalValue);
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.DecimalValue != right.DecimalValue);
            }
            else if (exp.Operator == Operator.Lt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.DecimalValue < right.DecimalValue);
            }
            else if (exp.Operator == Operator.Gt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.DecimalValue > right.DecimalValue);
            }
            else if (exp.Operator == Operator.Le)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.DecimalValue <= right.DecimalValue);
            }
            else if (exp.Operator == Operator.Ge)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.DecimalValue >= right.DecimalValue);
            }
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
        } // DecimalConstantValue
        else if (left is ByteConstantValue && right is ByteConstantValue)
        {
            if (exp.Operator == Operator.Add)
            {
                return m_ModelCore.Factory.ByteConstantValue((byte) (left.ByteValue + right.ByteValue), expectedType);
            }
            else if (exp.Operator == Operator.Subtract)
            {
                return m_ModelCore.Factory.ByteConstantValue((byte) (left.ByteValue - right.ByteValue), expectedType);
            }
            else if (exp.Operator == Operator.Multiply)
            {
                return m_ModelCore.Factory.ByteConstantValue((byte) (left.ByteValue * right.ByteValue), expectedType);
            }
            else if (exp.Operator == Operator.Divide)
            {
                return m_ModelCore.Factory.ByteConstantValue((byte) (left.ByteValue / right.ByteValue), expectedType);
            }
            else if (exp.Operator == Operator.Remainder)
            {
                return m_ModelCore.Factory.ByteConstantValue((byte) (left.ByteValue % right.ByteValue), expectedType);
            }
            else if (exp.Operator == Operator.BitwiseAnd)
            {
                return m_ModelCore.Factory.ByteConstantValue((byte) (left.ByteValue & right.ByteValue), expectedType);
            }
            else if (exp.Operator == Operator.BitwiseXor)
            {
                return m_ModelCore.Factory.ByteConstantValue((byte) (left.ByteValue ^ right.ByteValue), expectedType);
            }
            else if (exp.Operator == Operator.BitwiseOr)
            {
                return m_ModelCore.Factory.ByteConstantValue((byte) (left.ByteValue | right.ByteValue), expectedType);
            }
            else if (exp.Operator == Operator.LeftShift)
            {
                return m_ModelCore.Factory.ByteConstantValue((byte) (left.ByteValue << right.ByteValue), expectedType);
            }
            else if (exp.Operator == Operator.RightShift)
            {
                return m_ModelCore.Factory.ByteConstantValue((byte) (left.ByteValue >> right.ByteValue), expectedType);
            }
            else if (exp.Operator == Operator.UnsignedRightShift)
            {
                return m_ModelCore.Factory.ByteConstantValue((byte) (left.ByteValue >>> right.ByteValue), expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ByteValue == right.ByteValue);
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ByteValue != right.ByteValue);
            }
            else if (exp.Operator == Operator.Lt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ByteValue < right.ByteValue);
            }
            else if (exp.Operator == Operator.Gt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ByteValue > right.ByteValue);
            }
            else if (exp.Operator == Operator.Le)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ByteValue <= right.ByteValue);
            }
            else if (exp.Operator == Operator.Ge)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ByteValue >= right.ByteValue);
            }
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
        } // ByteConstantValue
        else if (left is ShortConstantValue && right is ShortConstantValue)
        {
            if (exp.Operator == Operator.Add)
            {
                return m_ModelCore.Factory.ShortConstantValue((short) (left.ShortValue + right.ShortValue), expectedType);
            }
            else if (exp.Operator == Operator.Subtract)
            {
                return m_ModelCore.Factory.ShortConstantValue((short) (left.ShortValue - right.ShortValue), expectedType);
            }
            else if (exp.Operator == Operator.Multiply)
            {
                return m_ModelCore.Factory.ShortConstantValue((short) (left.ShortValue * right.ShortValue), expectedType);
            }
            else if (exp.Operator == Operator.Divide)
            {
                return m_ModelCore.Factory.ShortConstantValue((short) (left.ShortValue / right.ShortValue), expectedType);
            }
            else if (exp.Operator == Operator.Remainder)
            {
                return m_ModelCore.Factory.ShortConstantValue((short) (left.ShortValue % right.ShortValue), expectedType);
            }
            else if (exp.Operator == Operator.BitwiseAnd)
            {
                return m_ModelCore.Factory.ShortConstantValue((short) (left.ShortValue & right.ShortValue), expectedType);
            }
            else if (exp.Operator == Operator.BitwiseXor)
            {
                return m_ModelCore.Factory.ShortConstantValue((short) (left.ShortValue ^ right.ShortValue), expectedType);
            }
            else if (exp.Operator == Operator.BitwiseOr)
            {
                return m_ModelCore.Factory.ShortConstantValue((short) (left.ShortValue | right.ShortValue), expectedType);
            }
            else if (exp.Operator == Operator.LeftShift)
            {
                return m_ModelCore.Factory.ShortConstantValue((short) (left.ShortValue << right.ShortValue), expectedType);
            }
            else if (exp.Operator == Operator.RightShift)
            {
                return m_ModelCore.Factory.ShortConstantValue((short) (left.ShortValue >> right.ShortValue), expectedType);
            }
            else if (exp.Operator == Operator.UnsignedRightShift)
            {
                return m_ModelCore.Factory.ShortConstantValue((short) (left.ShortValue >>> right.ShortValue), expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ShortValue == right.ShortValue);
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ShortValue != right.ShortValue);
            }
            else if (exp.Operator == Operator.Lt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ShortValue < right.ShortValue);
            }
            else if (exp.Operator == Operator.Gt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ShortValue > right.ShortValue);
            }
            else if (exp.Operator == Operator.Le)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ShortValue <= right.ShortValue);
            }
            else if (exp.Operator == Operator.Ge)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.ShortValue >= right.ShortValue);
            }
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
        } // ShortConstantValue
        else if (left is IntConstantValue && right is IntConstantValue)
        {
            if (exp.Operator == Operator.Add)
            {
                return m_ModelCore.Factory.IntConstantValue(left.IntValue + right.IntValue, expectedType);
            }
            else if (exp.Operator == Operator.Subtract)
            {
                return m_ModelCore.Factory.IntConstantValue(left.IntValue - right.IntValue, expectedType);
            }
            else if (exp.Operator == Operator.Multiply)
            {
                return m_ModelCore.Factory.IntConstantValue(left.IntValue * right.IntValue, expectedType);
            }
            else if (exp.Operator == Operator.Divide)
            {
                return m_ModelCore.Factory.IntConstantValue(left.IntValue / right.IntValue, expectedType);
            }
            else if (exp.Operator == Operator.Remainder)
            {
                return m_ModelCore.Factory.IntConstantValue(left.IntValue % right.IntValue, expectedType);
            }
            else if (exp.Operator == Operator.BitwiseAnd)
            {
                return m_ModelCore.Factory.IntConstantValue(left.IntValue & right.IntValue, expectedType);
            }
            else if (exp.Operator == Operator.BitwiseXor)
            {
                return m_ModelCore.Factory.IntConstantValue(left.IntValue ^ right.IntValue, expectedType);
            }
            else if (exp.Operator == Operator.BitwiseOr)
            {
                return m_ModelCore.Factory.IntConstantValue(left.IntValue | right.IntValue, expectedType);
            }
            else if (exp.Operator == Operator.LeftShift)
            {
                return m_ModelCore.Factory.IntConstantValue(left.IntValue << right.IntValue, expectedType);
            }
            else if (exp.Operator == Operator.RightShift)
            {
                return m_ModelCore.Factory.IntConstantValue(left.IntValue >> right.IntValue, expectedType);
            }
            else if (exp.Operator == Operator.UnsignedRightShift)
            {
                return m_ModelCore.Factory.IntConstantValue(left.IntValue >>> right.IntValue, expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.IntValue == right.IntValue);
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.IntValue != right.IntValue);
            }
            else if (exp.Operator == Operator.Lt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.IntValue < right.IntValue);
            }
            else if (exp.Operator == Operator.Gt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.IntValue > right.IntValue);
            }
            else if (exp.Operator == Operator.Le)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.IntValue <= right.IntValue);
            }
            else if (exp.Operator == Operator.Ge)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.IntValue >= right.IntValue);
            }
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
        } // IntConstantValue
        else if (left is LongConstantValue && right is LongConstantValue)
        {
            if (exp.Operator == Operator.Add)
            {
                return m_ModelCore.Factory.LongConstantValue(left.LongValue + right.LongValue, expectedType);
            }
            else if (exp.Operator == Operator.Subtract)
            {
                return m_ModelCore.Factory.LongConstantValue(left.LongValue - right.LongValue, expectedType);
            }
            else if (exp.Operator == Operator.Multiply)
            {
                return m_ModelCore.Factory.LongConstantValue(left.LongValue * right.LongValue, expectedType);
            }
            else if (exp.Operator == Operator.Divide)
            {
                return m_ModelCore.Factory.LongConstantValue(left.LongValue / right.LongValue, expectedType);
            }
            else if (exp.Operator == Operator.Remainder)
            {
                return m_ModelCore.Factory.LongConstantValue(left.LongValue % right.LongValue, expectedType);
            }
            else if (exp.Operator == Operator.BitwiseAnd)
            {
                return m_ModelCore.Factory.LongConstantValue(left.LongValue & right.LongValue, expectedType);
            }
            else if (exp.Operator == Operator.BitwiseXor)
            {
                return m_ModelCore.Factory.LongConstantValue(left.LongValue ^ right.LongValue, expectedType);
            }
            else if (exp.Operator == Operator.BitwiseOr)
            {
                return m_ModelCore.Factory.LongConstantValue(left.LongValue | right.LongValue, expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.LongValue == right.LongValue);
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.LongValue != right.LongValue);
            }
            else if (exp.Operator == Operator.Lt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.LongValue < right.LongValue);
            }
            else if (exp.Operator == Operator.Gt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.LongValue > right.LongValue);
            }
            else if (exp.Operator == Operator.Le)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.LongValue <= right.LongValue);
            }
            else if (exp.Operator == Operator.Ge)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.LongValue >= right.LongValue);
            }
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
        } // LongConstantValue
        else if (left is BigIntConstantValue && right is BigIntConstantValue)
        {
            if (exp.Operator == Operator.Add)
            {
                return m_ModelCore.Factory.BigIntConstantValue(left.BigIntValue + right.BigIntValue, expectedType);
            }
            else if (exp.Operator == Operator.Subtract)
            {
                return m_ModelCore.Factory.BigIntConstantValue(left.BigIntValue - right.BigIntValue, expectedType);
            }
            else if (exp.Operator == Operator.Multiply)
            {
                return m_ModelCore.Factory.BigIntConstantValue(left.BigIntValue * right.BigIntValue, expectedType);
            }
            else if (exp.Operator == Operator.Divide)
            {
                return m_ModelCore.Factory.BigIntConstantValue(left.BigIntValue / right.BigIntValue, expectedType);
            }
            else if (exp.Operator == Operator.Remainder)
            {
                return m_ModelCore.Factory.BigIntConstantValue(left.BigIntValue % right.BigIntValue, expectedType);
            }
            else if (exp.Operator == Operator.BitwiseAnd)
            {
                return m_ModelCore.Factory.BigIntConstantValue(left.BigIntValue & right.BigIntValue, expectedType);
            }
            else if (exp.Operator == Operator.BitwiseXor)
            {
                return m_ModelCore.Factory.BigIntConstantValue(left.BigIntValue ^ right.BigIntValue, expectedType);
            }
            else if (exp.Operator == Operator.BitwiseOr)
            {
                return m_ModelCore.Factory.BigIntConstantValue(left.BigIntValue | right.BigIntValue, expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.BigIntValue == right.BigIntValue);
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.BigIntValue != right.BigIntValue);
            }
            else if (exp.Operator == Operator.Lt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.BigIntValue < right.BigIntValue);
            }
            else if (exp.Operator == Operator.Gt)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.BigIntValue > right.BigIntValue);
            }
            else if (exp.Operator == Operator.Le)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.BigIntValue <= right.BigIntValue);
            }
            else if (exp.Operator == Operator.Ge)
            {
                return m_ModelCore.Factory.BooleanConstantValue(left.BigIntValue >= right.BigIntValue);
            }
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
        } // BigIntConstantValue
        else if (left is EnumConstantValue && right is EnumConstantValue && left.StaticType == right.StaticType && left.StaticType.IsFlagsEnum)
        {
            if (exp.Operator == Operator.Add)
            {
                return m_ModelCore.Factory.EnumConstantValue(EnumConstHelpers.IncludeFlags(left.EnumConstValue, right.EnumConstValue), expectedType);
            }
            else if (exp.Operator == Operator.Equals || exp.Operator == Operator.StrictEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(EnumConstHelpers.ValuesEquals(left.EnumConstValue, right.EnumConstValue));
            }
            else if (exp.Operator == Operator.NotEquals || exp.Operator == Operator.StrictNotEquals)
            {
                return m_ModelCore.Factory.BooleanConstantValue(!EnumConstHelpers.ValuesEquals(left.EnumConstValue, right.EnumConstValue));
            }
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
        } // EnumConstantValue
        else
        {
            if (faillible)
            {
                VerifyError(null, 158, exp.Span.Value, new DiagnosticArguments {});
            }
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
    }  // binary expression

    // verification for compile-time "in" operator.
    // works for flags enumeration only, currently.
    private Symbol In_VerifyConstantBinaryExp(Ast.BinaryExpression exp, bool faillible)
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
    } // binary expression ("in")

    // implicitly converts NaN, +Infinity and -Infinity to numeric types other
    // than Number.
    private Symbol ImplicitNaNOrInfToOtherNumericType(Symbol r, Symbol expectedType)
    {
        if (r is NumberConstantValue && double.IsNaN(r.NumberValue) || !double.IsFinite(r.NumberValue) && expectedType != null && expectedType.ToNonNullableType() != m_ModelCore.NumberType && m_ModelCore.IsNumericType(expectedType.ToNonNullableType()))
        {
            var nonNullableType = expectedType.ToNonNullableType();
            if (nonNullableType == m_ModelCore.NumberType)
            {
                return m_ModelCore.Factory.NumberConstantValue(r.NumberValue, expectedType);
            }
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
        return r;
    } // ImplicitNaNOrInfToOtherNumericType

    // verifies lexical reference; ensure
    // - it is not undefined.
    // - it is not an ambiguous reference.
    // - it is lexically visible.
    // - if it is a non-argumented generic type or function, throw a VerifyError.
    // - it is a compile-time value.
    private Symbol VerifyConstantLexicalReference
    (
        Ast.Identifier id,
        bool faillible,
        Symbol expectedType,
        bool instantiatingGeneric
    )
    {
        var exp = id;
        var r = m_Frame.ResolveProperty(id.Name);
        if (r == null)
        {
            // VerifyError: undefined reference
            if (faillible)
            {
                ReportNameNotFound(id.Name, exp.Span.Value, null);
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
            r = r?.EscapeAlias();
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
                r.Base.FindActivation()?.AddExtendedLifeVariable(r.Property);
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

            if (r is Value && r.StaticType == null)
            {
                VerifyError(null, 199, exp.Span.Value, new DiagnosticArguments {});
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return r;
            }

            r = ImplicitNaNOrInfToOtherNumericType(r, expectedType);

            exp.SemanticSymbol = r;
            exp.SemanticConstantExpResolved = true;
            return r;
        }
    } // lexical reference

    // verifies member; ensure
    // - it is not undefined.
    // - it is lexically visible.
    // - if it is a non-argumented generic type or function, throw a VerifyError.
    // - it is a compile-time value.
    private Symbol VerifyConstantMemberExp
    (
        Ast.MemberExpression memb,
        bool faillible,
        Symbol expectedType = null,
        bool instantiatingGeneric = false
    )
    {
        Ast.Expression exp = memb;
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
                ReportNameNotFound(memb.Id.Name, memb.Id.Span.Value, @base);
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
            r = r?.EscapeAlias();
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
                r.Base.FindActivation()?.AddExtendedLifeVariable(r.Property);
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

            r = ImplicitNaNOrInfToOtherNumericType(r, expectedType);

            exp.SemanticSymbol = r;
            exp.SemanticConstantExpResolved = true;
            return r;
        }
    } // member expression

    private Symbol VerifyConstDefaultExp(Ast.DefaultExpression defaultExp, bool faillible)
    {
        var exp = defaultExp;
        var t = VerifyTypeExp(defaultExp.Type);
        if (t == null)
        {
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        var defaultValue = t.DefaultValue;
        if (defaultValue == null && faillible)
        {
            VerifyError(null, 159, exp.Span.Value, new DiagnosticArguments {["t"] = t});
        }
        exp.SemanticSymbol = defaultValue;
        exp.SemanticConstantExpResolved = true;
        return exp.SemanticSymbol;
    } // default expression

    private Symbol VerifyConstantStringLiteral
    (
        Ast.StringLiteral exp,
        bool faillible,
        Symbol expectedType
    )
    {
        var enumType = expectedType is EnumType ? expectedType : null;
        if (enumType != null)
        {
            object matchingVariant = enumType.EnumGetVariantNumberByString(exp.Value);
            if (matchingVariant == null)
            {
                if (faillible)
                {
                    VerifyError(null, 164, exp.Span.Value, new DiagnosticArguments {["et"] = enumType, ["name"] = exp.Value});
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            exp.SemanticSymbol = m_ModelCore.Factory.EnumConstantValue(matchingVariant, enumType);
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        exp.SemanticSymbol = m_ModelCore.Factory.StringConstantValue(exp.Value);
        exp.SemanticConstantExpResolved = true;
        return exp.SemanticSymbol;
    } // string literal

    private Symbol VerifyConstantNullLiteral(Ast.NullLiteral exp, Symbol expectedType)
    {
        exp.SemanticSymbol = m_ModelCore.Factory.NullConstantValue(expectedType != null && expectedType.IncludesNull ? expectedType : m_ModelCore.NullType);
        exp.SemanticConstantExpResolved = true;
        return exp.SemanticSymbol;
    } // null literal

    private Symbol VerifyConstantBooleanLiteral(Ast.BooleanLiteral exp)
    {
        exp.SemanticSymbol = m_ModelCore.Factory.BooleanConstantValue(exp.Value);
        exp.SemanticConstantExpResolved = true;
        return exp.SemanticSymbol;
    } // boolean literal

    private Symbol VerifyConstantNumericLiteral(Ast.NumericLiteral exp, Symbol expectedType)
    {
        var r = m_ModelCore.Factory.NumberConstantValue(exp.Value, m_ModelCore.NumberType);
        r = ImplicitNaNOrInfToOtherNumericType(r, expectedType);

        // adapt to expected type
        if (expectedType != null && r.StaticType != expectedType && m_ModelCore.IsNumericType(expectedType.ToNonNullableType()))
        {
            var nonNullableType = expectedType.ToNonNullableType();

            if (nonNullableType == m_ModelCore.NumberType)
            {
                r = m_ModelCore.Factory.NumberConstantValue(exp.Value, expectedType);
            }
            else if (nonNullableType == m_ModelCore.DecimalType)
            {
                r = m_ModelCore.Factory.DecimalConstantValue((decimal) exp.Value, expectedType);
            }
            else if (nonNullableType == m_ModelCore.ByteType)
            {
                r = m_ModelCore.Factory.ByteConstantValue((byte) exp.Value, expectedType);
            }
            else if (nonNullableType == m_ModelCore.ShortType)
            {
                r = m_ModelCore.Factory.ShortConstantValue((short) exp.Value, expectedType);
            }
            else if (nonNullableType == m_ModelCore.IntType)
            {
                r = m_ModelCore.Factory.IntConstantValue((int) exp.Value, expectedType);
            }
            else if (nonNullableType == m_ModelCore.LongType)
            {
                r = m_ModelCore.Factory.LongConstantValue((long) exp.Value, expectedType);
            }
            else if (nonNullableType == m_ModelCore.BigIntType)
            {
                r = m_ModelCore.Factory.BigIntConstantValue((System.Numerics.BigInteger) exp.Value, expectedType);
            }
        }

        exp.SemanticSymbol = r;
        exp.SemanticConstantExpResolved = true;
        return exp.SemanticSymbol;
    } // numeric literal

    private Symbol VerifyConstantCondExp(Ast.ConditionalExpression exp, bool faillible, Symbol expectedType)
    {
        // for non-constant conditionals, the test is not limited to a Boolean.
        var test = LimitConstantExpType(exp.Test, m_ModelCore.BooleanType, faillible);
        if (test == null)
        {
            if (faillible)
            {
                VerifyConstantExp(exp.Consequent, faillible, expectedType);
                VerifyConstantExp(exp.Alternative, faillible, expectedType);
            }
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
        if (!(test is BooleanConstantValue))
        {
            throw new Exception("Internal verify error");
        }
        var consequent = VerifyConstantExp(exp.Consequent, faillible, expectedType);
        var alternative = VerifyConstantExp(exp.Alternative, faillible, expectedType);

        exp.SemanticSymbol = test.BooleanValue ? consequent : alternative;
        exp.SemanticConstantExpResolved = true;
        return exp.SemanticSymbol;
    } // conditional expression

    private Symbol VerifyConstantParenExp(Ast.ParensExpression exp, bool faillible, Symbol expectedType, bool instantiatingGeneric)
    {
        exp.SemanticSymbol = VerifyConstantExp(exp.Expression, faillible, expectedType, instantiatingGeneric);
        exp.SemanticConstantExpResolved = true;
        return exp.SemanticSymbol;
    } // parentheses expression

    private Symbol VerifyConstantListExp(Ast.ListExpression exp, bool faillible, Symbol expectedType)
    {
        Symbol r = null;
        bool valid = true;
        foreach (var subExpr in exp.Expressions)
        {
            r = VerifyConstantExp(subExpr, faillible, expectedType);
            valid = valid && r != null;
        }
        exp.SemanticSymbol = valid ? r : null;
        exp.SemanticConstantExpResolved = true;
        return exp.SemanticSymbol;
    } // list expression

    private Symbol LimitConstantExpType
    (
        Ast.Expression exp,
        Symbol expectedType,
        bool faillible = true
    )
    {
        if (exp.SemanticConstantExpResolved)
        {
            return exp.SemanticSymbol;
        }
        var r = VerifyConstantExpAsValue(exp, faillible, expectedType);
        if (r == null)
        {
            return null;
        }
        if (r.StaticType == expectedType)
        {
            return r;
        }
        var conversion = TypeConversions.ConvertConstant(r, expectedType);
        if (conversion == null && faillible)
        {
            VerifyError(null, 168, exp.Span.Value, new DiagnosticArguments {["expected"] = expectedType, ["got"] = r.StaticType});
        }
        exp.SemanticSymbol = conversion;
        return exp.SemanticSymbol;
    } // LimitConstantExpType

    private Symbol LimitStrictConstantExpType
    (
        Ast.Expression exp,
        Symbol expectedType,
        bool faillible = true
    )
    {
        if (exp.SemanticConstantExpResolved)
        {
            return exp.SemanticSymbol;
        }
        var r = VerifyConstantExpAsValue(exp, faillible, null);
        if (r == null)
        {
            return null;
        }
        if (r.StaticType == expectedType)
        {
            return r;
        }
        if (faillible)
        {
            VerifyError(null, 168, exp.Span.Value, new DiagnosticArguments {["expected"] = expectedType, ["got"] = r.StaticType});
        }
        exp.SemanticSymbol = null;
        return exp.SemanticSymbol;
    } // LimitStrictConstantExpType

    private Symbol VerifyConstantExpAsValue
    (
        Ast.Expression exp,
        bool faillible,
        Symbol expectedType = null
    )
    {
        if (exp.SemanticConstantExpResolved || exp.SemanticExpResolved)
        {
            return exp.SemanticSymbol;
        }
        var r = VerifyConstantExp(exp, faillible, expectedType);
        if (r == null)
        {
            return null;
        }
        if (!(r is Value))
        {
            if (faillible)
            {
                VerifyError(null, 167, exp.Span.Value, new DiagnosticArguments {});
            }
            exp.SemanticSymbol = null;
            return null;
        }
        return r;
    } // VerifyConstantExpAsValue
}