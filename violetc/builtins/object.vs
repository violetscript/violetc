package;

[FFI(memorySize = 8)]
public class Object {
    public native function Object();
    public native function get constructor(): Class;
    public native function toString(): String;
    public native function valueOf(): *;
}