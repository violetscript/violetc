namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using System.Numerics;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public static class EnumConstHelpers
{
    /// <summary>
    /// Combines two flags values.
    /// </summary>
    public static object IncludeFlags(object one, object other)
    {
        if (one is double doubleV)
        {
            return (double) (((int) doubleV) | ((int) ((double) other)));
        }
        if (one is decimal decimalV)
        {
            return (decimal) (((int) decimalV) | ((int) ((decimal) other)));
        }
        if (one is int intV)
        {
            return intV | ((int) other);
        }
        if (one is byte byteV)
        {
            return (byte) (byteV | ((byte) other));
        }
        if (one is short shortV)
        {
            return (short) (shortV | ((short) other));
        }
        if (one is long longV)
        {
            return longV | ((long) other);
        }
        if (one is BigInteger bigIntV)
        {
            return bigIntV | ((BigInteger) other);
        }
        throw new Exception("Unimplemented");
    }

    /// <summary>
    /// Determines if a flags value is empty.
    /// </summary>
    public static bool HasZeroFlags(object v)
    {
        if (v is double doubleV)
        {
            return doubleV == 0;
        }
        if (v is decimal decimalV)
        {
            return decimalV == 0;
        }
        if (v is int intV)
        {
            return intV == 0;
        }
        if (v is byte byteV)
        {
            return byteV == 0;
        }
        if (v is short shortV)
        {
            return shortV == 0;
        }
        if (v is long longV)
        {
            return longV == 0;
        }
        if (v is BigInteger bigIntV)
        {
            return bigIntV == 0;
        }
        throw new Exception("Unimplemented");
    }

    /// <summary>
    /// Determines if two enumeration values are equals.
    /// </summary>
    public static bool ValuesEquals(object one, object other)
    {
        if (one is double doubleV)
        {
            return doubleV == ((double) other);
        }
        if (one is decimal decimalV)
        {
            return decimalV == ((decimal) other);
        }
        if (one is int intV)
        {
            return intV == ((int) other);
        }
        if (one is byte byteV)
        {
            return byteV == ((byte) other);
        }
        if (one is short shortV)
        {
            return shortV == ((short) other);
        }
        if (one is long longV)
        {
            return longV == ((long) other);
        }
        if (one is BigInteger bigIntV)
        {
            return bigIntV == ((BigInteger) other);
        }
        throw new Exception("Unimplemented");
    }

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
        type = type.ToNonNullableType();
        if (type is EnumType)
        {
            type = type.NumericType;
        }
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
        type = type.ToNonNullableType();
        if (type is EnumType)
        {
            type = type.NumericType;
        }
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
        type = type.ToNonNullableType();
        if (type is EnumType)
        {
            type = type.NumericType;
        }
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
        type = type.ToNonNullableType();
        if (type is EnumType)
        {
            type = type.NumericType;
        }
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

    public static bool IsOne(object value)
    {
        if (value is double doubleV)
        {
            return doubleV == 1;
        }
        if (value is decimal decimalV)
        {
            return decimalV == 1;
        }
        if (value is int intV)
        {
            return intV == 1;
        }
        if (value is byte byteV)
        {
            return byteV == 1;
        }
        if (value is short shortV)
        {
            return shortV == 1;
        }
        if (value is long longV)
        {
            return longV == 1;
        }
        if (value is BigInteger bigIntV)
        {
            return bigIntV == 1;
        }
        throw new Exception("Unimplemented");
    }

    public static bool IsPowerOf2(object value)
    {
        if (value is double doubleV)
        {
            var intV2 = (int) doubleV;
            return (intV2 != 0) && ((intV2 & (intV2 - 1)) == 0);
        }
        if (value is decimal decimalV)
        {
            var intV2 = (int) decimalV;
            return (intV2 != 0) && ((intV2 & (intV2 - 1)) == 0);
        }
        if (value is int intV)
        {
            return (intV != 0) && ((intV & (intV - 1)) == 0);
        }
        if (value is byte byteV)
        {
            return (byteV != 0) && ((byteV & (byteV - 1)) == 0);
        }
        if (value is short shortV)
        {
            return (shortV != 0) && ((shortV & (shortV - 1)) == 0);
        }
        if (value is long longV)
        {
            return (longV != 0) && ((longV & (longV - 1)) == 0);
        }
        if (value is BigInteger bigIntV)
        {
            return (bigIntV != 0) && ((bigIntV & (bigIntV - 1)) == 0);
        }
        throw new Exception("Unimplemented");
    }
}