# VioletScript Compiler

[VioletScript](https://violetscript.github.io) compiler implemented in .NET. Currently it does not generate any code; code generation will be worked on anytime, among other things. You can currently use it to type check.

- Debug type checker with `dotnet run -- someScript`

Current goals:

- Planning basic standard objects:
  - [ ] Reflect (finishing type meta-objects)
  - [ ] Math
  - [ ] Observable
- VioletDoc HTML generated docs
  - Use Markdig for compiling the Markdown: https://github.com/xoofx/markdig
- Standard objects implementation in a systems language
  - [ ] Update `FFI(typeId)` of most native classes (0 and 1 are already used for `undefined` and `null`), including `Object`, primitives and most things.
  - [ ] Update `FFI(memorySize)` of native non-final classes. It will be usually `constructorPointer + basePointer` for a _root_ supertype, **_but_** `basePointer` for a non-root supertype (that is, constructor pointer belongs to root supertype).
    - [ ] `TextMatch`
    - [ ] `Error`
      - [ ] `AggregateError`
- Compile to .wasm

Future goals:

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