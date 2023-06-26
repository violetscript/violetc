# VioletScript Compiler

[VioletScript](https://violetscript.github.io) compiler implemented in .NET. Currently it does not generate any code; code generation will be worked on anytime. You can currently use it to type check.

- Debug it with `dotnet run -- someScript`
- [ ] Fixing bug: items imported from a package aren't visibly lexically in a type annotation from a variable definition.

## Command

VioletScript uses the extension `.vs`, which is automatically added.

```
violetc index
```