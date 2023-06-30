package;

public final class Promise.<T> {
    public native function Promise(resolve: (value: T) => void, reject: (reason: *) => void);
    public native static function all(iterable: Iterable.<Promise.<T>>): Promise.<[T]>;
    public native static function allSettled(iterable: Iterable.<Promise.<T>>): Promise.<[PromiseOutcome.<T>]>;
    public native static function any(iterable: Iterable.<Promise.<T>>): Promise.<T>;
    public native static function race(iterable: Iterable.<Promise.<T>>): Promise.<T>;
    public native static function resolve(value: Promise.<T> | T): Promise.<T>;
    public native static function reject(reason: *): Promise.<T>;

    /**
     * @param onRejected If specified and the base `Promise` is rejected,
     * the return of this callback is the fulfillment value of the returned `Promise`.
     */
    public native function then.<F>(onFulfilled: (value: T) => F, onRejected: ((reason: *) => F)?): Promise.<F>;

    /**
     * @param onRejected If the base `Promise` is rejected,
     * the return of this callback is the fulfillment value of the returned `Promise`.
     */
    public native function #catch.<F>(onRejected: (reason: *) => F): Promise.<F>;

    public native function #finally.<F>(onFinally: () => *): *;
}

public type PromiseOutcome.<T> = {
    status: PromiseOutcomeStatus,
    value: undefined | T,
    reason: *,
};

public enum PromiseOutcomeStatus wraps Int {
    FULFILLED = 0;
    REJECTED = 1;
}