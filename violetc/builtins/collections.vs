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

    public native function push(...arguments: [T]): Int;

    proxy native function getIndex(i: Int): undefined | T;

    proxy native function setIndex(i: Int, v: T): void;

    proxy function add(a: [T], b: [T]): [T] (
        a.concat(b)
    );

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
}

public final class ByteArray {
}