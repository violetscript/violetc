# .wasm Target

Refer to cpp-target for some notes and port them here later.

Use https://github.com/RyanLamansky/dotnet-webassembly to write the .wasm binary.

## Branching

- .wasm's `br` instruction, when applied to a block, goes to its end; when applied to a loop, goes to its beginning. The label 0 refers to the innermost scope.
  - All loops must be wrapped by a block so that `break` is possible.
- Label stack (of string or null for unlabeled) and iterator statement stack. Breaking a loop should be a `br` op of its .wasm scope index plus one (the surrounding block for the loop). If `break` or `continue` have no label, they affect the innermost loop.

## .wasm Imports

- Q: https://github.com/WebAssembly/design/issues/1481

## Class Inheritance

- _Native classes:_ Inheriting a native class requires super constructor to only receive the previously allocated object. This is not all though: how much has to be allocated? And what will be the offset of the subtype fields? Well, this will be done specifically through the `[FFI]` decorator. Find some good syntax for expressing memory layout. Maybe `[FFI]` can just specify the size of the native class, so that subclasses use memory after that size.
  - _Known native non-final classes:_
    - `TextMatch`
    - `Error`