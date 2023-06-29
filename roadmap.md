# Compiler roadmap

Some of the notes in this document apply to verification and bytecode or code generation.

- [ ] **VioletDoc:** In the verifier, for valid annotatable definitions, parse VioletDoc comments for the items. For efficiency, I guess the parser could attach any detected `/** */` comments to the annotatable definitions so that the list of comments doesn't need to be iterated for matching span.
  - [ ] VioletDoc comments applied to record fields from a record alias contribute `@field` tags to the alias (`@field {x} d`). It will also work for subfields.
- [ ] **VioletDoc:** When generating documentation for built-ins, the `global` reference (which is the top-level package) has to be documented manually, since it is not defined by any source.
- [ ] **VioletDoc:** Apply doc comments to enum variants.
- [ ] **VioletDoc:** Omit interface implementor methods and overrides if they have no attached doc comment.
- [ ] **Code generation:** Remember that an expression's result may have been wrapped into a `ConversionValue` by the type checker, so access it carefully during codegen.
- [ ] **Code generation:** Empty programs that, for example, contain include directives whose inner statement sequence is empty or consist of other empty include directives, should not generate any activation and not be evaluated.
- [ ] **Code generation:** An expression whose associated symbol is a constant value should not be evaluated or processed at runtime. `exp.semanticSymbol.isConstantValue`.
- [ ] **Code generation:** Since the operator proxies for all numeric types is defined in the semantic model core and not in the standard built-in sources, the generated code should not be based on any proxy definition for them; the purpose of this is to avoid polluting the sources of the standard built-in objects. All of the following unary and binary operators are supported for all numeric types: `+ - ~ < > <= >= + - * / % ** << >> >>> & ^ |`
- [ ] **Code generation:** Don't forget to implement the string proxy operators (`+ < > <= >=`).
- [ ] **Code generation:** When a class inherits static methods or virtual properties, `this` has to be replaced by that class in each such method. Look everywhere for `ClassStaticThis`.
- [ ] **Code generation:** Variables without constant initial value are represented in memory similiar to a Rust `Option<T>`. This is important because of constructors and `this` accessed before `super()`.
- [ ] Decorators must not be allowed in certain places, like over ordinary functions, ordinary variables and `static` variables.
- [ ] **Code generation:** Since method overrides can specify additional optional parameters or an additional rest parameter and a subtype return type or a type different from an original `*` return type, conversions may be necessary in different senses in the override implementation and overrides may need to be invoked with more arguments.
- [ ] **Code generation:** FFI names for functions are automatically chosen based on parent definition (`fn.parentDefinition.fullyQualifiedName`), replacing dots by `_`. For getters and setters, there's either a `get_` or `set_` prefix before the item name (parent's name appears before that prefix followed by `_`).
- [ ] **Code generation:** In an object initialiser for any type other than `Map`, spreads must be evaluated before the fields (so one loop for spreads and one loop for fields, don't forget).
- [ ] **Code generation:** When iterating arrays (the `Array.<T>` type) in `for each`, do not use their iterator, for optimization purposes; generate some `for (...; i < array.length; ++i)` loop (do not cache the length too on that loop to not crash on array length modification).
- [ ] **Language Server Protocol (LSP):** Avoid re-compiling libraries by caching only publicly-visible definitions without shipping their implementation and loading them into the semantic core for use in LSP.

### `x is y: C`

- [x] Use `exp.GetTypeTestFrames` to gather binary `is` frames.
- [x] Attach a new frame to this binary operator and define `y` there. Binary `&&` operator recursively pushes frames from left operand when verifying right operand and pops them back. Ternary operator recursively pushes frames from its test when verifying the consequent expression and pops them back.
- [x] Few statements should push any frames recursively from condition expressions when verifying their bodies and pop back. This is done only for `if (test)`, `while (test)` and `for (; test;)`.

### Not Implemented

- [ ] Generic Type Inference; e.g. `a.map(...);` rather than `a.map.<R>(...);`.
- [ ] `o?.k > 0 ? x : y`: using optional access in a condition

### Future Features

- [ ] Implicit conversion between function types with only differing parameter names (`ConversionFromTo.ToFunctionWithDifferentParamNames`).
- [ ] `!` in array destructuring. Useful since you can't destructure from `undefined | T`.

### Done

- [x] Enum definitions "override" the `valueOf()` and `toString()` methods.
- [x] `typeExp.<>`: verify constraints
- [x] `exp.<>`: verify constraints
- [x] `v as T` or `v as? T` DO NOT turn `T` into `T?`; instead, verification for such conversion produces a `ConversionValue` with `isOptional` set to `true`, and `TypeConversions.convertExploicit(...)` will properly wrap `T` into `T?` if needed.
- [x] Instance methods can be overriden with additional optional parameters and a contravariant return type or any return type if the original return type is any.
- [x] Prohibit defining writable instance (non `static`) variables for `[Value]` classes.

### C# Notes

- Careful when writting C# code: write flag values manually for `[Flags]` enums, as C# does not increment its counter correctly for flags.