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
}