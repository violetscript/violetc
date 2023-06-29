namespace VioletScript.Parser.Semantic.Logic;

using System.Collections.Generic;
using System.Numerics;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Model;

public enum ConversionFromTo {
    ToUserImplicit,
    ToNumTypeWithWiderRange,

    /// <summary>
    /// Conversion from non-union type to union type.
    /// NOTE that the conversion base may also be "another"
    /// implicit conversion.
    /// </summary>
    NonUnionToCompatUnion,

    UnionToCompatUnion,
    FromRecordToEqvRecord,
    FromAny,
    ToAny,
    ToCovariantType,
    ToUserExplicit,
    ToUnionMember,
    ToContravariantType,
    ToCovariantArray,
    ToContravariantArray,
    BetweenNumericTypes,
    FromStringToEnum,
    FromNumberToEnum,
}

public static class TypeConversions {
    public static Symbol ConvertConstant(Symbol value, Symbol toType) {
        var fromType = value.StaticType;
        if (fromType == toType) {
            return value;
        }
        var mc = value.ModelCore;
        var f = mc.Factory;
        if (value is UndefinedConstantValue) {
            if (toType.IncludesUndefined) return f.UndefinedConstantValue(toType);
            if (toType.IncludesNull) return f.NullConstantValue(toType);
            if (toType.IsFlagsEnum) return f.EnumConstantValue(EnumConstHelpers.Empty(toType), toType);
            return null;
        }
        if (value is NullConstantValue) {
            if (toType.IncludesNull) return f.NullConstantValue(toType);
            if (toType.IncludesUndefined) return f.UndefinedConstantValue(toType);
            return null;
        }
        if (value is NumberConstantValue) {
            if (toType == mc.AnyType || toType == mc.ObjectType)
                return f.NumberConstantValue(value.NumberValue, toType);
            if (toType == mc.DecimalType) return f.DecimalConstantValue((decimal) value.NumberValue, toType);
            return null;
        }
        if (value is DecimalConstantValue) {
            if (toType == mc.AnyType || toType == mc.ObjectType)
                return f.DecimalConstantValue(value.DecimalValue, toType);
            return null;
        }
        if (value is ByteConstantValue) {
            if (toType == mc.AnyType || toType == mc.ObjectType)
                return f.ByteConstantValue(value.ByteValue, toType);
            if (toType == mc.ShortType) return f.ShortConstantValue(value.ByteValue, toType);
            if (toType == mc.IntType) return f.IntConstantValue(value.ByteValue, toType);
            if (toType == mc.LongType) return f.LongConstantValue(value.ByteValue, toType);
            if (toType == mc.BigIntType) return f.BigIntConstantValue((BigInteger) ((double) value.ByteValue), toType);
            if (toType == mc.NumberType) return f.NumberConstantValue(value.ByteValue, toType);
            if (toType == mc.DecimalType) return f.DecimalConstantValue(value.ByteValue, toType);
            return null;
        }
        if (value is ShortConstantValue) {
            if (toType == mc.AnyType || toType == mc.ObjectType)
                return f.ShortConstantValue(value.ShortValue, toType);
            if (toType == mc.IntType) return f.IntConstantValue(value.ShortValue, toType);
            if (toType == mc.LongType) return f.LongConstantValue(value.ShortValue, toType);
            if (toType == mc.BigIntType) return f.BigIntConstantValue((BigInteger) ((double) value.ShortValue), toType);
            if (toType == mc.NumberType) return f.NumberConstantValue(value.ShortValue, toType);
            if (toType == mc.DecimalType) return f.DecimalConstantValue(value.ShortValue, toType);
            return null;
        }
        if (value is IntConstantValue) {
            if (toType == mc.AnyType || toType == mc.ObjectType)
                return f.IntConstantValue(value.IntValue, toType);
            if (toType == mc.LongType) return f.LongConstantValue(value.IntValue, toType);
            if (toType == mc.BigIntType) return f.BigIntConstantValue((BigInteger) ((double) value.IntValue), toType);
            if (toType == mc.NumberType) return f.NumberConstantValue(value.IntValue, toType);
            if (toType == mc.DecimalType) return f.DecimalConstantValue(value.IntValue, toType);
            return null;
        }
        if (value is LongConstantValue) {
            if (toType == mc.AnyType || toType == mc.ObjectType)
                return f.LongConstantValue(value.LongValue, toType);
            return null;
        }
        if (value is BigIntConstantValue) {
            if (toType == mc.AnyType || toType == mc.ObjectType)
                return f.BigIntConstantValue(value.BigIntValue, toType);
            return null;
        }
        return null;
    }

    public static Symbol ConvertImplicit(Symbol value, Symbol toType) {
        var fromType = value.StaticType;
        if (fromType == toType) {
            return value;
        }
        var constConv = ConvertConstant(value, toType);
        if (constConv != null) {
            return constConv;
        }
        var mc = value.ModelCore;
        var f = mc.Factory;
        if (fromType == mc.AnyType) {
            return f.ConversionValue(value, toType, ConversionFromTo.FromAny);
        }
        if (toType == mc.AnyType) {
            return f.ConversionValue(value, toType, ConversionFromTo.ToAny);
        }
        if (InheritedProxies.FindImplicitConversion(fromType, toType) != null) {
            return f.ConversionValue(value, toType, ConversionFromTo.ToUserImplicit);
        }
        if (mc.IsNumericType(fromType) && mc.IsNumericType(toType)) {
            if (fromType == mc.NumberType) {
                if (toType == mc.DecimalType) return f.ConversionValue(value, toType, ConversionFromTo.ToNumTypeWithWiderRange);
            } else if (fromType == mc.ByteType) {
                if (toType == mc.NumberType || toType == mc.DecimalType || toType == mc.ShortType || toType == mc.IntType || toType == mc.LongType || toType == mc.BigIntType) return f.ConversionValue(value, toType, ConversionFromTo.ToNumTypeWithWiderRange);
            } else if (fromType == mc.ShortType) {
                if (toType == mc.NumberType || toType == mc.DecimalType || toType == mc.IntType || toType == mc.LongType || toType == mc.BigIntType) return f.ConversionValue(value, toType, ConversionFromTo.ToNumTypeWithWiderRange);
            } else if (fromType == mc.IntType) {
                if (toType == mc.NumberType || toType == mc.DecimalType || toType == mc.LongType || toType == mc.BigIntType) return f.ConversionValue(value, toType, ConversionFromTo.ToNumTypeWithWiderRange);
            } else if (fromType == mc.LongType) {
                if (toType == mc.BigIntType) return f.ConversionValue(value, toType, ConversionFromTo.ToNumTypeWithWiderRange);
            }
            return null;
        }
        if (toType is UnionType) {
            if (!(fromType is UnionType)) {
                foreach (var memberType in toType.UnionMemberTypes) {
                    var subconversion = TypeConversions.ConvertImplicit(value, memberType);
                    if (subconversion != null) {
                        return f.ConversionValue(subconversion, toType, ConversionFromTo.NonUnionToCompatUnion);
                    }
                }
            }
            if (fromType is UnionType) {
                Symbol u_fromType = fromType;
                Symbol u_toType = toType;
                // this condition allows converting union with undefined or null to
                // compatible union with undefined or null.
                if ((fromType.IncludesNull || fromType.IncludesUndefined)
                &&   (toType.IncludesNull  || toType.IncludesUndefined))
                {
                    u_fromType = u_fromType.ToNonNullableType();
                    u_toType = u_toType.ToNonNullableType();
                }
                var compat = true;
                var targetMembers = u_toType.UnionMemberTypes;
                foreach (var m in u_fromType.UnionMemberTypes) {
                    if (!targetMembers.Contains(m)) {
                        compat = false;
                        break;
                    }
                }
                if (compat) {
                    return f.ConversionValue(value, toType, ConversionFromTo.UnionToCompatUnion);
                }
            }
            return null;
        }
        if (fromType is RecordType && toType is RecordType) {
            var compat = true;
            var inputFields = fromType.RecordTypeFields;
            var targetFields = toType.RecordTypeFields;
            foreach (var field in targetFields) {
                if (!inputFields.Contains(field)) {
                    compat = false;
                    break;
                }
            }
            if (compat) {
                return f.ConversionValue(value, toType, ConversionFromTo.FromRecordToEqvRecord);
            }
        }
        if (fromType.IsSubtypeOf(toType)) {
            return f.ConversionValue(value, toType, ConversionFromTo.ToCovariantType);
        }
        return null;
    }

    public static Symbol ConvertExplicit(Symbol value, Symbol toType, bool isOptional) {
        var fromType = value.StaticType;
        if (fromType == toType) {
            return value;
        }
        var implicitConv = ConvertImplicit(value, toType);
        if (implicitConv != null) {
            return implicitConv;
        }
        var mc = value.ModelCore;
        var f = mc.Factory;

        if (InheritedProxies.FindExplicitConversion(fromType, toType) != null) {
            return f.ConversionValue(value, toType, ConversionFromTo.ToUserExplicit, isOptional);
        }
        if (fromType is UnionType) {
            if (fromType.UnionMemberTypes.Contains(toType)) {
                return f.ConversionValue(value, toType, ConversionFromTo.ToUnionMember, isOptional);
            }
            return null;
        }
        if (toType.IsSubtypeOf(fromType)) {
            return f.ConversionValue(value, toType, ConversionFromTo.ToContravariantType, isOptional);
        }
        if (fromType.IsArgumentationOf(mc.ArrayType) && toType.IsArgumentationOf(mc.ArrayType)) {
            if (fromType.ArgumentTypes[0].IsSubtypeOf(toType.ArgumentTypes[0])) {
                return f.ConversionValue(value, toType, ConversionFromTo.ToCovariantArray);
            }
            if (toType.ArgumentTypes[0].IsSubtypeOf(fromType.ArgumentTypes[0])) {
                return f.ConversionValue(value, toType, ConversionFromTo.ToContravariantArray);
            }
            return null;
        }
        if (mc.IsNumericType(fromType) && mc.IsNumericType(toType)) {
            return f.ConversionValue(value, toType, ConversionFromTo.BetweenNumericTypes);
        }
        if (toType is EnumType) {
            if (fromType == mc.StringType) {
                return f.ConversionValue(value, toType, ConversionFromTo.FromStringToEnum, isOptional);
            }
            if (fromType == toType.NumericType) {
                return f.ConversionValue(value, toType, ConversionFromTo.FromNumberToEnum, isOptional);
            }
        }

        return null;
    }
}