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

/**
 * The `String` type represents a sequence of UTF-16
 * Code Units.
 */
[Value]
public class String {
    public native function String(argument: *);

    native proxy function add(a: String, b: String): String;
    native proxy function lt(a: String, b: String): Boolean;
    native proxy function gt(a: String, b: String): Boolean;
    native proxy function le(a: String, b: String): Boolean;
    native proxy function ge(a: String, b: String): Boolean;

    proxy function iterateValues(): Generator.<String> {
        for each (const codePoint in this.codePoints()) {
            yield String.fromCodePoint(codePoint);
        }
    }

    public static native function fromCodePoint(...codePoints: [Int]): String;

    public static native function fromCharCode(...charCodes: [Int]): String;

    public native function get isEmpty(): Boolean;

    /**
     * Returns the number of characters in UTF-16 Code Units.
     */
    public native function get length(): Int;

    /**
     * Replaces parameter sequences in a string by given arguments.
     * # Example
     * ```
     * '$a $$'.apply({ a: 10 })
     * '$1 $2'.apply(['one', 'two'])
     * '$<hyphens-n_Underscores>'.apply({ 'hyphens-n_Underscores': 10 })
     * ```
     */
    public native function apply(arguments: Map.<String, *> | [*]): String;

    public function codePoints(): CodePointIterator (
        new CodePointIterator(this)
    );

    public native function charAt(index: Int): String;

    public native function charCodeAt(index: Int): Int;

    public native function codePointAt(index: Int): Int;

    public function concat(...strings: [String]): String (
        ([this, ...strings]: [String]).reduce.<String>((a, b) => a + b)
    );
}

public final class CodePointIterator implements Iterator.<Int> {
    public native function CodePointIterator(string: String);

    public native function next(): {done: Boolean, value?: Int};
}

[Value]
public class Boolean {
    public native function Boolean(argument: *);

    public override native function toString(): String;
}