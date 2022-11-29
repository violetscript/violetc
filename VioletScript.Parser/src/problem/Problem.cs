namespace VioletScript.Parser.Problem;

using VioletScript.Parser.Source;
using System.Collections.Generic;
using ProblemVars = Dictionary<string, object>;

public sealed class Problem {
    public int Id;
    public ProblemKind Kind;
    /// <summary>Span.</summary>
    public Span Span;
    public ProblemVars FormatArguments;

    public Problem(int id, ProblemKind kind, Span span, ProblemVars formatArguments) {
        Id = id;
        Kind = kind;
        Span = span;
        FormatArguments = formatArguments;
    }

    public static Problem Warning(int id, Span span, ProblemVars formatArguments = null) {
        return new Problem(id, ProblemKind.Warning, span, formatArguments ?? new ProblemVars {});
    }

    public static Problem SyntaxError(int id, Span span, ProblemVars formatArguments = null) {
        return new Problem(id, ProblemKind.SyntaxError, span, formatArguments ?? new ProblemVars {});
    }

    public static Problem VerifyError(int id, Span span, ProblemVars formatArguments = null) {
        return new Problem(id, ProblemKind.VerifyError, span, formatArguments ?? new ProblemVars {});
    }

    public bool IsWarning {
        get => this.Kind == ProblemKind.Warning;
    }

    public string KindString {
        get => this.Kind == ProblemKind.Warning ? "Warning"
            : this.Kind == ProblemKind.SyntaxError ? "SyntaxError"
            : "VerifyError";
    }

    public override string ToString() {
        return DefaultProblemFormatterStatics.Formatter.Format(this);
    }
}

public enum ProblemKind {
    Warning,
    SyntaxError,
    VerifyError,
}