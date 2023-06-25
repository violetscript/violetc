# VioletScript Compiler

[VioletScript](https://violetscript.github.io) compiler implemented in .NET. Currently it does not generate any code; code generation will be worked on anytime. You can currently use it to type check.

Update: the type checker is done! I'll write the command as soon as possible to fix any possible minor bugs and probably work on other areas.

## Command

VioletScript uses the extension `.vs`, which is automatically added.

```
violetc index
```

## Tasks

- [ ] When packaging the compiler, it must ship with the built-in standard object sources.