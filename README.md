# VioletScript Compiler

[VioletScript](https://violetscript.github.io) compiler implemented in .NET. Currently it does not generate any code; code generation will be worked on anytime. You can currently use it to type check.

- Debug it with `dotnet run -- someScript`
- Currently writing built-ins for the primitive String type
- [ ] Currrent bug to fix: type substitution of generic method under a generic class is not replacing signature correctly

## Command

VioletScript uses the extension `.vs`, which is automatically added.

```
violetc index
```

## Known Bugs and Limitations

- Extending or implementing a generic type before it's defined may cause a .NET exception due to how the compiler resolves types. In the moment just define them in an order that makes sense.
- You can do `o?.x`, but not yet chain it like `o?.f()` or `o?.x.y`.
- Type arguments are not inferred (`C.<T>` vs `C` or `fn.<T>()` vs `fn()`).