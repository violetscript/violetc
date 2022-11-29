namespace VioletScript.Parser.Semantic.Logic;

using System.Collections.Generic;
using System.Numerics;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Model;

public enum ConversionFromTo {
    ToUserImplicit,
    ToNumTypeWithWiderRange,
    NonUnionToCompatUnion,
    UnionToCompatUnion,
    FromRecordToEqvRecord,
    FromAny,
    ToAny,
    ToCovariantType,
    ToUserExplicit,
    ToOutOfUnionWithNull,
    ToOutOfUnionWithUndefined,
    ToOutOfUnionWithUndefinedAndNull,
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
        if (InheritedProxies.FindImplicitConversion(fromType, toType) != null) {
            return f.ConversionValue(value, toType, ConversionFromTo.ToUserImplicit);
        }
        if (mc.IsNumericType(fromType) && mc.IsNumericType(toType)) {
            //
        }
        return null;
    }

    public static Symbol ConvertExplicit(Symbol value, Symbol toType) {
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
        //
        return null;
    }
}