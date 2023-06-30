package;

public class Error {
    public native function Error(message: String = '', options: ErrorOptions? = null);

    public native function get message(): String;
    public native function set message(value);

    public native function get name(): String;
    public native function set name(value);

    public native function get cause(): *;
    public native function set cause(value);

    public override function toString(): String (
        this.name.isEmpty ? this.message : this.message.isEmpty ? this.name : '$name: $msg'.apply({name: this.name, msg: this.message})
    );
}

public type ErrorOptions = {
    cause: *,
};

public class AggregateError extends Error {
    public function AggregateError(errors: [*], message: String = '', options: ErrorOptions? = null) {
        super(message, options);
        this.errors = errors;
        this.name = 'AggregateError';
    }

    public native function get errors(): [*];
    public native function set errors(value);
}

public class AssertionError extends Error {
    public function AssertionError(message: String = '', options: ErrorOptions? = null) {
        super(message, options);
        this.name = 'AssertionError';
    }
}

public class VerifyError extends Error {
    public function VerifyError(message: String = '', options: ErrorOptions? = null) {
        super(message, options);
        this.name = 'VerifyError';
    }
}