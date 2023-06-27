namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public class Namespace : Symbol {
    private string m_Name;
    private Properties m_Properties = new Properties();
    private Visibility m_Visibility = Visibility.Internal;
    private Symbol m_Parent;

    public Namespace(string name) {
        m_Name = name;
    }

    public override string Name {
        get => m_Name;
    }

    public override Symbol ParentDefinition {
        get => m_Parent;
        set => m_Parent = value;
    }

    public override Properties Properties {
        get => m_Properties;
    }

    public override Visibility Visibility {
        get => m_Visibility;
        set => m_Visibility = value;
    }

    public override string ToString() {
        return FullyQualifiedName;
    }
}

public class Package : Namespace {
    private Dictionary<string, Symbol> m_Subpackages = new Dictionary<string, Symbol>{};

    public Package(string name) : base(name) {
        this.Visibility = Visibility.Public;
    }

    public override Dictionary<string, Symbol> Subpackages {
        get => new Dictionary<string, Symbol>(m_Subpackages);
    }

    public override void AddSubpackage(Symbol package) {
        m_Subpackages[package.Name] = package;
        package.ParentDefinition = this;
    }

    public override Symbol GetSubpackage(string name) {
        return m_Subpackages.ContainsKey(name) ? m_Subpackages[name] : null;
    }

    public override Symbol FindOrCreateDeepSubpackage(string[] dotDelimitedId) {
        Symbol r = this;
        for (int index = 0; index < dotDelimitedId.Count(); index += 1) {
            var sp = r.GetSubpackage(dotDelimitedId[index]);
            if (sp == null) {
                sp = ModelCore.Factory.Package(dotDelimitedId[index]);
                r.AddSubpackage(sp);
            }
            r = sp;
        }
        return r;
    }
}

public class NamespaceSet : Symbol {
    private string m_Name;
    private Symbol[] m_Set;
    private Visibility m_Visibility = Visibility.Internal;
    private Symbol m_Parent;

    public NamespaceSet(string name, Symbol[] set) {
        m_Name = name;
        m_Set = set;
    }

    public override string Name {
        get => m_Name;
    }

    public override Symbol ParentDefinition {
        get => m_Parent;
        set => m_Parent = value;
    }

    public override Visibility Visibility {
        get => m_Visibility;
        set => m_Visibility = value;
    }

    public override Symbol[] NamespaceSetItems {
        get => m_Set.ToArray();
    }

    public override string ToString() {
        return FullyQualifiedName;
    }
}