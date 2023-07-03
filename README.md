# VioletScript Compiler

[VioletScript](https://violetscript.github.io) compiler implemented in .NET. Currently it does not generate any code; code generation will be worked on anytime, among other things. You can currently use it to type check.

- Debug type checker with `dotnet run -- someScript`

Current goals:

- Improvements:
  - [ ] Add a verify error if a native class doesn't specify its `memorySize`.
  - [ ] Address what are "native" classes. `native` methods exist, but wouldn't it make sense to add the `native` modifier to a class too?
- Planning basic standard objects:
  - [ ] Reflect (finishing type meta-objects)
  - [ ] Math
  - [ ] Observable
- VioletDoc HTML generated docs
  - Use Markdig for compiling the Markdown: https://github.com/xoofx/markdig
- Standard objects implementation in a systems language
  - [ ] Update `FFI(typeId)` of most native classes (0 and 1 are already used for `undefined` and `null`), including `Object`, primitives and most things.
  - [ ] Update `FFI(memorySize)` of _**all**_ standard object classes (whether `final` or not). It will be usually 8 (pointer expressed as `u64`, that refers to a Rust `Box`).
    - [x] Object
    - [ ] Everything else
- Compile to .wasm

Future goals:

- Bindings Generation
- Package Manager
- Language Server Protocol
- Use with Godot Engine

## Command

VioletScript uses the extension `.vs`, which is automatically added.

```
violetc index
```

## Known Bugs and Limitations

- Extending or implementing a generic type before it's defined may cause a .NET exception due to how the compiler resolves types. In the moment just define them in an order that makes sense.
- Type arguments are not inferred (`C.<T>` vs `C` or `fn.<T>()` vs `fn()`).

## Codegen Goal

WebAssembly.

[Ideas noted.](./wasm-target.md)

## Standard Objects Implementation

[Ideas noted.](./standard-implementation)