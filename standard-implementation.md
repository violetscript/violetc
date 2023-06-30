# Standard Objects Implementation

## IMPORTANT: DontInit

**IMPORTANT:** Never forget to add the `DontInit` decorator to native classes.

This may be not necessary for some specific types though as codegen can be specialized for native classes, but that's not really necessary. `Map`s, structural records, flags and user types will work fine for object initialiser.

## IMPORTANT: FFI

- **IMPORTANT:** Pointers in native non-final classes must be stored as Rust `u64` in memory because of different platforms.
- **IMPORTANT:** Every `FFI(memorySize)` must account for the `constructor` property from `Object`, which is at offset 0. It is a pointer (in Rust's `u64`).
- **IMPORTANT:** Everytime the memory size of a non-final native class changes, update its `FFI(memorySize)` decorator to reflect it, otherwise it may corrupt the object and cause anything of wrong. That usually won't happen because classes will internally wrap their fields into a heap object. It should usually be like `constructorUint64Pointer + base1Uint64Pointer + ...baseNUint64Pointer + currentBaseUint64Pointer`.
- **IMPORTANT:** To avoid having to change `FFI`, wrap the object base fields into a heap object internally (thus the base will be constructor + po).

## RegExp

- Based on Unicode Code Points
- Sticky flag: https://gist.github.com/hydroper/c41b0186885af8b3b667846f579782c7

## Promise

- Consult https://gist.github.com/hydroper/1e1f940bb455a0264f0ee1100e9de4cb for properly implementing it.
- Static methods such as `race()` should take an iterable instead of array as opposed to the above ActionScript version.