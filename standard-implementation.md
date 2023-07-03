# Standard Objects Implementation

## IMPORTANT: DontInit

**IMPORTANT:** Never forget to add the `DontInit` decorator to native classes.

This may be not necessary for some specific types though as codegen can be specialized for native classes, but that's not really necessary. `Map`s, structural records, flags and user types will work fine for object initialiser.

## IMPORTANT: FFI

- **IMPORTANT:** Pointers in native non-final classes must be stored as Rust `u64` in memory because of different platforms.
- **IMPORTANT:** Non-final native classes must specify `FFI(memorySize)`. `Object` already specifies a pointer (`u64`) at the offset 0 for the `constructor` property, so they've to be implemented taking that into consideration.
  - `FFI(memorySize)` specifies the bytes used by the _exact_ type; bytes from supertypes are specified by themselves. This is useful to avoid breaking. As clarified in this document, `Object` already specifies 8 bytes for the `constructor` property.
- **IMPORTANT:** Everytime the memory size of a non-final native class changes, update its `FFI(memorySize)` decorator to reflect it, otherwise it may corrupt the object and cause anything of wrong. That usually won't happen because classes will internally wrap their fields into a heap object.
- **IMPORTANT:** To avoid having to change `FFI`, wrap the object base fields into a heap object internally.

## RegExp

- Based on Unicode Code Points
- Sticky flag: https://gist.github.com/hydroper/c41b0186885af8b3b667846f579782c7

## Promise

- Consult https://gist.github.com/hydroper/1e1f940bb455a0264f0ee1100e9de4cb for properly implementing it.
- Static methods such as `race()` should take an iterable instead of array as opposed to the above ActionScript version.