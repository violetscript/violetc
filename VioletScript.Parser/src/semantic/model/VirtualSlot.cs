namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public class VirtualSlot : Symbol {
}

public class NormalVirtualSlot : VirtualSlot {
    private string m_Name;
    private Symbol m_Type = null;
    private Visibility m_Visibility = Visibility.Internal;
    private Symbol m_Parent = null;
    private Symbol m_Getter;
    private Symbol m_Setter;

    public NormalVirtualSlot(string name, Symbol type) {
        m_Name = name;
        m_Type = type;
    }

    public override Symbol Getter {
        get => m_Getter;
        set => m_Getter = value;
    }

    public override Symbol Setter {
        get => m_Setter;
        set => m_Setter = value;
    }

    public override Symbol StaticType {
        get => m_Type;
        set => m_Type = value;
    }

    public override bool ReadOnly {
        get => m_Setter == null;
    }

    public override bool WriteOnly {
        get => m_Getter == null;
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

    public override string ToString() {
        return FullyQualifiedName;
    }

    public override bool IsInstantiated {
        get => false;
    }
}

public class InstantiatedVirtualSlot : VirtualSlot {
    private Symbol[] m_ParamTypeTParams;
    private Symbol[] m_ParamTypeArgumentsList;
    private Symbol m_Origin;
    private Symbol m_Type;
    private Symbol m_Getter = null;
    private Symbol m_Setter = null;

    public InstantiatedVirtualSlot(Symbol[] paramTypeTParams, Symbol[] paramTypeArgs, Symbol origin, Symbol type) {
        m_ParamTypeTParams = paramTypeTParams;
        m_ParamTypeArgumentsList = paramTypeArgs;
        m_Origin = origin;
        m_Type = type;
    }

    public override bool IsInstantiated {
        get => true;
    }

    public override Symbol[] TParamsFromRelatedParameterizedType {
        get => m_ParamTypeTParams;
    }

    public override Symbol[] ArgumentsToRelatedParameterizedType {
        get => m_ParamTypeArgumentsList;
    }

    public override Symbol OriginalDefinition {
        get => m_Origin;
    }

    public override Symbol StaticType {
        get => m_Type;
    }

    public override bool ReadOnly {
        get => m_Origin.Setter == null;
    }

    public override bool WriteOnly {
        get => m_Origin.Getter == null;
    }

    public override string Name {
        get => m_Origin.Name;
    }

    public override Visibility Visibility {
        get => m_Origin.Visibility;
    }

    public override Symbol Getter {
        get {
            if (m_Getter != null) {
                return m_Getter;
            }
            if (m_Origin.Getter == null) {
                return null;
            }
            m_Getter = m_Origin.Getter.ReplaceTypes(TParamsFromRelatedParameterizedType, ArgumentsToRelatedParameterizedType);
            return m_Getter;
        }
    }

    public override Symbol Setter {
        get {
            if (m_Setter != null) {
                return m_Setter;
            }
            if (m_Origin.Setter == null) {
                return null;
            }
            m_Setter = m_Origin.Setter.ReplaceTypes(TParamsFromRelatedParameterizedType, ArgumentsToRelatedParameterizedType);
            return m_Setter;
        }
    }

    public override string ToString() {
        return FullyQualifiedName;
    }
}