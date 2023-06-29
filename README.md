# VioletScript Compiler

[VioletScript](https://violetscript.github.io) compiler implemented in .NET. Currently it does not generate any code; code generation will be worked on anytime, among other things. You can currently use it to type check.

- Debug type checker with `dotnet run -- someScript`

Current goals:

- Planning basic standard objects:
  - [ ] Map and weak map (collections)
  - [ ] Set and weak set (collections)
  - [ ] Regex
  - [ ] Promise
  - [ ] Errors
  - [ ] Reflect
  - [ ] Math
- VioletDoc HTML generated docs
- Standard objects implementation in C++ (many things will be unimplemented too at first, probably `Intl`...)
- Transpile to C++

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

C++.

[Ideas noted.](cpp-target.md)