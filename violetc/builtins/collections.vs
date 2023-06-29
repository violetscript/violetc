package;

public interface Iterable.<T> {
    function iterator(): Iterator.<T>;
}

public interface Iterator.<T> extends Iterable.<T> {
    function next(): {done: Boolean, value?: T};

    /**
     * Returns another iterator that yields the results
     * of invoking a function on each item of the current iterator.
     */
    function map.<R>(callbackFn: (item: T) => R): Generator.<R> {
        for each (const item in this) {
            yield callbackFn(item);
        }
    }

    function filter(callbackFn: (item: T) => Boolean): [T] {
        const r = []: [T];
        for each (const item in this) {
            if (callbackFn(item)) {
                r.push(item);
            }
        }
        return r;
    }
    
    function some(callbackFn: (item: T) => Boolean): Boolean {
        for each (const item in this) {
            if (callbackFn(item)) {
                return true;
            }
        }
        return false;
    }
}

public final class Generator.<T> implements Iterator.<T> {
    function iterator(): Iterator.<T> {
        return this;
    }

    public native function next(): {done: Boolean, value?: T};
}

public final class Array.<T> implements Iterable.<T> {
    public static native function from(argument: Iterable.<T>): [T];

    public native function iterator(): Iterator.<T>;

    /**
     * Indicates the number of elements. If `length`
     * is reassigned to a number greater than or equals to the current
     * `length`, it will produce no effect.
     */
    public native function get length(): Int;

    public native function set length(value);

    public native function push(...arguments: [T]): Int;

    proxy native function getIndex(i: Int): undefined | T;

    proxy native function setIndex(i: Int, v: T): void;

    proxy native function equals(a: [T], b: [T]): Boolean;

    proxy native function notEquals(a: [T], b: [T]): Boolean;

    proxy function has(v: T): Boolean (
        this.indexOf(v) != -1
    );

    /**
     * If index is out of bounds, throws an exception;
     * otherwise, returns the element at that index.
     * This is not the same as `array[i]!` as it allows
     * accessing `undefined` or `null` values.
     * @hidden
     */
    public native function atStrict(index: Int): T;

    proxy function iterateValues(): Generator.<T> {
        for (var i: Int = 0; i < this.length; ++i) {
            yield this.atStrict(i);
        }
    }

    proxy function add(a: [T], b: [T]): [T] (
        a.concat(b)
    );

    public function get isEmpty(): Boolean (
        this.length == 0
    );

    public function get first(): undefined | T (
        this[0]
    );

    public function set first(value) {
        if (this.isEmpty) return;
        this[0] = value;
    }

    public function get last(): undefined | T (
        this.isEmpty ? undefined : this[this.length - 1]
    );

    public function set last(value) {
        if (this.isEmpty) return;
        this[this.length - 1] = value;
    }

    /**
     * The `concat` method is used to merge two or more arrays.
     * This method does not modify the existing arrays and returns
     * a new array.
     */
    public native function concat(...arrays: [[T]]): [T];

    /**
     * @throws {TypeError} If `initialValue` is not provided and the array is empty or
     * if `U` and `T` are incompatible types.
     */
    public native function reduce.<U>(callbackFn: (accumulator: U, currentValue: T) => U, initialValue: undefined | U = undefined): U;

    // reduceRight is for later for now.

    public function filter(callbackFn: (item: T, index: Int, array: [T]) => Boolean): [T] {
        const r: [T] = [];
        for (var i: Int = 0; i < this.length; ++i) {
            const item = this.atStrict(i);
            if (callbackFn(item, i, this)) {
                r.push(item);
            }
        }
        return r;
    }

    public function map.<R>(callbackFn: (item: T, index: Int, array: [T]) => R): [R] {
        const r: [R] = [];
        for (var i: Int = 0; i < this.length; ++i) {
            r.push(callbackFn(this.atStrict(i), i, this));
        }
        return r;
    }

    public function some(callbackFn: (item: T, index: Int, array: [T]) => Boolean): Boolean {
        for (var i: Int = 0; i < this.length; ++i) {
            if (callbackFn(this.atStrict(i), i, this)) {
                return true;
            }
        }
        return false;
    }

    public function every(callbackFn: (item: T, index: Int, array: [T]) => Boolean): Boolean {
        for (var i: Int = 0; i < this.length; ++i) {
            if (!callbackFn(this.atStrict(i), i, this)) {
                return false;
            }
        }
        return true;
    }

    public function forEach(callbackFn: (item: T, index: Int, array: [T]) => void): void {
        for (var i: Int = 0; i < this.length; ++i) {
            callbackFn(this.atStrict(i), i, this);
        }
    }

    /**
     * @internal This method must be efficient
     * when concatenating multiple elements
     * and each is converted to string similiar to `String(v)`.
     */
    public native function join(sep: String = ', '): String;

    public native function slice(from: Int, to: Int? = null): [T];

    public native function indexOf(value: T, startIndex: Int? = null): Int;

    public native function lastIndexOf(value: T, startIndex: Int? = null): Int;

    /**
     * Reverses the array in-place.
     */
    public native function reverse(): [T];

    /**
     * Sorts the array in place. The default sorting behavior, if
     * `compareFn` is not specified, should be ascending order
     * for number types and the string type.
     */
    public native function sort(compareFn: ((a: T, b: T) => Int)? = null): [T];

    /**
     * The copying version of the `reverse()` method.
     * Returns a new array with elements in reversed order.
     */
    public function toReversed(): [T] (
        this.slice(0).reverse()
    );

    /**
     * The copying version of the `sort()` method.
     * Returns a new array with sorted elements.
     */
    public function toSorted(compareFn: ((a: T, b: T) => Int)? = null): [T] (
        this.slice(0).sort(compareFn)
    );

    // deepEquals() for later

    public native function insertAt(index: Int, value: T): void;

    public native function removeAt(index: Int): undefined | T;

    public native function splice(start: Int, deleteCount: Int = Infinity, ...items: [T]): [T];
}

/**
 * Represents a growable array of bytes for working with
 * binary data.
 * Byte order is determined by the `endian` property,
 * which is `'littleEndian'` by default.
 */
public final class ByteArray implements IDataInput, IDataOutput, Iterable.<Byte> {
    public native function ByteArray();
    public static native function from(argument: Iterable.<Byte>): ByteArray;
    public static native function withCapacity(initialCapacity: Int): ByteArray;
    public static native function withZeroes(length: Int): ByteArray;

    public native function iterator(): Iterator.<Byte>;

    /**
     * Indexing. If index is out of bounds, it has no effect or yields 0.
     */
    proxy native function getIndex(index: Int): Byte;
    proxy native function setIndex(index: Int, value: Byte): void;

    proxy function iterateValues(): Generator.<Byte> {
        for (var i: Int = 0; i < this.length; ++i) {
            yield this[i];
        }
    }

    proxy native function equals(a: ByteArray, b: ByteArray): Boolean;
    proxy native function notEquals(a: ByteArray, b: ByteArray): Boolean;

    /**
     * Indicates number of bytes. If reassigned to a higher value,
     * it has no effect.
     */
    public native function get length(): Int;
    public native function set length(value);

    public native function get endian(): Endian;
    public native function set endian(value);

    public native function get position(): Int;
    public native function set position(value);

    public native function get bytesAvailable(): Int;
    public native function get hasBytesAvailable(): Boolean;

    /**
     * Clears the array, resetting its `length` and position
     * to zero. `keepCapacity` is `false` by default.
     */
    public native function clear(keepCapacity: Boolean = false): void;

    public function values(): Generator.<Byte> {
        for (var i: Int = 0; i < this.length; ++i) {
            yield this[i];
        }
    }

    public native function readFloat(): Number;
    public native function readDouble(): Number;
    public native function readByte(): Byte;
    public native function readSignedByte(): Int;
    public native function readShort(): Short;
    public native function readUnsignedShort(): Int;
    public native function readInt(): Int;
    public native function readUnsignedInt(): Long;
    public native function readLong(): Long;
    public native function readUnsignedLong(): BigInt;

    public native function writeFloat(value: Number): void;
    public native function writeDouble(value: Number): void;
    public native function writeByte(value: Byte): void;
    public native function writeSignedByte(value: Int): void;
    public native function writeShort(value: Short): void;
    public native function writeUnsignedShort(value: Int): void;
    public native function writeInt(value: Int): void;
    public native function writeUnsignedInt(value: Long): void;
    public native function writeLong(value: Long): void;
    public native function writeUnsignedLong(value: BigInt): void;
}

public enum Endian {
    LITTLE_ENDIAN;
    BIG_ENDIAN;
}

/**
 * Provides a set of methods for reading binary data.
 */
public interface IDataInput {
    function get endian(): Endian;
    function set endian(value);

    function get bytesAvailable(): Int;
    function get hasBytesAvailable(): Boolean;

    /**
     * Reads single-precision floating point.
     * @throws {IOError}
     */
    function readFloat(): Number;

    /**
     * Reads double-precision floating point.
     * @throws {IOError}
     */
    function readDouble(): Number;

    /**
     * Reads unsigned byte.
     * @throws {IOError}
     */
    function readByte(): Byte;

    /**
     * @throws {IOError}
     */
    function readSignedByte(): Int;

    /**
     * Reads signed short.
     * @throws {IOError}
     */
    function readShort(): Short;

    /**
     * Reads unsigned short.
     * @throws {IOError}
     */
    function readUnsignedShort(): Int;

    /**
     * Reads signed integer.
     * @throws {IOError}
     */
    function readInt(): Int;

    /**
     * Reads unsigned integer.
     * @throws {IOError}
     */
    function readUnsignedInt(): Long;

    /**
     * Reads signed long.
     * @throws {IOError}
     */
    function readLong(): Long;

    /**
     * Reads unsigned long.
     * @throws {IOError}
     */
    function readUnsignedLong(): BigInt;
}

/**
 * Provides a set of methods for writing binary data.
 */
public interface IDataOutput {
    function get endian(): Endian;
    function set endian(value);

    /**
     * Writes single-precision floating point.
     * @throws {IOError}
     */
    function writeFloat(value: Number): void;

    /**
     * Writes double-precision floating point.
     * @throws {IOError}
     */
    function writeDouble(value: Number): void;

    /**
     * Writes unsigned byte.
     * @throws {IOError}
     */
    function writeByte(value: Byte): void;

    /**
     * Writes signed byte.
     * @throws {IOError}
     */
    function writeSignedByte(value: Int): void;

    /**
     * Writes signed short.
     * @throws {IOError}
     */
    function writeShort(value: Short): void;

    /**
     * Writes unsigned short.
     * @throws {IOError}
     */
    function writeUnsignedShort(value: Int): void;

    /**
     * Writes signed integer.
     * @throws {IOError}
     */
    function writeInt(value: Int): void;

    /**
     * Writes unsigned integer.
     * @throws {IOError}
     */
    function writeUnsignedInt(value: Long): void;

    /**
     * Writes signed long.
     * @throws {IOError}
     */
    function writeLong(value: Long): void;

    /**
     * Writes unsigned long.
     * @throws {IOError}
     */
    function writeUnsignedLong(value: BigInt): void;
}

public final class Map.<K, V> implements Iterable.<[K, V]> {
    public native function iterator(): Iterator.<[K, V]>;
}