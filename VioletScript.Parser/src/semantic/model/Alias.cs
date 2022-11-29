namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public class Alias : Symbol {
    private string m_Name;
    private Symbol m_To;
    private Visibility m_Visibility;
    private Symbol m_Parent;

    public Alias(string name, Symbol to) {
        m_Name = name;
        m_To = to;
    }

    public override string Name {
        get => m_Name;
    }

    public override Symbol AliasToSymbol {
        get => m_To;
    }

    public override Visibility Visibility {
        get => m_Visibility;
        set { m_Visibility = value; }
    }

    public override Symbol ParentDefinition {
        get => m_Parent;
        set { m_Parent = value; }
    }
}