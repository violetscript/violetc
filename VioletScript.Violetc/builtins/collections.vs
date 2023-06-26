package;

public interface Iterator.<T> {
    function next(): {done: Boolean, value?: T};

    function map.<R>(callbackFn: (item: T) => R): Generator.<R> {
        for each (const item in this) {
            yield item;
        }
    }
}

public final class Generator.<T> implements Iterator.<T> {
    public native function next(): {done: Boolean, value?: T};
}

public final class Array.<T> {
    public static native function from(argument: Iterator.<T>): [T];
}