# VioletScript Compiler

[VioletScript](https://violetscript.github.io) compiler implemented in .NET. Currently it does not generate any code; code generation will be worked on anytime. You can currently use it to type check.

Update: the type checker is done! I'll write the command as soon as possible to fix any possible minor bugs and probably work on other areas.

## Command

Any argument starting with `"builtins:"` is used to specify a source for the standard built-in objects. It will be resolved before the rest.

VioletScript uses the extension `.vs`, which is automatically added.

```
violetc "builtins:standard-objects" index
```

## Built-ins

When resolving the built-ins, it's allowed to duplicate definitions, so it's not recommended to use it for actual programs. It exists because of basic definitions on which the verification relies, such as `Object` and `Iterator`.