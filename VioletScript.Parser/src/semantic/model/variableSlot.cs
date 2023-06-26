namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public class VariableSlot : Symbol {
}

public class NormalVariableSlot : VariableSlot {
    private string m_Name;
    private Symbol m_Type = null;
    private bool m_ReadOnly;
    private Symbol m_InitValue = null;
    private Visibility m_Visibility = Visibility.Internal;
    private Symbol m_Parent = null;
    private bool m_AllowsShadowing = false;

    public NormalVariableSlot(string name, bool readOnly, Symbol staticType) {
        m_Name = name;
        m_ReadOnly = readOnly;
        m_Type = staticType;
    }

    public override Symbol StaticType {
        get => m_Type;
        set => m_Type = value;
    }

    public override bool ReadOnly {
        get => m_ReadOnly;
        set => m_ReadOnly = value;
    }

    public override Symbol InitValue {
        get => m_InitValue;
        set => m_InitValue = value;
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

    public override bool AllowsShadowing {
        get => m_AllowsShadowing;
        set {
            m_AllowsShadowing = value;
        }
    }
}

public class VariableSlotFromTypeWithArgs : VariableSlot {
    private Symbol[] m_ParamTypeTParams;
    private Symbol[] m_ParamTypeArgumentsList;
    private Symbol m_Origin;
    private Symbol m_Type;

    public VariableSlotFromTypeWithArgs(Symbol[] paramTypeTParams, Symbol[] paramTypeArgs, Symbol origin, Symbol type) {
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

    public override string FullyQualifiedName {
        get => m_Origin.FullyQualifiedName;
    }

    public override Symbol StaticType {
        get => m_Type;
    }

    public override bool ReadOnly {
        get => m_Origin.ReadOnly;
    }

    public override string Name {
        get => m_Origin.Name;
    }

    public override Visibility Visibility {
        get => m_Origin.Visibility;
    }

    public override string ToString() {
        return FullyQualifiedName;
    }
}