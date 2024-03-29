namespace VioletScript.Parser.Source;

using System.Collections.Generic;
using VioletScript.Parser.Diagnostic;

public sealed class Script {
    private string m_FilePath;
    private string m_Content;
    private bool m_Valid = true;
    private List<int> m_LineStarts = new List<int>();
    private List<Script> m_IncludesScripts = new List<Script>();
    private List<Diagnostic> m_Diagnostics = new List<Diagnostic>();
    private List<Comment> m_Comments = new List<Comment>();

    public Script(string filePath, string content) {
        m_FilePath = filePath == "" ? "" : Path.GetFullPath(filePath);
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

    public void Invalidate() {
        m_Valid = false;
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

    public bool IncludesScriptByFilePath(string filePath) =>
        this.FilePath == filePath || m_IncludesScripts.Any(script => script.IncludesScriptByFilePath(filePath));

    public void AddIncludedScript(Script script) {
        m_IncludesScripts.Add(script);
    }

    public List<Comment> Comments {
        get => m_Comments.GetRange(0, m_Comments.Count);
    }

    public void AddComment(Comment c) {
        m_Comments.Add(c);
    }

    public List<Diagnostic> Diagnostics {
        get => m_Diagnostics.GetRange(0, m_Diagnostics.Count);
    }

    public List<Diagnostic> DiagnosticsFromIncludedScripts {
        get {
            var r = new List<Diagnostic>();
            foreach (var incScript in this.m_IncludesScripts) {
                r.AddRange(incScript.DiagnosticsFromIncludedScripts);
                r.AddRange(incScript.Diagnostics);
            }
            return r;
        }
    }

    public List<Diagnostic> DiagnosticsFromCurrentAndIncludedScripts {
        get {
            var r = this.DiagnosticsFromIncludedScripts;
            r.AddRange(this.Diagnostics);
            return r;
        }
    }

    public Diagnostic CollectDiagnostic(Diagnostic p) {
        m_Valid = p.IsWarning ? m_Valid : false;
        m_Diagnostics.Add(p);
        return p;
    }

    public void SortDiagnostics() {
        m_Diagnostics.Sort((a, b) => a.Span.CompareTo(b.Span));
    }

    public void SortDiagnosticsForIncludedScripts() {
        m_Diagnostics.Sort((a, b) => a.Span.CompareTo(b.Span));
        foreach (var s in m_IncludesScripts) {
            s.SortDiagnostics();
            s.SortDiagnosticsForIncludedScripts();
        }
    }
}