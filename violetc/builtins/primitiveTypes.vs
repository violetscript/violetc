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

    /**
     * Returns a range iterator. If `step >= 0`, it is similiar
     * to `for (var i = from; i < to; i += step)`; otherwise
     * `for (var i = from; i > to; i += step)`.
     */
    public static function range(from: Number, to: Number, step: Number = 1): Generator.<Number> {
        if (step < 0) {
            for (var i = from; i > to; i += step) {
                yield i;
            }
            return;
        }
        for (var i = from; i < to; i += step) {
            yield i;
        }
    }
}

[Value]
public class Decimal {
    public native function Decimal(argument: *);

    public native override function toString(radix: Int? = null): String;

    /**
     * Returns a range iterator. If `step >= 0`, it is similiar
     * to `for (var i = from; i < to; i += step)`; otherwise
     * `for (var i = from; i > to; i += step)`.
     */
    public static function range(from: Decimal, to: Decimal, step: Decimal = 1): Generator.<Decimal> {
        if (step < 0) {
            for (var i = from; i > to; i += step) {
                yield i;
            }
            return;
        }
        for (var i = from; i < to; i += step) {
            yield i;
        }
    }
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

    /**
     * Returns a range iterator. If `step >= 0`, it is similiar
     * to `for (var i = from; i < to; i += step)`; otherwise
     * `for (var i = from; i > to; i += step)`.
     */
    public static function range(from: Byte, to: Byte, step: Byte = 1): Generator.<Byte> {
        if (step < 0) {
            for (var i = from; i > to; i += step) {
                yield i;
            }
            return;
        }
        for (var i = from; i < to; i += step) {
            yield i;
        }
    }
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

    /**
     * Returns a range iterator. If `step >= 0`, it is similiar
     * to `for (var i = from; i < to; i += step)`; otherwise
     * `for (var i = from; i > to; i += step)`.
     */
    public static function range(from: Short, to: Short, step: Short = 1): Generator.<Short> {
        if (step < 0) {
            for (var i = from; i > to; i += step) {
                yield i;
            }
            return;
        }
        for (var i = from; i < to; i += step) {
            yield i;
        }
    }
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

    public static native function min(...values: [Int]): Int;

    public static native function max(...values: [Int]): Int;

    /**
     * Returns a range iterator. If `step >= 0`, it is similiar
     * to `for (var i = from; i < to; i += step)`; otherwise
     * `for (var i = from; i > to; i += step)`.
     */
    public static function range(from: Int, to: Int, step: Int = 1): Generator.<Int> {
        if (step < 0) {
            for (var i = from; i > to; i += step) {
                yield i;
            }
            return;
        }
        for (var i = from; i < to; i += step) {
            yield i;
        }
    }
}

[Value]
public class Long {
    public static const MIN_VALUE: Long;

    public static const MAX_VALUE: Long;

    public native function Long(argument: *);

    public native override function toString(radix: Int? = null): String;

    /**
     * Returns a range iterator. If `step >= 0`, it is similiar
     * to `for (var i = from; i < to; i += step)`; otherwise
     * `for (var i = from; i > to; i += step)`.
     */
    public static function range(from: Long, to: Long, step: Long = 1): Generator.<Long> {
        if (step < 0) {
            for (var i = from; i > to; i += step) {
                yield i;
            }
            return;
        }
        for (var i = from; i < to; i += step) {
            yield i;
        }
    }
}

[Value]
public class BigInt {
    public native function BigInt(argument: *);

    public native override function toString(radix: Int? = null): String;

    /**
     * Returns a range iterator. If `step >= 0`, it is similiar
     * to `for (var i = from; i < to; i += step)`; otherwise
     * `for (var i = from; i > to; i += step)`.
     */
    public static function range(from: BigInt, to: BigInt, step: BigInt = 1): Generator.<BigInt> {
        if (step < 0) {
            for (var i = from; i > to; i += step) {
                yield i;
            }
            return;
        }
        for (var i = from; i < to; i += step) {
            yield i;
        }
    }
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

    proxy function getIndex(index: Int): String (
        index < 0 || index >= this.length ? '' : String.fromCodePoint(this.codePointAt(index))
    );

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

    public function rightCodePoints(): RightCodePointIterator (
        new RightCodePointIterator(this)
    );

    public native function charAt(index: Int): String;

    public native function charCodeAt(index: Int): Int;

    public native function codePointAt(index: Int): Int;

    public function concat(...strings: [String]): String (
        ([this, ...strings]: [String]).join('')
    );

    public function repeat(count: Int): String (
        Array.<String>.from(Int.range(0, Int.max(0, count)).map.<String>(_ => this)).join('')
    );

    public native function replace(pattern: String | ITextPattern, replacement: String | (match: TextMatch) => String): String;

    /**
     * @throws {TypeError} If the `pattern` is a regex that does not have
     * the global (`g`) flag set.
     */
    public native function replaceAll(pattern: String | ITextPattern, replacement: String | (match: TextMatch) => String): String;

    public native function match(pattern: ITextPattern): TextMatch?;

    public function matchAll(pattern: ITextPattern): Generator.<TextMatch> {
        for (;;) {
            const match = this.match(pattern);
            if (match != null) {
                yield match!;
            }
        }
    }

    public native function reverse(): String;

    public native function split(pattern: String | ITextPattern): [String];

    public native function indexOf(substring: String, startIndex: Int? = null): Int;

    public native function lastIndexOf(substring: String, startIndex: Int? = null): Int;

    public native function trim(): String;

    public native function trimLeft(): String;

    public native function trimRight(): String;

    public native function startsWith(str: String): Boolean;

    public native function endsWith(str: String): Boolean;

    public native function slice(from: Int, to: Int? = null): String;

    public native function substr(from: Int, length: Int? = null): String;

    public native function substring(from: Int, to: Int? = null): String;
}

public final class CodePointIterator implements Iterator.<Int> {
    public native function CodePointIterator(string: String);

    public native function next(): {done: Boolean, value?: Int};
}

public final class RightCodePointIterator implements Iterator.<Int> {
    public native function RightCodePointIterator(string: String);

    public native function next(): {done: Boolean, value?: Int};
}

[Value]
public class Boolean {
    public native function Boolean(argument: *);

    public override native function toString(): String;
}