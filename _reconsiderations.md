# Reconsiderations

## Symbol Model and Solving

In a potential next compiler, everything "symbolic" should be resolved asynchronously, including some logic of the symbol model, like generic type replacement.

The symbol model should use special placeholder symbols to indicate that something is still unknown, like:

- The class from which another one inherits.