# AST tasks

Use this document for tracking nodes to visit.

- [ ] Type expressions
  - [ ] Identifier
  - [ ] Any
  - [ ] Void
  - [ ] Undefined
  - [ ] Null
  - [ ] Function
  - [ ] Array
  - [ ] Tuple
  - [ ] Record
  - [ ] Parentheses
  - [ ] Union
  - [ ] Member
  - [ ] Generic instantiation
  - [ ] Nullable
  - [ ] Non-nullable
  - [ ] Typed type expression
- [ ] Variable binding
- [ ] Simple variable declaration
- [ ] Destructuring patterns
  - [ ] Non-destructuring
  - [ ] Array
  - [ ] Object
- [ ] Expressions
  - [ ] Identifier
    - Possibly typed
  - [ ] `import.meta`
  - [ ] Embed
  - [ ] Unary
    - [ ] Non-null
    - [ ] Post-incremenet
    - [ ] Post-decrement
  - [ ] Binary
  - [ ] Type binary (`as is instanceof`)
  - [ ] Default
  - [ ] Function
  - [ ] Object initializer
  - [ ] Array initializer
  - [ ] Node initializer
  - [ ] Node list initializer
  - [ ] Member
    - [ ] Possibly `?.x`
  - [ ] Index
    - [ ] Possibly `?.[k]`
  - [ ] Call
    - [ ] Possibly `?.()`
  - [ ] This literal
  - [ ] String literal
  - [ ] Null literal
  - [ ] Boolean literal
  - [ ] Numeric literal
  - [ ] `RegExp` literal
  - [ ] Conditional
  - [ ] Parentheses
  - [ ] List
  - [ ] Generic instantiation
  - [ ] Assignment
  - [ ] New
  - [ ] Super
- [ ] Program
- [ ] Package definitions
- [ ] Generics
- [ ] Statements
  - [ ] Declarations
    - [ ] Namespaces
    - [ ] Namespace alias
    - [ ] Variables
    - [ ] Functions
      - [ ] Constructor (`Ast.ConstructorDefinition`)
      - [ ] Proxy (`Ast.ProxyDefinition`)
      - [ ] Getter (`Ast.GetterDefinition`)
      - [ ] Setter (`Ast.SetterDefinition`)
    - [ ] Classes
    - [ ] Interfaces
    - [ ] Enums
    - [ ] Types
  - [ ] Expression statement
  - [ ] Empty statement
  - [ ] Block
  - [ ] Super statement
  - [ ] Import
  - [ ] If
  - [ ] Do
  - [ ] While
  - [ ] Break
  - [ ] Continue
  - [ ] Return
  - [ ] Throw
  - [ ] Try
  - [ ] Labeled statement
  - [ ] For
  - [ ] `for..in`
  - [ ] `for each`
  - [ ] Switch
  - [ ] `switch type`
  - [ ] Include
  - [ ] `use namespace`
  - [ ] `use resource`
  - [ ] With
