# VioletScript Compiler

[VioletScript](https://violetscript.github.io/docs/language_overview/quick_tour.html) is a very fast version of JavaScript with a satisfactory type system and extensive syntax.

This is the compiler implemented in .NET. Currently it does not generate any code; code generation will be worked on anytime, among other things. You can currently use it to type check.

- Debug type checker with `dotnet run -- someScript`
- [To Do](./to-do.md)

## Command

VioletScript uses the extension `.vs`, which is automatically added.

```
violetc index
```

## Known Bugs and Limitations

- Extending or implementing a generic type before it's defined may cause a .NET exception due to how the compiler resolves types. In the moment just define them in an order that makes sense.
- Type arguments are not inferred (`C.<T>` vs `C` or `fn.<T>()` vs `fn()`).