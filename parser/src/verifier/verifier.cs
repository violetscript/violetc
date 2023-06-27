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
    /// <summary>
    /// Allows duplicate definitions. Used internally
    /// for standard built-in objects only.
    /// </summary>
    public bool AllowDuplicates = false;
}

public partial class Verifier
{
    private ModelCore m_ModelCore = new ModelCore();
    private VerifierOptions m_Options = new VerifierOptions();
    private Symbol m_Frame = null;
    private Stack<Symbol> m_MethodSlotStack = new Stack<Symbol>();
    private bool m_Valid = true;

    private Stack<List<Ast.Statement>> m_ImportOrAliasDirectivesStack = new Stack<List<Ast.Statement>>();

    private Stack<Ast.ProgramStrictnessFlags> m_StrictnessFlags = new Stack<Ast.ProgramStrictnessFlags>();

    /// <summary>
    /// List of import directives, use namespace directives,
    /// namespace aliases and type aliases.
    /// This list is consumed by <c>VerifyPrograms</c>.
    /// This is equivalent to <c>m_ImportOrAliasDirectivesStack.Peek()</c>.
    /// </summary>
    private List<Ast.Statement> m_ImportOrAliasDirectives
    {
        get => m_ImportOrAliasDirectivesStack.Peek();
    }

    /// <summary>
    /// List of type expressions with arguments.
    /// This list is feed by the <c>VerifyTypeExp</c> method and later consumed by
    /// <c>VerifyPrograms</c>.
    /// </summary>
    private List<Ast.TypeExpressionWithArguments> m_TypeExpsWithArguments = null;

    private Symbol CurrentMethodSlot
    {
        get => m_MethodSlotStack.Count() != 0 ? m_MethodSlotStack.Peek() : null;
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

    public Verifier()
    {
        this.m_StrictnessFlags.Push(0);
    }

    private Diagnostic SyntaxError(Script script, int id, Span span, DiagnosticArguments args = null)
    {
        script ??= span.Script;
        m_Valid = false;
        return script.CollectDiagnostic(Diagnostic.SyntaxError(id, span, args));
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

    private void EnterFrame(Symbol frame)
    {
        frame.ParentFrame ??= m_Frame;
        m_Frame = frame;
    }

    private void EnterFrames(List<Symbol> frames)
    {
        foreach (var frame in frames)
        {
            EnterFrame(frame);
        }
    }

    private void EnterFrames(Symbol[] frames)
    {
        foreach (var frame in frames)
        {
            EnterFrame(frame);
        }
    }

    private void ExitFrame()
    {
        m_Frame = m_Frame?.ParentFrame;
    }

    private void ExitNFrames(int n)
    {
        for (int i = 0; i < n; ++i)
        {
            ExitFrame();
        }
    }
}