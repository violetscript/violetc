# To Do

- Improvements:
  - [ ] Add a warning if a `switch` does not match all variants of an enum.
  - [ ] Add a verify error if a native class doesn't specify its `memorySize`.
  - [ ] Address what are "native" classes. `native` methods exist, but wouldn't it make sense to add the `native` modifier to a class too?
    - Idea: if a class contains any `native` method, getter or setter, it is native (do that check directly on the parser and mark it as `native` if any); if a class contains the `native` modifier, it is native. It should be easy to allow this `native` modifier! In that case, document it too.
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

## Codegen Goal

WebAssembly.

[Ideas noted.](./wasm-target.md)

## Standard Objects Implementation

[Ideas noted.](./standard-implementation)