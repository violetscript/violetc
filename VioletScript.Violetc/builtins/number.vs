package;

[Value]
public class Number {
    public static const POSITIVE_INFINITY: Number = Infinity;

    public static const NEGATIVE_INFINITY: Number = -Infinity;

    public static const MIN_VALUE: Number;

    public static const MAX_VALUE: Number;

    public native function Number(argument: *);

    public native override function toString(radix: undefined | Int = undefined): String;
}