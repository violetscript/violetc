# Compiler roadmap

Some of the notes in this document apply to verification and bytecode or code generation.

- [ ] **VioletDoc:** In the verifier, for valid annotatable definitions, parse VioletDoc comments for the items. For efficiency, I guess the parser could attach any detected `/** */` comments to the annotatable definitions so that the list of comments doesn't need to be iterated for matching span.
  - [ ] _Do not_ parse VioletDoc comments applied to record fields. This is not supported in VioletDoc.
- [ ] **Code generation:** Empty programs that, for example, contain include directives whose inner statement sequence is empty or consist of other empty include directives, should not generate any activation and not be evaluated.
- [ ] **Code generation:** An expression whose associated symbol is a constant value should not be evaluated or processed at runtime. `exp.semanticSymbol.isConstantValue`.
- [ ] **Code generation:** When a class inherits static methods or virtual properties, `this` has to be replaced by that class in each such method. Look everywhere for `ClassStaticThis`.
- [ ] **Code generation:** Variables with type without default value are represented in memory similiar to a Rust `Option<T>`. This is important because of constructors and `this` accessed before `super()`.
- [ ] Decorators must not be allowed in certain places, like over ordinary functions, ordinary variables and `static` variables.
- [ ] **Language Server Protocol (LSP):** Avoid re-compiling libraries by caching only publicly-visible definitions without shipping their implementation and loading them into the semantic core for use in LSP.

### `x is y: C`

- [x] Use `exp.GetTypeTestFrames` to gather binary `is` frames.
- [x] Attach a new frame to this binary operator and define `y` there. Binary `&&` operator recursively pushes frames from left operand when verifying right operand and pops them back. Ternary operator recursively pushes frames from its test when verifying the consequent expression and pops them back.
- [x] Few statements should push any frames recursively from condition expressions when verifying their bodies and pop back. This is done only for `if (test)`, `while (test)` and `for (; test;)`.

### Not Implemented

- [ ] Generic Type Inference; e.g. `a.map(...);` rather than `a.map.<R>(...);`.
- [ ] `o?.k > 0 ? x : y`: using optional access in a condition

### Done

- [x] Enum definitions "override" the `valueOf()` and `toString()` methods.
- [x] `typeExp.<>`: verify constraints
- [x] `exp.<>`: verify constraints
- [x] `v as T` or `v as? T` DO NOT turn `T` into `T?`; instead, verification for such conversion produces a `ConversionValue` with `isOptional` set to `true`, and `TypeConversions.convertExploicit(...)` will properly wrap `T` into `T?` if needed.
- [x] Instance methods can be overriden with additional optional parameters and a contravariant return type or any return type if the original return type is any.
- [x] Prohibit defining writable instance (non `static`) variables for `[Value]` classes.