# .wasm Target

Refer to cpp-target for some notes and port them here later.

Use https://github.com/RyanLamansky/dotnet-webassembly to write the .wasm binary.

## Branching

- .wasm's `br` instruction, when applied to a block, goes to its end; when applied to a loop, goes to its beginning. The label 0 refers to the innermost scope.
  - All loops must be wrapped by a block so that `break` is possible.