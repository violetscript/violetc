namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

/// <summary>
/// Represents a lexical scope.
/// </summary>
public class Frame : Symbol {
    private Properties m_Properties = new Properties();
    private List<Symbol> m_OpenNamespaces = new List<Symbol>{};
    private Symbol m_ParentFrame = null;

    public override Symbol ParentFrame {
        get => m_ParentFrame;
        set => m_ParentFrame = value;
    }

    public override Properties Properties {
        get => m_Properties;
    }

    public override List<Symbol> OpenNamespaces {
        get => m_OpenNamespaces;
    }

    public override void OpenNamespace(Symbol ns) {
        if (!m_OpenNamespaces.Contains(ns)) {
            m_OpenNamespaces.Add(ns);
        }
    }
}

public class ActivationFrame : Frame {
    private Dictionary<Symbol, bool> m_ExtendedLifeVariables = null;
    private Symbol m_ActivationThisOrThisAsStaticType = null;

    public override Symbol ActivationThisOrThisAsStaticType {
        get => m_ActivationThisOrThisAsStaticType;
        set => m_ActivationThisOrThisAsStaticType = value;
    }

    public override bool VariableHasExtendedLife(Symbol slot) {
        return m_ExtendedLifeVariables.ContainsKey(slot);
    }

    public override void AddExtendedLifeVariable(Symbol slot) {
        m_ExtendedLifeVariables ??= new Dictionary<Symbol, bool>{};
        m_ExtendedLifeVariables[slot] = true;
    }
}

public class ClassFrame : Frame {
    private Symbol m_Type;

    public ClassFrame(Symbol type) {
        m_Type = type;
    }

    public override Symbol TypeFromFrame {
        get => m_Type;
    }
}

public class EnumFrame : Frame {
    private Symbol m_Type;

    public EnumFrame(Symbol type) {
        m_Type = type;
    }

    public override Symbol TypeFromFrame {
        get => m_Type;
    }
}

public class InterfaceFrame : Frame {
    private Symbol m_Type;

    public InterfaceFrame(Symbol type) {
        m_Type = type;
    }

    public override Symbol TypeFromFrame {
        get => m_Type;
    }
}

public class NamespaceFrame : Frame {
    private Symbol m_NS;

    public NamespaceFrame(Symbol ns) {
        m_NS = ns;
    }

    public override Symbol NamespaceFromFrame {
        get => m_NS;
    }
}

public class PackageFrame : Frame {
    private Symbol m_P;

    public PackageFrame(Symbol package) {
        m_P = package;
    }

    public override Symbol PackageFromFrame {
        get => m_P;
    }
}

public class WithFrame : Frame {
    private Symbol m_Object;

    public WithFrame(Symbol @object) {
        m_Object = @object;
    }

    public override Symbol ObjectFromWithFrame {
        get => m_Object;
    }
}