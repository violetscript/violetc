package;

public class Function {
    public native function Function();;

    public native function apply(arguments: [*]): void;

    public native function call(...arguments: [*]): void;

    public native function bind(...arguments: [*]): void;
}