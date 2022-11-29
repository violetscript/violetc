namespace VioletScript.Parser.Source;

using System.Collections.Generic;
using VioletScript.Parser.Problem;

public sealed class Script {
    string m_FilePath;
    string m_Content;
    bool m_Valid = true;
    List<int> m_LineStarts = new List<int>();
    List<Script> m_IncludesScripts = new List<Script>();
    List<Problem> m_Problems = new List<Problem>();
    List<Comment> m_Comments = new List<Comment>();

    public Script(string filePath, string content) {
        m_FilePath = filePath;
        m_Content = content;
        m_LineStarts.Add(0);
        m_LineStarts.Add(0);
    }

    public string FilePath {
        get => m_FilePath;
    }

    public string Content {
        get => m_Content;
    }

    public bool IsValid {
        get => m_Valid;
    }

    public void AddLineStart(int index) {
        m_LineStarts.Add(index);
    }

    public int GetLineStart(int lineNumber) {
        if (lineNumber >= m_LineStarts.Count) {
            return m_LineStarts[m_LineStarts.Count - 1];
        }
        return m_LineStarts[lineNumber];
    }

    public int GetLineIndent(int lineNumber) {
        int lineStart = GetLineStart(lineNumber);
        int i = 0;
        int l = m_Content.Count();
        for (; i < l; ++i) {
            if (!SourceCharacter.IsWhitespace(m_Content[i])) {
                break;
            }
        }
        return i - lineStart;
    }

    public List<Script> IncludesScripts {
        get => m_IncludesScripts.GetRange(0, m_IncludesScripts.Count);
    }

    public void AddIncludedScript(Script script) {
        m_IncludesScripts.Add(script);
    }

    public List<Comment> Comments {
        get => m_Comments.GetRange(0, m_Comments.Count);
    }

    public void AddComment(Comment c) {
        m_Comments.Add(c);
    }

    public List<Problem> Problems {
        get => m_Problems.GetRange(0, m_Problems.Count);
    }

    public Problem CollectProblem(Problem p) {
        m_Valid = p.IsWarning ? m_Valid : false;
        m_Problems.Add(p);
        return p;
    }

    public void SortProblems() {
        m_Problems.Sort((a, b) => a.Span.CompareTo(b.Span));
        foreach (var s in m_IncludesScripts) {
            s.SortProblems();
        }
    }
}