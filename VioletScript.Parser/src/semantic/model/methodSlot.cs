namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

[Flags]
public enum MethodSlotFlags {
    UsesYield,
    UsesAwait,
    Override,
    Final,
    Native,
    OptionalInterfaceMethod,
}

public class MethodSlot : Symbol {
}

public class NormalMethodSlot : MethodSlot {
    private string m_Name;
    private Symbol m_Type;
    private Visibility m_Visibility = Visibility.Internal;
    private Symbol m_Parent = null;
    private MethodSlotFlags m_Flags;
    private Symbol[] m_TypeParameters = null;
    private Symbol m_BelongsToVirtualProp = null;
    private List<Symbol> m_OverridenBy = null;

    public NormalMethodSlot(string name, Symbol type, MethodSlotFlags flags) {
        m_Name = name;
        m_Type = type;
        m_Flags = flags;
    }

    public override Symbol StaticType {
        get => m_Type;
        set => m_Type = value;
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

    public override MethodSlotFlags MethodFlags {
        get => m_Flags;
        set => m_Flags = value;
    }

    public override bool UsesYield {
        get => (m_Flags & MethodSlotFlags.UsesYield) != 0;
        set => m_Flags |= MethodSlotFlags.UsesYield;
    }

    public override bool UsesAwait {
        get => (m_Flags & MethodSlotFlags.UsesAwait) != 0;
        set => m_Flags |= MethodSlotFlags.UsesAwait;
    }

    public override bool HasOverrideModifier {
        get => (m_Flags & MethodSlotFlags.Override) != 0;
        set => m_Flags |= MethodSlotFlags.Override;
    }

    public override bool IsFinal {
        get => (m_Flags & MethodSlotFlags.Final) != 0;
        set => m_Flags |= MethodSlotFlags.Final;
    }

    public override bool IsNative {
        get => (m_Flags & MethodSlotFlags.Native) != 0;
        set => m_Flags |= MethodSlotFlags.Native;
    }

    public override Symbol[] TypeParameters {
        get => m_TypeParameters;
        set => m_TypeParameters = value;
    }

    public override Symbol BelongsToVirtualProperty {
        get => m_BelongsToVirtualProp;
        set => m_BelongsToVirtualProp = value;
    }

    public override Symbol[] MethodOverridenBy {
        get => m_OverridenBy != null ? m_OverridenBy.ToArray() : new Symbol[]{};
    }

    public override void AddMethodOverrider(Symbol method) {
        m_OverridenBy ??= new List<Symbol>{};
        m_OverridenBy.Add(method);
    }

    public override string ToString() {
        string p = "";
        if (m_TypeParameters != null) {
            p = ".<" + String.Join(", ", m_TypeParameters.Select(p => p.Name)) + ">";
        }
        return FullyQualifiedName + p;
    }
}

public class InstantiationOfGenericMethodSlot : MethodSlot {
    private Symbol m_Origin;
    private Symbol[] m_Arguments;
    private Symbol m_Type;

    public InstantiationOfGenericMethodSlot(Symbol origin, Symbol[] arguments, Symbol type) {
        m_Origin = origin;
        m_Arguments = arguments;
        m_Type = type;
    }

    public override bool IsInstantiatedTypeParameterizedMethod {
        get => true;
    }

    public override Symbol OriginalDefinition {
        get => m_Origin;
    }

    public override Symbol[] ArgumentTypes {
        get => m_Arguments.ToArray();
    }

    public override Symbol StaticType {
        get => m_Type;
    }

    public override string Name {
        get => m_Origin.Name;
    }

    public override string FullyQualifiedName {
        get => m_Origin.FullyQualifiedName;
    }

    public override Visibility Visibility {
        get => m_Origin.Visibility;
    }

    public override MethodSlotFlags MethodFlags {
        get => m_Origin.MethodFlags;
    }

    public override bool UsesYield {
        get => m_Origin.UsesYield;
    }

    public override bool UsesAwait {
        get => m_Origin.UsesAwait;
    }

    public override bool HasOverrideModifier {
        get => m_Origin.HasOverrideModifier;
    }

    public override bool IsFinal {
        get => m_Origin.IsFinal;
    }

    public override bool IsNative {
        get => m_Origin.IsNative;
    }

    public override string ToString() {
        return FullyQualifiedName;
    }
}

public class InstantiatedMethodSlot : MethodSlot {
    private Symbol[] m_ParamTypeTParams;
    private Symbol[] m_ParamTypeArgumentsList;
    private Symbol m_Origin;
    private Symbol m_Type;
    private Symbol m_BelongsToVirtualProp = null;
    private List<Symbol> m_OverridenBy = null;
    private Symbol[] m_TypeParameters = null;

    public InstantiatedMethodSlot(Symbol[] paramTypeTParams, Symbol[] paramTypeArgs, Symbol origin, Symbol type) {
        m_ParamTypeTParams = paramTypeTParams;
        m_ParamTypeArgumentsList = paramTypeArgs;
        m_Origin = origin;
        m_Type = type;

        if (m_Origin.BelongsToVirtualProperty != null) {
            m_BelongsToVirtualProp = TypeReplacement.Replace(m_Origin.BelongsToVirtualProperty, m_ParamTypeTParams, m_ParamTypeArgumentsList);
        }

        var origOverriders = m_Origin.MethodOverridenBy;
        if (origOverriders.Count() > 0) {
            m_OverridenBy = new List<Symbol>(origOverriders.Select(m => TypeReplacement.Replace(m, m_ParamTypeTParams, m_ParamTypeArgumentsList)));
        }
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

    public override string Name {
        get => m_Origin.Name;
    }

    public override string ToString() {
        return FullyQualifiedName;
    }

    public override Symbol StaticType {
        get => m_Type;
    }

    public override Visibility Visibility {
        get => m_Origin.Visibility;
    }

    public override MethodSlotFlags MethodFlags {
        get => m_Origin.MethodFlags;
    }

    public override bool UsesYield {
        get => m_Origin.UsesYield;
    }

    public override bool UsesAwait {
        get => m_Origin.UsesAwait;
    }

    public override bool HasOverrideModifier {
        get => m_Origin.HasOverrideModifier;
    }

    public override bool IsFinal {
        get => m_Origin.IsFinal;
    }

    public override bool IsNative {
        get => m_Origin.IsNative;
    }

    public override Symbol BelongsToVirtualProperty {
        get => m_BelongsToVirtualProp;
    }

    public override Symbol[] TypeParameters {
        get {
            if (m_TypeParameters != null) {
                return m_TypeParameters;
            }
            if (m_Origin.TypeParameters == null) {
                return null;
            }
            var r = new List<Symbol>{};
            foreach (var p in m_Origin.TypeParameters) {
                var newP = ModelCore.Factory.TypeParameter(p.Name);
                foreach (var itrfc in p.ImplementsInterfaces) {
                    newP.AddImplementedInterface(itrfc.ReplaceTypes(TParamsFromRelatedParameterizedType, ArgumentsToRelatedParameterizedType));
                }
                if (p.SuperType != null) {
                    newP.SuperType = p.ReplaceTypes(TParamsFromRelatedParameterizedType, ArgumentsToRelatedParameterizedType);
                }
                r.Add(newP);
            }
            m_TypeParameters = r.ToArray();
            return m_TypeParameters;
        }
    }

    public override Symbol[] MethodOverridenBy {
        get => m_OverridenBy != null ? m_OverridenBy.ToArray() : new Symbol[]{};
    }

    public override void AddMethodOverrider(Symbol method) {
        m_OverridenBy ??= new List<Symbol>{};
        m_OverridenBy.Add(method);
    }
}