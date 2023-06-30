package;

/**
 * Interface for text matching patterns.
 */
public interface ITextPattern {
    function match(input: String): TextMatch?;
    function matchAll(input: String): Iterator.<TextMatch>;
    function replace(input: String, replacement: TextReplacement): String;
    function search(input: String): Int;
    function split(input: String): [String];
}

[DontInit]
public class TextMatch {
    public native function TextMatch();

    public native function get index(): Int;
    public native function set index(value);

    public native function get input(): String;
    public native function set input(value);

    /**
     * Entire match string.
     */
    public native function get match(): String;
    public native function set match(value);

    /**
     * Match capture strings.
     */
    public native function get captures(): [String];
    public native function set captures(value);

    /**
     * Named capturing groups.
     */
    public native function get groups(): Map.<String, String>?;
    public native function set groups(value);

    /**
     * Array where each entry represents the bounds
     * of a substring match. The first element represents
     * bounds of the entire match.
     */
    public native function get indices(): [TextMatchBounds]?;
    public native function set indices(value);

    proxy function getIndex(index: Int): String (
        index == 0 ? this.match : index < 0 ? '' : (this.captures[index - 1] ?? '')!
    );
}

public type TextMatchBounds = {start: Int, end: Int};

/**
 * Regular expression for matching Code Point text patterns.
 *
 * # Supported Flags
 *
 * - `g`: `global`
 * - `i`: `ignoreCase`
 * - `m`: `multiline`
 * - `x`: `ignoreWhitespace`
 * - `y`: `sticky`
 * - `d`: `hasIndices`
 */
[DontInit]
public final class RegExp implements ITextPattern {
    /**
     * @throws {SyntaxError}
     */
    public native function RegExp(pattern: String, flags: String = '');

    // VioletScript's flags enums aren't used here so that
    // the API looks closer to JavaScript.

    public native function get source(): String;
    public native function get flags(): String;
    public native function get lastIndex(): Int;
    public native function set lastIndex(value);

    public native function get global(): Boolean;
    public native function get ignoreCase(): Boolean;
    public native function get multiline(): Boolean;
    public native function get sticky(): Boolean;
    public native function get hasIndices(): Boolean;

    /**
     * The `x` flag. If the `x` flag was specified, any whitespace,
     * lines and comments in the form `# ...` will be ignored
     * in the source pattern. This flag is used for readability.
     */
    public native function get ignoreWhitespace(): Boolean;

    public native function exec(input: String): TextMatch?;

    public function test(input: String): Boolean (
        this.exec(input) != null
    );

    /**
     * Returns a string representation of the `RegExp`, such as
     * `"/pattern/flags"`.
     */
    public override native function toString(): String;

    // # ITextPattern interface

    public native function match(input: String): TextMatch?;
    public function matchAll(input: String): Iterator.<TextMatch> {
        for (;;) {
            const match = this.match(input);
            if (match != null) {
                yield match!;
            }
        }
    }
    public native function replace(input: String, replacement: TextReplacement): String;
    public native function search(input: String): Int;
    public native function split(input: String): [String];
}