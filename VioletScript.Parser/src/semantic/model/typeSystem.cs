namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using System.Linq;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public static class DefaultValue
{
    public static Symbol Obtain(Symbol type)
    {
        var mc = type.ModelCore;
        var f = mc.Factory;

        // undefined is preferred over null
        if (type.IncludesUndefined)
        {
            return f.UndefinedConstantValue(type);
        }

        if (type.IncludesNull) return f.NullConstantValue(type);
        if (type == mc.StringType) return f.StringConstantValue("", type);
        if (type == mc.BooleanType) return f.BooleanConstantValue(false, type);
        if (type == mc.NumberType) return f.NumberConstantValue(0, type);
        if (type == mc.DecimalType) return f.DecimalConstantValue(0, type);
        if (type == mc.ByteType) return f.ByteConstantValue(0, type);
        if (type == mc.ShortType) return f.ShortConstantValue(0, type);
        if (type == mc.IntType) return f.IntConstantValue(0, type);
        if (type == mc.LongType) return f.LongConstantValue(0, type);
        if (type == mc.BigIntType) return f.BigIntConstantValue(0, type);
        if (type is EnumType && type.IsFlagsEnum) return EnumConstHelpers.Empty(type);
        if (type is TypeParameter) return f.TypeParameterConstantValue(type);
        return null;
    }
}

public static class TypeRelationship {
    public static bool IsSubtypeOf(Symbol possiblySubtype, Symbol possiblySuperType) {
        return possiblySubtype.SuperTypes.Contains(possiblySuperType);
    }
}

public static class TypeReplacement {
    public static Symbol Replace(Symbol symbolToReplace, Symbol[] typeParameters, Symbol[] argumentsList) {
        if (symbolToReplace is Type) {
            if (symbolToReplace is FunctionType) {
                List<NameAndTypePair> @params = null;
                List<NameAndTypePair> optParams = null;
                NameAndTypePair? restParam = symbolToReplace.FunctionRestParameter != null
                    ? symbolToReplace.FunctionRestParameter.Value.ReplaceTypes(typeParameters, argumentsList) : null;
                Symbol returnType = symbolToReplace.FunctionReturnType.ReplaceTypes(typeParameters, argumentsList);
                if (symbolToReplace.FunctionHasRequiredParameters) {
                    @params = new List<NameAndTypePair>{};
                    foreach (var p in symbolToReplace.FunctionRequiredParameters) {
                        @params.Add(p.ReplaceTypes(typeParameters, argumentsList));
                    }
                }
                if (symbolToReplace.FunctionHasOptParameters) {
                    optParams = new List<NameAndTypePair>{};
                    foreach (var p in symbolToReplace.FunctionOptParameters) {
                        optParams.Add(p.ReplaceTypes(typeParameters, argumentsList));
                    }
                }
                return symbolToReplace.ModelCore.InternFunctionType(@params.ToArray(), optParams.ToArray(), restParam, returnType);
            }
            if (symbolToReplace is TupleType) {
                return symbolToReplace.ModelCore.InternTupleType(
                    symbolToReplace.TupleElementTypes.Select(t => t.ReplaceTypes(typeParameters, argumentsList)).ToArray());
            }
            if (symbolToReplace is RecordType) {
                return symbolToReplace.ModelCore.InternRecordType(
                    symbolToReplace.RecordTypeFields.Select(f => f.ReplaceTypes(typeParameters, argumentsList)).ToArray());
            }
            if (symbolToReplace is UnionType) {
                return symbolToReplace.ModelCore.InternUnionType(
                    symbolToReplace.UnionMemberTypes.Select(t => t.ReplaceTypes(typeParameters, argumentsList)).ToArray());
            }
            if (symbolToReplace is InstantiatedType) {
                return symbolToReplace.ModelCore.InternInstantiatedType(
                    symbolToReplace.OriginalDefinition,
                    symbolToReplace.ArgumentTypes.Select(t => t.ReplaceTypes(typeParameters, argumentsList)).ToArray());
            }
            if (symbolToReplace is TypeParameter) {
                int i = Array.IndexOf(typeParameters, symbolToReplace.ShadowsTypeParameter ?? symbolToReplace);
                if (i != -1) {
                    return argumentsList[i];
                }
            }
        }
        else if (symbolToReplace is VariableSlot) {
            return symbolToReplace.ModelCore.InternInstantiatedVariableSlot(
                symbolToReplace, typeParameters, argumentsList);
        }
        else if (symbolToReplace is VirtualSlot) {
            return symbolToReplace.ModelCore.InternInstantiatedVirtualSlot(
                symbolToReplace, typeParameters, argumentsList);
        }
        else if (symbolToReplace is MethodSlot) {
            return symbolToReplace.ModelCore.InternInstantiatedMethodSlot(
                symbolToReplace, typeParameters, argumentsList);
        }
        return symbolToReplace;
    }

    public static NameAndTypePair ReplaceInNameAndTypePair(NameAndTypePair pair, Symbol[] typeParameters, Symbol[] argumentsList) {
        return new NameAndTypePair(pair.Name, Replace(pair.Type, typeParameters, argumentsList));
    }
}

public class Delegate : Symbol {
    private Symbol m_AssociatedType;
    private Properties m_Properties = new Properties();
    private Dictionary<Operator, Symbol> m_Proxies = new Dictionary<Operator, Symbol>{};
    private Dictionary<Symbol, Symbol> m_ImplicitConversionProxies = new Dictionary<Symbol, Symbol>{};
    private Dictionary<Symbol, Symbol> m_ExplicitConversionProxies = new Dictionary<Symbol, Symbol>{};

    public Delegate(Symbol associatedType) {
        m_AssociatedType = associatedType;
    }

    public override Symbol AssociatedType {
        get => m_AssociatedType;
    }

    public override Properties Properties {
        get => m_Properties;
    }

    public override Dictionary<Operator, Symbol> Proxies {
        get => m_Proxies;
    }

    public override Dictionary<Symbol, Symbol> ImplicitConversionProxies {
        get => m_ImplicitConversionProxies;
    }

    public override Dictionary<Symbol, Symbol> ExplicitConversionProxies {
        get => m_ExplicitConversionProxies;
    }
}

public class Type : Symbol {
    private Visibility m_Visibility = Visibility.Internal;

    public override Visibility Visibility {
        get => m_Visibility;
        set => m_Visibility = value;
    }

    public override Symbol DefaultValue {
        get => VioletScript.Parser.Semantic.Model.DefaultValue.Obtain(this);
    }

    public override bool IsSubtypeOf(Symbol other) {
        return TypeRelationship.IsSubtypeOf(this, other);
    }

    public override Symbol ToNullableType() {
        return this.ModelCore.Factory.UnionType(new Symbol[]{this.ModelCore.NullType, this});
    }
}

public class AnyType : Type {
    public AnyType() {
    }

    public override bool IncludesUndefined {
        get => true;
    }

    public override bool IncludesNull {
        get => true;
    }

    public override string ToString() {
        return "*";
    }

    public override bool TypeStructurallyEquals(Symbol other) {
        return other is AnyType;
    }
}

public class UndefinedType : Type {
    public UndefinedType() {
    }

    public override bool IncludesUndefined {
        get => true;
    }

    public override bool IncludesNull {
        get => false;
    }

    public override string ToString() {
        return "undefined";
    }

    public override bool TypeStructurallyEquals(Symbol other) {
        return other is UndefinedType;
    }
}

public class NullType : Type {
    public NullType() {
    }

    public override bool IncludesUndefined {
        get => false;
    }

    public override bool IncludesNull {
        get => true;
    }

    public override string ToString() {
        return "null";
    }

    public override bool TypeStructurallyEquals(Symbol other) {
        return other is NullType;
    }
}

public class ClassType : Type {
    private string m_Name;
    private Symbol m_SuperType = null;
    private List<Symbol> m_ImplementsInterfaces = null;
    private bool m_IsFinal;
    private bool m_IsValue;
    private bool m_DontInit = false;
    private Symbol[] m_TypeParameters = null;
    private Properties m_Properties = new Properties();
    private Symbol m_Delegate = null;
    private Symbol m_ConstructorDefinition = null;
    private Symbol m_ParentDefinition = null;
    private List<Symbol> m_LimitedKnownSubtypes = null;

    public ClassType(string name, bool isFinal, bool isValue) {
        m_Name = name;
        m_IsFinal = isFinal;
        m_IsValue = isValue;
    }

    public override bool IncludesUndefined {
        get => false;
    }

    public override bool IncludesNull {
        get => false;
    }

    public override bool IsClassType {
        get => true;
    }

    public override string Name {
        get => m_Name;
    }

    public override Symbol SuperType {
        get => m_SuperType;
        set => m_SuperType = value;
    }

    public override bool IsFinal {
        get => m_IsFinal;
        set => m_IsFinal = value;
    }

    public override bool IsValueClass {
        get => m_IsValue;
        set => m_IsValue = value;
    }

    public override bool DontInit {
        get
        {
            if (m_SuperType != null && m_SuperType.DontInit)
            {
                return true;
            }
            return m_DontInit;
        }
        set => m_DontInit = value;
    }

    public override Symbol[] ImplementsInterfaces {
        get => m_ImplementsInterfaces != null ? m_ImplementsInterfaces.ToArray() : new Symbol[]{};
    }

    public override Symbol[] TypeParameters {
        get => m_TypeParameters;
        set => m_TypeParameters = value;
    }

    public override Properties Properties {
        get => m_Properties;
    }

    public override Symbol Delegate {
        get => m_Delegate;
        set => m_Delegate = value;
    }

    public override Symbol ConstructorDefinition {
        get => m_ConstructorDefinition;
        set => m_ConstructorDefinition = value;
    }

    public override Symbol ParentDefinition {
        get => m_ParentDefinition;
        set => m_ParentDefinition = value;
    }

    public override Symbol[] DirectSuperTypes {
        get {
            var r = new List<Symbol>{SuperType};
            foreach (var itrfc in ImplementsInterfaces) {
                r.Add(itrfc);
            }
            return r.ToArray();
        }
    }

    public override void AddImplementedInterface(Symbol itrfc) {
        m_ImplementsInterfaces ??= new List<Symbol>{};
        if (!m_ImplementsInterfaces.Contains(itrfc)) {
            m_ImplementsInterfaces.Add(itrfc);
        }
    }

    public override Symbol[] LimitedKnownSubtypes {
        get => m_LimitedKnownSubtypes?.ToArray();
    }

    public override void AddLimitedKnownSubtype(Symbol type) {
        // do not accumulate known subtypes for `Object` to improve
        // memory performance.
        if (this == ModelCore.ObjectType) {
            return;
        }
        m_LimitedKnownSubtypes ??= new List<Symbol>{};
        if (!m_LimitedKnownSubtypes.Contains(type)) {
            m_LimitedKnownSubtypes.Add(type);
        }
    }

    public override string ToString() {
        return FullyQualifiedName + (m_TypeParameters != null ? ".<"+ String.Join(", ", m_TypeParameters.Select(p => p.Name)) +">" : "");
    }

    public override bool TypeStructurallyEquals(Symbol other) {
        return this == other;
    }
}

public class EnumType : Type {
    private string m_Name;
    private bool m_IsFlags;
    private Symbol m_NumericType;
    private Properties m_Properties = new Properties();
    private Symbol m_Delegate = null;
    private Symbol m_ParentDefinition = null;
    private Dictionary<string, object> m_Variants = new Dictionary<string, object>{};

    public EnumType(string name, bool isFlags, Symbol numericType) {
        m_Name = name;
        m_IsFlags = isFlags;
        m_NumericType = numericType;
    }

    public override bool IncludesUndefined {
        get => false;
    }

    public override bool IncludesNull {
        get => false;
    }

    public override Dictionary<string, object> EnumVariants {
        get => new Dictionary<string, object>(m_Variants);
    }

    public override Symbol NumericType {
        get => m_NumericType;
    }

    public override bool IsFlagsEnum {
        get => m_IsFlags;
    }

    public override Symbol SuperType {
        get => ModelCore.ObjectType;
    }

    public override string Name {
        get => m_Name;
    }

    public override Properties Properties {
        get => m_Properties;
    }

    public override Symbol Delegate {
        get => m_Delegate;
        set => m_Delegate = value;
    }

    public override Symbol ParentDefinition {
        get => m_ParentDefinition;
        set => m_ParentDefinition = value;
    }

    public override Symbol[] DirectSuperTypes {
        get {
            return new []{ModelCore.ObjectType};
        }
    }

    public override bool EnumHasVariantByString(string value) {
        return m_Variants.ContainsKey(value);
    }

    public override bool EnumHasVariantByNumber(object value) {
        return m_Variants.ContainsValue(value);
    }

    public override object EnumGetVariantNumberByString(string value) {
        return m_Variants.ContainsKey(value) ? m_Variants[value] : null;
    }

    public override void EnumSetVariant(string stringValue, object numericValue) {
        m_Variants[stringValue] = numericValue;
    }

    public override void EnumInitializeMethods() {
        // valueOf()
        var valueOfMethod = ModelCore.Factory.MethodSlot("valueOf", ModelCore.Factory.FunctionType(null, null, null, m_NumericType), MethodSlotFlags.Override | MethodSlotFlags.Native);
        valueOfMethod.Visibility = Visibility.Public;
        Delegate.Properties.Set("valueOf", valueOfMethod);

        // toString()
        var toStringMethod = ModelCore.Factory.MethodSlot("toString", ModelCore.Factory.FunctionType(null, null, null, ModelCore.StringType), MethodSlotFlags.Override | MethodSlotFlags.Native);
        toStringMethod.Visibility = Visibility.Public;
        Delegate.Properties.Set("toString", toStringMethod);

        if (m_IsFlags) {
            // all
            var allProp = ModelCore.Factory.VirtualSlot("all", this);
            allProp.Visibility = Visibility.Public;
            allProp.Getter = ModelCore.Factory.MethodSlot("", ModelCore.Factory.FunctionType(null, null, null, this), MethodSlotFlags.Native);
            this.Properties.Set("all", allProp);

            // + operation
            var proxyAddMethod = ModelCore.Factory.MethodSlot("", ModelCore.Factory.FunctionType(new[]{new NameAndTypePair("a", this), new NameAndTypePair("b", this)}, null, null, this), MethodSlotFlags.Native);
            Delegate.Proxies[Operator.Add] = proxyAddMethod;

            // - operation
            var proxySubtractMethod = ModelCore.Factory.MethodSlot("", ModelCore.Factory.FunctionType(new[]{new NameAndTypePair("a", this), new NameAndTypePair("b", this)}, null, null, this), MethodSlotFlags.Native);
            Delegate.Proxies[Operator.Subtract] = proxySubtractMethod;

            // toggle()
            var toggleMethod = ModelCore.Factory.MethodSlot("toggle", ModelCore.Factory.FunctionType(new[]{new NameAndTypePair("value", this)}, null, null, this), MethodSlotFlags.Native);
            toggleMethod.Visibility = Visibility.Public;
            Delegate.Properties.Set("toggle", toggleMethod);

            // filter()
            var filterMethod = ModelCore.Factory.MethodSlot("filter", ModelCore.Factory.FunctionType(new[]{new NameAndTypePair("value", this)}, null, null, this), MethodSlotFlags.Native);
            filterMethod.Visibility = Visibility.Public;
            Delegate.Properties.Set("filter", filterMethod);

            // include()
            var includeMethod = ModelCore.Factory.MethodSlot("include", ModelCore.Factory.FunctionType(new[]{new NameAndTypePair("value", this)}, null, null, this), MethodSlotFlags.Native);
            includeMethod.Visibility = Visibility.Public;
            Delegate.Properties.Set("include", includeMethod);

            // exclude()
            var excludeMethod = ModelCore.Factory.MethodSlot("exclude", ModelCore.Factory.FunctionType(new[]{new NameAndTypePair("value", this)}, null, null, this), MethodSlotFlags.Native);
            excludeMethod.Visibility = Visibility.Public;
            Delegate.Properties.Set("exclude", excludeMethod);

            // v in f;
            var proxyHasMethod = ModelCore.Factory.MethodSlot("has", ModelCore.Factory.FunctionType(new[]{new NameAndTypePair("value", this)}, null, null, ModelCore.BooleanType), MethodSlotFlags.Native);
            Delegate.Proxies[Operator.In] = proxyHasMethod;
        }
    }

    public override string ToString() {
        return Name;
    }

    public override bool TypeStructurallyEquals(Symbol other) {
        return this == other;
    }
}

public class InterfaceType : Type {
    private string m_Name;
    private List<Symbol> m_ExtendsInterfaces = null;
    private Symbol[] m_TypeParameters = null;
    private Symbol m_Delegate = null;
    private Symbol m_ParentDefinition = null;
    private List<Symbol> m_LimitedKnownSubtypes = null;

    public InterfaceType(string name) {
        m_Name = name;
    }

    public override bool IncludesUndefined {
        get => false;
    }

    public override bool IncludesNull {
        get => false;
    }

    public override bool IsInterfaceType {
        get => true;
    }

    public override string Name {
        get => m_Name;
    }

    public override Symbol[] ExtendsInterfaces {
        get => m_ExtendsInterfaces != null ? m_ExtendsInterfaces.ToArray() : new Symbol[]{};
    }

    public override Symbol[] TypeParameters {
        get => m_TypeParameters;
        set => m_TypeParameters = value;
    }

    public override Symbol Delegate {
        get => m_Delegate;
        set => m_Delegate = value;
    }

    public override Symbol ParentDefinition {
        get => m_ParentDefinition;
        set => m_ParentDefinition = value;
    }

    public override Symbol[] DirectSuperTypes {
        get => ExtendsInterfaces.ToArray();
    }

    public override void AddExtendedInterface(Symbol itrfc) {
        m_ExtendsInterfaces ??= new List<Symbol>{};
        if (!m_ExtendsInterfaces.Contains(itrfc)) {
            m_ExtendsInterfaces.Add(itrfc);
        }
    }

    public override Symbol[] LimitedKnownSubtypes {
        get => m_LimitedKnownSubtypes?.ToArray();
    }

    public override void AddLimitedKnownSubtype(Symbol type) {
        m_LimitedKnownSubtypes ??= new List<Symbol>{};
        if (!m_LimitedKnownSubtypes.Contains(type)) {
            m_LimitedKnownSubtypes.Add(type);
        }
    }

    public override string ToString() {
        return FullyQualifiedName + (m_TypeParameters != null ? ".<"+ String.Join(", ", m_TypeParameters.Select(p => p.Name)) +">" : "");
    }

    public override bool TypeStructurallyEquals(Symbol other) {
        return this == other;
    }
}

public class FunctionType : Type {
    private NameAndTypePair[] m_RequiredParams;
    private NameAndTypePair[] m_OptParams;
    private NameAndTypePair? m_RestParam;
    private Symbol m_ReturnType;

    public FunctionType(NameAndTypePair[] requiredParams, NameAndTypePair[] optParams, NameAndTypePair? restParam, Symbol returnType) {
        m_RequiredParams = requiredParams;
        m_OptParams = optParams;
        m_RestParam = restParam;
        m_ReturnType = returnType;
    }

    public override bool IncludesUndefined {
        get => false;
    }

    public override bool IncludesNull {
        get => false;
    }

    public override bool FunctionHasRequiredParameters {
        get => m_RequiredParams != null;
    }

    public override int FunctionCountOfRequiredParameters {
        get => m_RequiredParams != null ? m_RequiredParams.Count() : 0;
    }

    public override bool FunctionHasOptParameters {
        get => m_OptParams != null;
    }

    public override int FunctionCountOfOptParameters {
        get => m_OptParams != null ? m_OptParams.Count() : 0;
    }

    public override NameAndTypePair[] FunctionRequiredParameters {
        get => m_RequiredParams == null ? null : m_RequiredParams.ToArray();
    }

    public override NameAndTypePair[] FunctionOptParameters {
        get => m_OptParams == null ? null : m_OptParams.ToArray();
    }

    public override NameAndTypePair? FunctionRestParameter {
        get => m_RestParam;
    }

    public override Symbol FunctionReturnType {
        get => m_ReturnType;
    }

    public override Symbol SuperType {
        get => ModelCore.FunctionType;
    }

    public override Symbol[] DirectSuperTypes {
        get => new []{ModelCore.FunctionType};
    }

    public override string ToString() {
        var p = new List<string>{};
        if (m_RequiredParams != null) {
            foreach (var p2 in m_RequiredParams) {
                p.Add(p2.Name + ":" + p2.Type.ToString());
            }
        }
        if (m_OptParams != null) {
            foreach (var p2 in m_OptParams) {
                p.Add(p2.Name + "?:" + p2.Type.ToString());
            }
        }
        if (m_RestParam != null) {
            p.Add("..." + m_RestParam.Value.Name + ":" + m_RestParam.Value.Type.ToString());
        }
        return "(" + String.Join(", ", p) + ")" + "->" + (FunctionReturnType == ModelCore.UndefinedType ? "void" : FunctionReturnType.ToString());
    }

    public override bool TypeStructurallyEquals(Symbol otherAbstract) {
        if (!(otherAbstract is FunctionType)) {
            return false;
        }
        var other = (FunctionType) otherAbstract;

        if (m_RequiredParams != null) {
            var c = m_RequiredParams.Count();
            if (other.m_RequiredParams == null || other.m_RequiredParams.Count() != c) {
                return false;
            }
            for (int i = 0; i != c; ++i) {
                if (!m_RequiredParams[i].Type.TypeStructurallyEquals(other.m_RequiredParams[i].Type)) {
                    return false;
                }
            }
        } else if (other.m_RequiredParams != null) {
            return false;
        }

        if (m_OptParams != null) {
            var c = m_OptParams.Count();
            if (other.m_OptParams == null || other.m_OptParams.Count() != c) {
                return false;
            }
            for (int i = 0; i != c; ++i) {
                if (!m_OptParams[i].Type.TypeStructurallyEquals(other.m_OptParams[i].Type)) {
                    return false;
                }
            }
        } else if (other.m_OptParams != null) {
            return false;
        }

        if (m_RestParam != null) {
            if (other.m_RestParam == null || !m_RestParam.Value.Type.TypeStructurallyEquals(other.m_RestParam.Value.Type)) {
                return false;
            }
        } else if (other.m_RestParam != null) {
            return false;
        }

        return m_ReturnType.TypeStructurallyEquals(other.m_ReturnType);
    }
}

public class TupleType : Type {
    private Symbol[] m_Types;

    public TupleType(Symbol[] types) {
        m_Types = types;
    }

    public override bool IncludesUndefined {
        get => false;
    }

    public override bool IncludesNull {
        get => false;
    }

    public override Symbol SuperType {
        get => ModelCore.ObjectType;
    }

    public override Symbol[] DirectSuperTypes {
        get => new []{ModelCore.ObjectType};
    }

    public override Symbol[] TupleElementTypes {
        get => m_Types.ToArray();
    }

    public override int CountOfTupleElements {
        get => m_Types.Count();
    }

    public override string ToString() {
        return "[" + String.Join(", ", m_Types.Select(t => t.ToString())) + "]";
    }

    public override bool TypeStructurallyEquals(Symbol otherAbstract) {
        if (!(otherAbstract is TupleType)) {
            return false;
        }
        var other = (TupleType) otherAbstract;
        var c = m_Types.Count();
        if (other.m_Types.Count() != c) {
            return false;
        }
        for (int i = 0; i != c; ++i) {
            if (!m_Types[i].TypeStructurallyEquals(other.m_Types[i])) {
                return false;
            }
        }
        return true;
    }
}

public class RecordType : Type {
    private Symbol m_Delegate = null;
    private NameAndTypePair[] m_Fields;

    public RecordType(NameAndTypePair[] fields) {
        m_Fields = fields;
    }

    public override bool IncludesUndefined {
        get => false;
    }

    public override bool IncludesNull {
        get => false;
    }

    public override Symbol Delegate {
        get => m_Delegate;
        set => m_Delegate = value;
    }

    public override NameAndTypePair[] RecordTypeFields {
        get => m_Fields.ToArray();
    }

    public override int RecordFieldCount {
        get => m_Fields.Count();
    }

    public override Symbol SuperType {
        get => ModelCore.ObjectType;
    }

    public override Symbol[] DirectSuperTypes {
        get => new[]{ModelCore.ObjectType};
    }

    public override NameAndTypePair? RecordTypeGetField(string name)
    {
        foreach (var item in m_Fields)
        {
            if (item.Name == name)
            {
                return item;
            }
        }
        return null;
    }

    public override string ToString() {
        return "{" + String.Join(", ", m_Fields.Select(field => field.Name + ":" + field.Type.ToString())) + "}";
    }

    public override bool TypeStructurallyEquals(Symbol otherAbstract) {
        if (!(otherAbstract is RecordType)) {
            return false;
        }
        var other = (RecordType) otherAbstract;
        var c = m_Fields.Count();
        if (other.m_Fields.Count() != c) {
            return false;
        }
        for (int i = 0; i != c; ++i) {
            var fieldX = m_Fields[i];
            var fieldY = other.m_Fields[i];
            if (fieldX.Name != fieldY.Name || !fieldX.Type.TypeStructurallyEquals(fieldY.Type)) {
                return false;
            }
        }
        return true;
    }
}

public class UnionType : Type {
    private Symbol[] m_Types;

    public UnionType(Symbol[] types) {
        m_Types = types;
    }

    public override bool IncludesUndefined {
        get => m_Types.Where(t => t is UndefinedType).Count() > 0;
    }

    public override bool IncludesNull {
        get => m_Types.Where(t => t is NullType).Count() > 0;
    }

    public override Symbol[] UnionMemberTypes {
        get => m_Types.ToArray();
    }

    public override int UnionCountOfMembers {
        get => m_Types.Count();
    }

    /// <summary>
    /// Determines if union type is equivalent to <c>null|T</c>.
    /// </summary>
    public override bool IsNullableUnionType {
        get => m_Types.Count() == 2 && m_Types[0] == ModelCore.NullType;
    }

    public override string ToString() {
        if (m_Types.Count() == 2 && m_Types[0] == ModelCore.NullType) {
            return m_Types[1].ToString() + "?";
        }
        var o = new List<string>{};
        foreach (var t in m_Types) {
            if ((t is UnionType && !t.IsNullableUnionType)
            ||   t is FunctionType) {
                o.Add("(" + t.ToString() + ")");
            } else {
                o.Add(t.ToString());
            }
        }
        return String.Join("|", o);
    }

    public override bool TypeStructurallyEquals(Symbol otherAbstract) {
        if (!(otherAbstract is UnionType)) {
            return false;
        }
        var other = (UnionType) otherAbstract;
        var c = m_Types.Count();
        if (other.m_Types.Count() != c) {
            return false;
        }
        for (int i = 0; i != c; ++i) {
            if (!m_Types[i].TypeStructurallyEquals(other.m_Types[i])) {
                return false;
            }
        }
        return true;
    }

    public override Symbol ToNonNullableType() {
        var r = m_Types.Where(t => !(t is NullType) && !(t is UndefinedType)).ToArray();
        if (r.Count() == 0)
        {
            r = new Symbol[]{ModelCore.UndefinedType};
        }
        return this.ModelCore.Factory.UnionType(r);
    }
}

public class InstantiatedType : Type {
    private Symbol m_Origin;
    private Symbol[] m_ArgumentsList;
    private Symbol m_Delegate = null;
    private Properties m_Properties = null;
    private Symbol m_Constructor = null;

    public InstantiatedType(Symbol origin, Symbol[] argumentsList) {
        m_Origin = origin;
        m_ArgumentsList = argumentsList;
    }

    public override bool IncludesUndefined {
        get => false;
    }

    public override bool IncludesNull {
        get => false;
    }

    public override Symbol SuperType {
        get => m_Origin.SuperType.ReplaceTypes(m_Origin.TypeParameters, m_ArgumentsList);
    }

    public override Symbol[] ImplementsInterfaces {
        get => m_Origin.ImplementsInterfaces.Select(t => t.ReplaceTypes(m_Origin.TypeParameters, m_ArgumentsList)).ToArray();
    }

    public override Symbol[] DirectSuperTypes {
        get => m_Origin.DirectSuperTypes.Select(t => t.ReplaceTypes(m_Origin.TypeParameters, m_ArgumentsList)).ToArray();
    }

    public override Symbol ConstructorDefinition {
        get {
            if (m_Constructor != null) {
                return m_Constructor;
            }
            if (m_Origin.ConstructorDefinition == null) {
                return null;
            }
            m_Constructor = m_Origin.ConstructorDefinition.ReplaceTypes(m_Origin.TypeParameters, m_ArgumentsList);
            return m_Constructor;
        }
    }

    public override Symbol OriginalDefinition {
        get => m_Origin;
    }

    public override Symbol[] ArgumentTypes {
        get => m_ArgumentsList.ToArray();
    }

    public override bool IsInstantiated {
        get => true;
    }

    public override bool IsClassType {
        get => m_Origin.IsClassType;
    }

    public override bool IsInterfaceType {
        get => m_Origin.IsInterfaceType;
    }

    public override Symbol ParentDefinition {
        get => m_Origin.ParentDefinition;
    }

    public override bool IsValueClass {
        get => m_Origin.IsValueClass;
    }

    public override bool DontInit {
        get => m_Origin.DontInit;
    }

    public override bool IsFinal {
        get => m_Origin.IsFinal;
    }

    public override string FullyQualifiedName {
        get => m_Origin.FullyQualifiedName;
    }

    public override Properties Properties {
        get {
            if (m_Properties != null) {
                return m_Properties;
            }
            if (m_Origin.Properties == null) {
                return null;
            }
            m_Properties = new Properties();
            foreach (var (name, symbol) in m_Origin.Properties) {
                m_Properties.Set(name, symbol.ReplaceTypes(m_Origin.TypeParameters, m_ArgumentsList));
            }
            return m_Properties;
        }
    }

    public override Symbol Delegate {
        get {
            if (m_Delegate != null) {
                return m_Delegate;
            }
            m_Delegate = ModelCore.Factory.Delegate(this);
            foreach (var (name, symbol) in m_Origin.Delegate.Properties) {
                m_Delegate.Properties.Set(name, symbol.ReplaceTypes(m_Origin.TypeParameters, m_ArgumentsList));
            }
            foreach (var (@operator, symbol) in m_Origin.Delegate.Proxies) {
                m_Delegate.Proxies[@operator] = symbol.ReplaceTypes(m_Origin.TypeParameters, m_ArgumentsList);
            }
            foreach (var (type, symbol) in m_Origin.Delegate.ImplicitConversionProxies) {
                m_Delegate.ImplicitConversionProxies[type] = symbol.ReplaceTypes(m_Origin.TypeParameters, m_ArgumentsList);
            }
            foreach (var (type, symbol) in m_Origin.Delegate.ExplicitConversionProxies) {
                m_Delegate.ExplicitConversionProxies[type] = symbol.ReplaceTypes(m_Origin.TypeParameters, m_ArgumentsList);
            }
            return m_Delegate;
        }
    }

    public override string ToString() {
        return FullyQualifiedName + ".<" + String.Join(", ", m_ArgumentsList.Select(a => a.ToString())) + ">";
    }

    public override bool TypeStructurallyEquals(Symbol otherAbstract) {
        if (!(otherAbstract is InstantiatedType)) {
            return false;
        }
        var other = (InstantiatedType) otherAbstract;
        if (!m_Origin.TypeStructurallyEquals(other.m_Origin)) {
            return false;
        }
        var c = m_ArgumentsList.Count();
        if (other.m_ArgumentsList.Count() != c) {
            return false;
        }
        for (int i = 0; i != c; ++i) {
            if (!m_ArgumentsList[i].TypeStructurallyEquals(other.m_ArgumentsList[i])) {
                return false;
            }
        }
        return true;
    }
}

public class TypeParameter : Type {
    private string m_Name;
    private Symbol m_ExtendsClass = null;
    private List<Symbol> m_ImplementsInterfaces = null;
    private Symbol m_Shadows = null;

    public TypeParameter(string name) {
        m_Name = name;
    }

    public override bool IncludesUndefined {
        get => false;
    }

    public override bool IncludesNull {
        get => false;
    }

    public override string Name {
        get => m_Name;
    }

    public override Symbol SuperType {
        get => m_ExtendsClass;
        set => m_ExtendsClass = value;
    }

    public override Symbol[] DirectSuperTypes {
        get {
            var r = new List<Symbol>{};
            if (m_ExtendsClass != null) {
                r.Add(m_ExtendsClass);
            }
            if (m_ImplementsInterfaces != null) {
                foreach (var itrfc in m_ImplementsInterfaces) {
                    r.Add(itrfc);
                }
            }
            return r.ToArray();
        }
    }

    public override Symbol[] ImplementsInterfaces {
        get => m_ImplementsInterfaces == null ? new Symbol[]{} : m_ImplementsInterfaces.ToArray();
    }

    public override void AddImplementedInterface(Symbol itrfc) {
        m_ImplementsInterfaces ??= new List<Symbol>{};
        if (!m_ImplementsInterfaces.Contains(itrfc)) {
            m_ImplementsInterfaces.Add(itrfc);
        }
    }

    public override Symbol ShadowsTypeParameter
    {
        get => m_Shadows;
        set
        {
            m_Shadows = value;
        }
    }

    public override Symbol CloneTypeParameter()
    {
        TypeParameter r = (TypeParameter) this.ModelCore.Factory.TypeParameter(this.Name);
        r.m_ExtendsClass = this.m_ExtendsClass;
        r.m_ImplementsInterfaces = this.m_ImplementsInterfaces?.GetRange(0, this.m_ImplementsInterfaces.Count());
        r.m_Shadows = this.m_Shadows;
        return r;
    }

    public override string ToString() {
        return m_Name;
    }

    public override bool TypeStructurallyEquals(Symbol other) {
        return this == other;
    }
}

/// <summary>
/// Type used for creating a single list mixing
/// required, optional and rest function parameters.
/// </summary>
public struct RequiredOrOptOrRestParam
{
    public RequiredOrOptOrRestParamKind Kind;
    public NameAndTypePair NameAndType;

    public static List<RequiredOrOptOrRestParam> FromType(Symbol type)
    {
        return FromLists(type.FunctionRequiredParameters.ToList(), type.FunctionOptParameters.ToList(), type.FunctionRestParameter);
    }

    public static List<RequiredOrOptOrRestParam> FromLists
    (
        List<NameAndTypePair> required,
        List<NameAndTypePair> optional,
        NameAndTypePair? rest
    )
    {
        var mixed = new List<RequiredOrOptOrRestParam>();
        if (required != null)
        {
            foreach (var nt in required)
            {
                mixed.Add(new RequiredOrOptOrRestParam(RequiredOrOptOrRestParamKind.Required, nt));
            }
        }
        if (optional != null)
        {
            foreach (var nt in optional)
            {
                mixed.Add(new RequiredOrOptOrRestParam(RequiredOrOptOrRestParamKind.Optional, nt));
            }
        }
        if (rest.HasValue)
        {
            mixed.Add(new RequiredOrOptOrRestParam(RequiredOrOptOrRestParamKind.Rest, rest.Value));
        }
        return mixed;
    }

    public static
    (
        List<NameAndTypePair>,
        List<NameAndTypePair>,
        NameAndTypePair?
    )
    SeparateKinds(List<RequiredOrOptOrRestParam> list)
    {
        List<NameAndTypePair> rq = null;
        List<NameAndTypePair> opt = null;
        NameAndTypePair? rs = null;
        foreach (var p in list)
        {
            if (p.Kind == RequiredOrOptOrRestParamKind.Required)
            {
                rq ??= new List<NameAndTypePair>();
                rq.Add(p.NameAndType);
            }
            else if (p.Kind == RequiredOrOptOrRestParamKind.Optional)
            {
                opt ??= new List<NameAndTypePair>();
                opt.Add(p.NameAndType);
            }
            else
            {
                rs = p.NameAndType;
                break;
            }
        }
        return (rq, opt, rs);
    }

    public RequiredOrOptOrRestParam(RequiredOrOptOrRestParamKind kind, NameAndTypePair nameAndType)
    {
        this.Kind = kind;
        this.NameAndType = nameAndType;
    }
}

public enum RequiredOrOptOrRestParamKind
{
    Required,
    Optional,
    Rest,
}