## C++ Plans

- _Call stack debug information:_ Source function names may be a little different even when debugging a .
  - I'm not sure a stack of debug information is safe as it can get incorrect after the exception is handled and program resumes using that same information stack.
- Generated structure names: things will be prefixed by default with some prefix (like `violet_`) unless using the `[FFI]` decorator to avoid name collision.
  - Certain objects won't generate structure and rather re-use one from C++ side. This will be the case for `Object` maybe.
- `Object` in the any type context (`*`) may also be used to represent `undefined` or `null`. They are not treated as objects; this is just a variant representation.
- Needed structures will be defined multiple times; for example:
  - Empty: `class violet_C1 : violet_Object; class violet_C2 : violet_Object;`
    - Sort these empty structures based on inherited class. The basemost class appears first as an empty structure.
  - Then fields and method signatures: `class violet_C1 : violet_Object { ... }; class violet_C2 : violet_Object { ... };`
    - If a field has a constant initial value and it's possible to express it in C++, generate an initialiser for it.
  - Then implementation: `class violet_C1 : violet_Object { ... }; class violet_C2 : violet_Object { ... };`
    - If a field has both a constant initial value, no initialiser and it was possible to express its constant initial value in C++, don't generate an assignment for it in the constructor.

## Exceptions

```
#include <cassert>
#include <exception>

class MyCustomException : public std::exception {
    public:
    char* what () {
        return "Message plus stacktrace";
    }
};

namespace com::q {
    struct C {
        #line 404 "index.vs"
        static void f(int i) {
            assert(false);
            // C++ prrogram doesn't format the thrown error,
            // so no message nor callstack.
            // so every entry, including asynchronous functions,
            // have to catch any Error object from VioletScript
            // and panic, displaying its what() properly.
            throw MyCustomException();
        }
    };
}

int main() {
#line 777 "index.vs"
    com::q::C::f(0);
}
```