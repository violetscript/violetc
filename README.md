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