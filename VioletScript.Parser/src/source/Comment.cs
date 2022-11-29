namespace VioletScript.Parser.Source;

using System.Collections.Generic;

/// <summary>
/// Comment. The <c>Content</c> does not include the delimiters <c>//</c>,
/// <c>/*</c> and <c>*/</c>.
/// </summary>
public sealed class Comment {
    public bool MultiLine;
    public string Content;
    public Span Span;

    public Comment(bool multiLine, string content, Span span) {
        MultiLine = multiLine;
        Content = content;
        Span = span;
    }
}