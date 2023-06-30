package;

public namespace Reflect {
    public native function construct(typeObject: *, arguments: [*]): *;

    /**
     * Returns a type meta-object describing the given type.
     * Returns any of:
     * - `null`
     * - `AnyType`
     * - `UndefinedType`
     * - `NullType`
     * - `ArrayType`
     * - `ClassType`
     * - `EnumType`
     * - `InterfaceType`
     * - `TypeWithArguments`
     * - `UnionType`
     * - `TupleType`
     * - `RecordType`
     * - `FunctionType`
     * - `TypeParameterType`
     */
    public native function describeType(typeObject: Class): Object;

    public native function get(base: *, key: String): *;
    public native function set(base: *, key: String, value: *): void;
}

/**
 * Represents general name bindings for type reflection purposes.
 */
public final class Binding {
    public native function get name(): String;
    public native function get type(): Class;
    public native function get readOnly(): Boolean;
    public native function get writeOnly(): Boolean;
}

/**
 * A type-meta object describing the any type (`*`). The any type
 * is empty, therefore it contains no information.
 */
public final class AnyType {
}

/**
 * A type-meta object describing the `undefined` type. The `undefined` type
 * is empty, therefore it contains no information.
 */
public final class UndefinedType {
}

/**
 * A type-meta object describing the `null` type. The `null` type
 * is empty, therefore it contains no information.
 */
public final class NullType {
}

/**
 * A type-meta object describing an array type.
 */
public final class ArrayType {
    public native function get elementType(): Class;
    public native function set elementType(value);
}