namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public class Issue : Symbol {
}

public class AmbiguousReferenceIssue : Symbol {
    private string m_Name;

    public AmbiguousReferenceIssue(string name) {
        m_Name = name;
    }

    public override string Name {
        get => m_Name;
    }
}

public class CannotOverrideGenericMethodIssue : Symbol {
    private string m_Name;

    public CannotOverrideGenericMethodIssue(string name) {
        m_Name = name;
    }

    public override string Name {
        get => m_Name;
    }
}

public class CannotOverrideFinalMethodIssue : Symbol {
    private string m_Name;

    public CannotOverrideFinalMethodIssue(string name) {
        m_Name = name;
    }

    public override string Name {
        get => m_Name;
    }
}

public class MustOverrideAMethodIssue : Symbol {
    private string m_Name;

    public MustOverrideAMethodIssue(string name) {
        m_Name = name;
    }

    public override string Name {
        get => m_Name;
    }
}

public class IncompatibleOverrideSignatureIssue : Symbol {
    private string m_Name;
    private Symbol m_Expected;

    public IncompatibleOverrideSignatureIssue(string name, Symbol expectedSignature) {
        m_Name = name;
        m_Expected = expectedSignature;
    }

    public override string Name {
        get => m_Name;
    }

    public override Symbol ExpectedSignature {
        get => m_Expected;
    }
}