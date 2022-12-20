namespace VioletScript.Parser.Verifier;

using System.Collections.Generic;
using Ast = VioletScript.Parser.Ast;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Problem;
using VioletScript.Parser.Semantic.Logic;
using VioletScript.Parser.Semantic.Model;
using VioletScript.Parser.Source;

using ProblemVars = Dictionary<string, object>;

public class VerifierOptions {
    public bool AllowDuplicates = false;
}

public class VerifierContext {
    public Symbol ExpectedType = null;
    public DefinitionPhase DefinitionPhase = DefinitionPhase.Initial;
}

public enum DefinitionPhase {
    Initial,
}

public partial class Verifier {
    private ModelCore m_ModelCore = new ModelCore();
    private VerifierOptions m_Options = new VerifierOptions();
    private Symbol m_Frame = null;
    private Stack<Symbol> m_FunctionStack = new Stack<Symbol>();
    private bool m_Valid = true;

    private Symbol CurrentFunction {
        get => m_FunctionStack.Count != 0 ? m_FunctionStack.Peek() : null;
    }

    public bool AllProgramsAreValid {
        get => m_Valid;
    }

    public VerifierOptions Options {
        get => m_Options;
    }

    public ModelCore ModelCore {
        get => m_ModelCore;
    }

    private Problem VerifyError(Script script, int id, Span span, ProblemVars args = null) {
        m_Valid = false;
        return script.CollectProblem(Problem.VerifyError(id, span, args));
    }

    private Problem Warn(Script script, int id, Span span, ProblemVars args = null) {
        return script.CollectProblem(Problem.Warning(id, span, args));
    }
}