package;

[Value]
public class Number {
    public static const POSITIVE_INFINITY: Number = Infinity;

    public static const NEGATIVE_INFINITY: Number = -Infinity;

    public native function Number(argument: *);

    public native override function toString(radix: undefined | Int = undefined): String;
}