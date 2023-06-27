namespace VioletScript.Parser.Diagnostic;

using VioletScript.Parser.Source;
using System.Collections.Generic;
using System.IO;
using DiagnosticArguments = Dictionary<string, object>;

public sealed class Diagnostic {
    public int Id;
    public DiagnosticKind Kind;
    /// <summary>Span.</summary>
    public Span Span;
    public DiagnosticArguments FormatArguments;

    public Diagnostic(int id, DiagnosticKind kind, Span span, DiagnosticArguments formatArguments) {
        Id = id;
        Kind = kind;
        Span = span;
        FormatArguments = formatArguments;
    }

    public static Diagnostic Warning(int id, Span span, DiagnosticArguments formatArguments = null) {
        return new Diagnostic(id, DiagnosticKind.Warning, span, formatArguments ?? new DiagnosticArguments {});
    }

    public static Diagnostic SyntaxError(int id, Span span, DiagnosticArguments formatArguments = null) {
        return new Diagnostic(id, DiagnosticKind.SyntaxError, span, formatArguments ?? new DiagnosticArguments {});
    }

    public static Diagnostic VerifyError(int id, Span span, DiagnosticArguments formatArguments = null) {
        return new Diagnostic(id, DiagnosticKind.VerifyError, span, formatArguments ?? new DiagnosticArguments {});
    }

    public bool IsWarning {
        get => this.Kind == DiagnosticKind.Warning;
    }

    public string KindString {
        get => this.Kind == DiagnosticKind.Warning ? "Warning"
            : this.Kind == DiagnosticKind.SyntaxError ? "SyntaxError"
            : "VerifyError";
    }

    public override string ToString() {
        return DefaultDiagnosticFormatterStatics.Formatter.Format(this);
    }

    public string ToRelativeString(string basePath) {
        return DefaultDiagnosticFormatterStatics.Formatter.FormatRelative(this, basePath);
    }
}

public enum DiagnosticKind {
    Warning,
    SyntaxError,
    VerifyError,
}