# VioletScript Compiler

[VioletScript](https://violetscript.github.io) compiler implemented in .NET. Currently it does not generate any code; code generation will be worked on anytime. You can currently use it to type check.

- Debug it with `dotnet run -- someScript`

## Command

VioletScript uses the extension `.vs`, which is automatically added.

```
violetc index
```

## Known Bugs and Limitations

- Extending or implementing a generic type before it's defined may cause a .NET exception due to how the compiler resolves types. In the moment just define them in an order that makes sense.
- Type arguments are not inferred (`C.<T>` vs `C` or `fn.<T>()` vs `fn()`).

## Codegen Goal

Probably C++. C++ supports exceptions, so it should be more straightforward, and I won't need to generate bytecode like WASM manually.

- _Call stack debug information:_ I probably need a stack of debug information containing call locations. It can't be program-static due to concurrency and code suspension, so I guess I could pass this debug stack to every function manually only when debugging a VioletScript program and, when you don't need it, an optimization flag can be used to disable this debug information stack and not pass it to functions and their calls.
  - I'm not sure a stack of debug information is safe as it can get incorrect after the exception is handled and program resumes using that same information stack.
- Generated structure names: things will be prefixed by default with some prefix (like `violet_`) unless using the `[FFI]` decorator to avoid name collision.
  - Certain objects won't generate structure and rather re-use one from C++ side. This will be the case for `Object` maybe.
- `Object` in the any type context (`*`) may also be used to represent `undefined` or `null`. They are not treated as objects; this is just a variant representation.
- Needed structures will be defined multiple times; for example:
  - Empty: `class violet_C1 : violet_Object; class violet_C2;`
  - Then fields and method signatures: `class violet_C1 { ... }; class violet_C2 { ... };`
    - If a field has a constant initial value and it's possible to express it in C++, generate an initialiser for it.
  - Then implementation: `class violet_C1 { ... }; class violet_C2 { ... };`
    - If a field has both a constant initial value, no initialiser and it was possible to express its constant initial value in C++, don't generate an assignment for it in the constructor.