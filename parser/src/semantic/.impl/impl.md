# Implementation

## Numeric types

To support additional numeric types:

- Introduce a built-in class in `ModelCore`
- Add to numeric type set of `ModelCore`
- If it is an integer type, add to integer type set of `ModelCore`
- Introduce a `ConstantValue` subtype
- Resolve default constant value in `TypeSystem.DefaultValue`
- Provide conversions between other numeric types at `TypeConversions` logic
- Provide a correspoding `Factory` method
- Provide corresponding helpers in `EnumConstHelpers`
- Provide corresponding helpers in `NumConstHelpers`