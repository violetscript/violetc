namespace VioletScript.Parser.Verifier;

using System.Collections.Generic;
using Ast = VioletScript.Parser.Ast;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Diagnostic;
using VioletScript.Parser.Semantic.Logic;
using VioletScript.Parser.Semantic.Model;
using VioletScript.Parser.Source;

using DiagnosticArguments = Dictionary<string, object>;

public class VerifierOptions
{
    public bool AllowDuplicates = false;
}

public class VerifierContext
{
    public Symbol ExpectedType = null;
    public DefinitionPhase DefinitionPhase = DefinitionPhase.Initial;
}

public enum DefinitionPhase
{
    Initial,
}

public partial class Verifier
{
    private ModelCore m_ModelCore = new ModelCore();
    private VerifierOptions m_Options = new VerifierOptions();
    private Symbol m_Frame = null;
    private Stack<Symbol> m_FunctionStack = new Stack<Symbol>();
    private bool m_Valid = true;

    private Symbol CurrentFunction
    {
        get => m_FunctionStack.Count() != 0 ? m_FunctionStack.Peek() : null;
    }

    public bool AllProgramsAreValid
    {
        get => m_Valid;
    }

    public VerifierOptions Options
    {
        get => m_Options;
    }

    public ModelCore ModelCore
    {
        get => m_ModelCore;
    }

    private Diagnostic VerifyError(Script script, int id, Span span, DiagnosticArguments args = null)
    {
        script ??= span.Script;
        m_Valid = false;
        return script.CollectDiagnostic(Diagnostic.VerifyError(id, span, args));
    }

    private Diagnostic Warn(Script script, int id, Span span, DiagnosticArguments args = null)
    {
        script ??= span.Script;
        return script.CollectDiagnostic(Diagnostic.Warning(id, span, args));
    }
}