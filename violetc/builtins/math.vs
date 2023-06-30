package;

public namespace Math {
    public native function min(...values: [*]): *;
    public native function max(...values: [*]): *;

    /**
     * Applies minimum and maximum limit to a value.
     */
    public function clamp(value: *, min: *, max: *): * (
        value < min ? min : value > max ? max : value
    );
}