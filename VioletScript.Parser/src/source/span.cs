namespace VioletScript.Parser.Source;

using System;

/// <summary>Span.</summary>
public struct Span : IComparable<Span> {
    private Script m_Script;
    private int m_Start;
    private int m_End;
    private int m_FirstLine;
    private int m_LastLine;

    public Span(Script script, int firstLine, int lastLine, int start, int end) {
        m_Script = script;
        m_FirstLine = firstLine;
        m_LastLine = lastLine;
        m_Start = start;
        m_End = end;
    }

    public static Span WithLinesAndIndexes(Script script, int firstLine, int lastLine, int start, int end) {
        return new Span(script, firstLine, lastLine, start, end);
    }

    public static Span Inline(Script script, int line, int start, int end) {
        return WithLinesAndIndexes(script, line, line, start, end);
    }

    public static Span Point(Script script, int line, int index) {
        return Inline(script, line, index, index);
    }

    public Script Script {
        get => m_Script;
    }

    /// <summary>Start index.</summary>
    public int Start {
        get => m_Start;
    }

    /// <summary>End index.</summary>
    public int End {
        get => m_End;
    }

    /// <summary>First line. Starts at `1`.</summary>
    public int FirstLine {
        get => m_FirstLine;
    }

    /// <summary>Last line. Starts at `1`.</summary>
    public int LastLine {
        get => m_LastLine;
    }

    /// <summary>Zero-based first column.</summary>
    public int FirstColumn {
        get => m_Start - m_Script.GetLineStart(m_FirstLine);
    }

    /// <summary>Zero-based last column.</summary>
    public int LastColumn {
        get => m_End - m_Script.GetLineStart(m_LastLine);
    }

    /// <summary>Combines two `Span`s into one, going from `self` to `other`.</summary>
    public Span To(Span other) {
        return WithLinesAndIndexes(m_Script, m_FirstLine, other.m_LastLine, m_Start, other.m_End);
    }

    public int CompareTo(Span other) {
        return m_Start < other.m_Start ? -1 : m_Start == other.m_Start ? 0 : 1;
    }
}