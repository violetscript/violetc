package;

/**
 * Reserved interface for futurely supporting custom
 * text matching patterns.
 */
public interface ITextPattern {
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
}

public final class RegExp implements ITextPattern {
}