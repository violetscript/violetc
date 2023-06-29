package;

public interface Iterator.<T> {
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
    public native function next(): {done: Boolean, value?: T};
}

public final class Array.<T> {
    public static native function from(argument: Iterator.<T>): [T];

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

public final class ByteArray {
}