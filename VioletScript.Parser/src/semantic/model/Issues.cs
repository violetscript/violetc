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