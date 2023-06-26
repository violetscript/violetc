package;

[Value]
public class Number {
    public static const POSITIVE_INFINITY: Number = Infinity;

    public static const NEGATIVE_INFINITY: Number = -Infinity;

    public static const MIN_VALUE: Number;

    public static const MAX_VALUE: Number;

    public native function Number(argument: *);

    public native override function toString(radix: Int? = null): String;

    public native function toExponential(fractionDigits: Int? = null): String;

    /**
     * @throws {RangeError} If `digits` is not between `1` and `100` (inclusive).
     */
    public native function toFixed(digits: Int? = null): String;

    /**
     * @throws {RangeError} If `precision` is not between `1` and `100` (inclusive).
     */
    public native function toPrecision(precision: Int? = null): String;
}

[Value]
public class Decimal {
    public native function Decimal(argument: *);

    public native override function toString(radix: Int? = null): String;
}

/*
[Value]
public class Byte {
    public static const MIN_VALUE: Byte = 0;

    public static const MAX_VALUE: Byte = 0xFF;

    public native function Byte(argument: *);

    public native override function toString(radix: Int? = null): String;

    public native function toExponential(fractionDigits: Int? = null): String;

    /**
     * @throws {RangeError} If `digits` is not between `1` and `100` (inclusive).
     */
    public native function toFixed(digits: Int? = null): String;

    /**
     * @throws {RangeError} If `precision` is not between `1` and `100` (inclusive).
     */
    public native function toPrecision(precision: Int? = null): String;
}

[Value]
public class Short {
    public static const MIN_VALUE: Short = -0x80_00;

    public static const MAX_VALUE: Short = 0x7F_FF;

    public native function Short(argument: *);

    public native override function toString(radix: Int? = null): String;

    public native function toExponential(fractionDigits: Int? = null): String;

    /**
     * @throws {RangeError} If `digits` is not between `1` and `100` (inclusive).
     */
    public native function toFixed(digits: Int? = null): String;

    /**
     * @throws {RangeError} If `precision` is not between `1` and `100` (inclusive).
     */
    public native function toPrecision(precision: Int? = null): String;
}

[Value]
public class Int {
    public static const MIN_VALUE: Int = -0x80_00_00_00;

    public static const MAX_VALUE: Int = 0x7F_FF_FF_FF;

    public native function Int(argument: *);

    public native override function toString(radix: Int? = null): String;

    public native function toExponential(fractionDigits: Int? = null): String;

    /**
     * @throws {RangeError} If `digits` is not between `1` and `100` (inclusive).
     */
    public native function toFixed(digits: Int? = null): String;

    /**
     * @throws {RangeError} If `precision` is not between `1` and `100` (inclusive).
     */
    public native function toPrecision(precision: Int? = null): String;
}

[Value]
public class Long {
    public static const MIN_VALUE: Long;

    public static const MAX_VALUE: Long;

    public native function Long(argument: *);

    public native override function toString(radix: Int? = null): String;
}

[Value]
public class BigInt {
    public native function BigInt(argument: *);

    public native override function toString(radix: Int? = null): String;
}
*/