# VioletScript Compiler

[VioletScript](https://violetscript.github.io) compiler implemented in .NET. Currently it does not generate any code; code generation will be worked on anytime. You can currently use it to type check.

Update: the type checker is done! I'll fix any possible minor bugs and probably work on other areas. Bugs to fix and things to-do:

- Debug with `dotnet run -- foo`
- [ ] Fix current exception (related to set of direct subtypes)
- [ ] Finish the standard built-in objects sources after solving all bugs.

## Command

VioletScript uses the extension `.vs`, which is automatically added.

```
violetc index
```