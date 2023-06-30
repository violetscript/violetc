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

public class TextMatch {
    public var index: Int;
    public var input: String;

    /**
     * Entire match string.
     */
    public var match: String;

    /**
     * Match capture strings.
     */
    public var captures: [String] = [];

    /**
     * Named capturing groups.
     */
    public var groups: Map.<String, String>?;

    /**
     * Array where each entry represents the bounds
     * of a substring match. The first element represents
     * bounds of the entire match.
     */
    public var indices: [{start: Int, end: Int}]?;

    proxy function getIndex(index: Int): String (
        index == 0 ? this.match : index < 0 ? '' : (this.captures[index - 1] ?? '')!
    );
}

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
public final class RegExp implements ITextPattern {
    /**
     * @throws {SyntaxError}
     */
    public native function RegExp(pattern: String, flags: String = '');

    public native function get source(): String;
    public native function get flags(): String;
    public native function get lastIndex(): Int;
    public native function set lastIndex(value);

    public native function get global(): Boolean;
    public native function get ignoreCase(): Boolean;
    public native function get multiline(): Boolean;
    public native function get ignoreWhitespace(): Boolean;
    public native function get sticky(): Boolean;
    public native function get hasIndices(): Boolean;

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