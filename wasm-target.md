# .wasm Target

Refer to cpp-target for some notes and port them here later.

Use https://github.com/RyanLamansky/dotnet-webassembly to write the .wasm binary.

## Branching

- .wasm's `br` instruction, when applied to a block, goes to its end; when applied to a loop, goes to its beginning. The label 0 refers to the innermost scope.
  - All loops must be wrapped by a block so that `break` is possible.
- Label stack (of string or null for unlabeled) and iterator statement stack. Breaking a loop should be a `br` op of its .wasm scope index plus one (the surrounding block for the loop). If `break` or `continue` have no label, they affect the innermost loop.

## .wasm Imports

- Q: https://github.com/WebAssembly/design/issues/1481

## Classes

- It's not clear what is a native class. Address that question. Is it a class that includes definitiosn with `native` or add the same modifier to classes?
- `nativeSize` is inferred for non-native classes.
- _Native classes:_
  - Inheriting a native non-final class requires super constructor to only receive the previously allocated object.
  - `FFI(memorySize = n)` determines the memory size for the internal fields of the enclosing class (it covers the exact type, not the supertypes). Fields from subclasses use memory after that memory size plus `memorySize` of other supertypes (including `Object`).
- `new C1`, will be similiar to a C `malloc` taking the FFI `memorySize` (if the class is not native, `memorySize` is inferred) plus the FFI `memorySize` from supertypes (including `Object` if `C1` is not object) of `C1` and invoking its initialiser.

## Class Identifiers

Every type will have an _unique_ internal number id. Use `FFI(typeId = n)` for that.

Derive the following initially:

- 0 = undefined
- 1 = null

An exception must be thrown if it's a duplicate number. Types will get an id automatically, using a sorted counter for efficiency.

It's not yet confirmed if needed, but certain generic types such as array, may need a pre-assigned id. For example, for unions and dynamic contexts, although some implemented interfaces may already help on their usage.

## Ids of Arrays and Other Generic Types

If needed in the future, collect the id number of instantiations of generic types such as array and register them in the runtime statically, in case the runtime needs these.