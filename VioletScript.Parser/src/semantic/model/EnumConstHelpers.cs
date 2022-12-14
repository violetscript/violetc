namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using System.Numerics;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public static class EnumConstHelpers {
    public static Symbol Empty(Symbol type) {
        var modelCore = type.ModelCore;
        var factory = modelCore.Factory;
        return factory.EnumConstantValue(FromDouble(type, 0), type);
    }

    public static object Zero(Symbol type) {
        return FromDouble(type, 0);
    }

    public static object One(Symbol type) {
        return FromDouble(type, 1);
    }

    public static object FromDouble(Symbol type, double value) {
        var modelCore = type.ModelCore;
        if (type == modelCore.NumberType) return value;
        if (type == modelCore.DecimalType) return (decimal) value;
        if (type == modelCore.ByteType) return (byte) value;
        if (type == modelCore.ShortType) return (short) value;
        if (type == modelCore.IntType) return (int) value;
        if (type == modelCore.LongType) return (long) value;
        if (type == modelCore.BigIntType) return (BigInteger) value;
        throw new ArgumentException("Enum numeric type not handled: " + type.ToString());
    }

    public static object MultiplyPer2(Symbol type, object @base) {
        var modelCore = type.ModelCore;
        if (type == modelCore.NumberType) return ((double) @base) * ((double) 2);
        if (type == modelCore.DecimalType) return ((decimal) @base) * ((decimal) 2);
        if (type == modelCore.ByteType) return ((byte) @base) * ((byte) 2);
        if (type == modelCore.ShortType) return ((short) @base) * ((short) 2);
        if (type == modelCore.IntType) return ((int) @base) * ((int) 2);
        if (type == modelCore.LongType) return ((long) @base) * ((long) 2);
        if (type == modelCore.BigIntType) return ((BigInteger) @base) * ((BigInteger) ((double) 2));
        throw new ArgumentException("Enum numeric type not handled: " + type.ToString());
    }

    public static object Increment(Symbol type, object @base) {
        var modelCore = type.ModelCore;
        if (type == modelCore.NumberType) return ((double) @base) + ((double) 1);
        if (type == modelCore.DecimalType) return ((decimal) @base) + ((decimal) 1);
        if (type == modelCore.ByteType) return ((byte) @base) + ((byte) 1);
        if (type == modelCore.ShortType) return ((short) @base) + ((short) 1);
        if (type == modelCore.IntType) return ((int) @base) + ((int) 1);
        if (type == modelCore.LongType) return ((long) @base) + ((long) 1);
        if (type == modelCore.BigIntType) return ((BigInteger) @base) + ((BigInteger) ((double) 1));
        throw new ArgumentException("Enum numeric type not handled: " + type.ToString());
    }

    public static bool Includes(Symbol type, object @base, object flag) {
        var modelCore = type.ModelCore;
        if (type == modelCore.NumberType) return (((int) @base) & ((int) flag)) != 0;
        if (type == modelCore.DecimalType) return (((int) @base) & ((int) flag)) != 0;
        if (type == modelCore.ByteType) return (((byte) @base) & ((byte) flag)) != 0;
        if (type == modelCore.ShortType) return (((short) @base) & ((short) flag)) != 0;
        if (type == modelCore.IntType) return (((int) @base) & ((int) flag)) != 0;
        if (type == modelCore.LongType) return (((long) @base) & ((long) flag)) != 0;
        if (type == modelCore.BigIntType) return (((BigInteger) @base) & ((BigInteger) flag)) != 0;
        throw new ArgumentException("Enum numeric type not handled: " + type.ToString());
    }
}