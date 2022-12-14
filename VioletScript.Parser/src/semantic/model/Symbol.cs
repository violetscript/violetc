namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using System.Numerics;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public class Symbol {
    public ModelCore ModelCore = null;

    public virtual string Name {
        get => null;
    }

    public virtual Symbol AliasToSymbol {
        get => null;
    }

    public virtual Visibility Visibility {
        get => Visibility.Internal;
        set {}
    }

    public virtual Symbol ParentDefinition {
        get => null;
        set {}
    }

    public virtual Symbol AssociatedType {
        get => null;
    }

    public virtual Properties Properties {
        get => new Properties();
    }

    public virtual Dictionary<Operator, Symbol> Proxies {
        get => null;
    }

    public virtual Symbol DefaultValue {
        get => null;
    }

    public virtual bool IsSubtypeOf(Symbol other) {
        return false;
    }

    public virtual bool IncludesUndefined {
        get => false;
    }

    public virtual bool IncludesNull {
        get => false;
    }

    public virtual string FullyQualifiedName {
        get => (ParentDefinition != null && ParentDefinition.Name != null ? ParentDefinition.FullyQualifiedName + "." : "") + Name;
    }

    public virtual Symbol SuperType {
        get => null;
        set {}
    }

    public virtual bool IsFinal {
        get => false;
        set {}
    }

    public virtual bool IsValueClass {
        get => false;
        set {}
    }

    public virtual Symbol[] ImplementsInterfaces {
        get => new Symbol[]{};
    }

    public virtual Symbol[] TypeParameters {
        get => null;
        set {}
    }

    public virtual Symbol Delegate {
        get => null;
        set {}
    }

    public virtual Symbol ConstructorDefinition {
        get => null;
        set {}
    }

    public virtual void AddImplementedInterface(Symbol itrfc) {
    }

    public virtual Symbol[] DirectSuperTypes {
        get => new Symbol[]{};
    }

    public List<Symbol> SuperTypes {
        get {
            var r = new List<Symbol>{};
            foreach (var type in DirectSuperTypes) {
                if (!r.Contains(type)) {
                    r.Add(type);
                }
                foreach (var type2 in type.SuperTypes) {
                    if (!r.Contains(type2)) {
                        r.Add(type2);
                    }
                }
            }
            return r;
        }
    }

    public virtual Symbol NumericType {
        get => null;
    }

    public virtual bool IsFlagsEnum {
        get => false;
    }

    public virtual Dictionary<string, object> EnumVariants {
        get => new Dictionary<string, object>{};
    }

    public virtual bool EnumHasVariantByString(string value) {
        return false;
    }

    public virtual bool EnumHasVariantByNumber(object value) {
        return false;
    }

    public virtual object EnumGetVariantNumberByString(string value) {
        return null;
    }

    public virtual void EnumSetVariant(string stringValue, object numericValue) {
    }

    public virtual void EnumInitializeMethods() {
    }

    public virtual Symbol[] ExtendsInterfaces {
        get => new Symbol[]{};
    }

    public virtual void AddExtendedInterface(Symbol itrfc) {
    }

    /// <summary>Possibly null.</summary>
    public virtual NameAndTypePair[] FunctionRequiredParameters {
        get => null;
    }

    /// <summary>Possibly null.</summary>
    public virtual NameAndTypePair[] FunctionOptParameters {
        get => null;
    }

    /// <summary>Possibly null.</summary>
    public virtual NameAndTypePair? FunctionRestParameter {
        get => null;
    }

    /// <summary>Possibly null.</summary>
    public virtual Symbol FunctionReturnType {
        get => null;
    }

    public virtual Symbol[] TupleElementTypes {
        get => new Symbol[]{};
    }

    public virtual bool FunctionHasRequiredParameters {
        get => false;
    }

    public virtual int FunctionCountOfRequiredParameters {
        get => 0;
    }

    public virtual bool FunctionHasOptParameters {
        get => false;
    }

    public virtual int FunctionCountOfOptParameters {
        get => 0;
    }

    public virtual int CountOfTupleElements {
        get => 0;
    }

    public virtual NameAndTypePair[] RecordTypeFields {
        get => new NameAndTypePair[]{};
    }

    public virtual int RecordFieldCount {
        get => 0;
    }

    public virtual Symbol[] UnionMemberTypes {
        get => new Symbol[]{};
    }

    public virtual int UnionCountOfMembers {
        get => 0;
    }

    public virtual Symbol OriginalDefinition {
        get => null;
    }

    public virtual Symbol[] ArgumentTypes {
        get => new Symbol[]{};
    }

    public virtual bool IsInstantiated {
        get => false;
    }

    public virtual Dictionary<string, Symbol> Subpackages {
        get => new Dictionary<string, Symbol>{};
    }

    public virtual void AddSubpackage(Symbol package) {
    }

    public virtual Symbol GetSubpackage(string name) {
        return null;
    }

    public virtual Symbol FindOrCreateDeepSubpackage(string[] dotDelimitedId) {
        return null;
    }

    public virtual Symbol[] NamespaceSetItems {
        get => new Symbol[]{};
    }

    public virtual Symbol StaticType {
        get => null;
        set {}
    }

    public virtual bool ReadOnly {
        get => true;
        set {}
    }

    public virtual bool WriteOnly {
        get => false;
        set {}
    }

    /// <summary>
    /// Indicates a next frame for a variable definition in which its bindings
    /// can be accessed. This allows shadowing previous bindings.
    /// </summary>
    public virtual Symbol ShadowFrame {
        get => null;
        set {}
    }

    public virtual Symbol InitValue {
        get => null;
        set {}
    }

    public virtual Symbol[] TParamsFromRelatedParameterizedType {
        get => null;
    }

    public virtual Symbol[] ArgumentsToRelatedParameterizedType {
        get => new Symbol[]{};
    }

    public virtual Symbol Getter {
        get => null;
        set {}
    }

    public virtual Symbol Setter {
        get => null;
        set {}
    }

    public virtual MethodSlotFlags MethodFlags {
        get => (MethodSlotFlags) 0;
        set {}
    }

    public virtual bool UsesYield {
        get => false;
        set {}
    }

    public virtual bool UsesAwait {
        get => false;
        set {}
    }

    public virtual bool HasOverrideModifier {
        get => false;
        set {}
    }

    public virtual bool IsNative {
        get => false;
        set {}
    }

    public virtual Symbol BelongsToVirtualProperty {
        get => null;
        set {}
    }

    public virtual Symbol[] MethodOverridenBy {
        get => new Symbol[]{};
    }

    public virtual void AddMethodOverrider(Symbol method) {
    }

    public virtual bool IsInstantiatedTypeParameterizedMethod {
        get => false;
    }

    public virtual Symbol[] ArgumentsToTypeParameterizedMethod {
        get => new Symbol[]{};
    }

    public Symbol ReplaceTypes(Symbol[] typeParameters, Symbol[] argumentsList) {
        return TypeReplacement.Replace(this, typeParameters, argumentsList);
    }

    /// <summary>
    /// Limited set of known subtypes. This allows tracking, for example, which classes
    /// inherit a given class. This is usually empty for the <c>Object</c> type
    /// to improve memory usage, since it has a large amount of subtypes.
    /// </summary>
    public virtual Symbol[] LimitedKnownSubtypes {
        get => new Symbol[]{};
    }

    public virtual void AddLimitedKnownSubtype(Symbol type) {
    }

    public virtual string StringValue {
        get => "";
    }

    public virtual bool BooleanValue {
        get => false;
    }

    public virtual double NumberValue {
        get => 0;
    }

    public virtual decimal DecimalValue {
        get => 0;
    }

    public virtual byte ByteValue {
        get => 0;
    }

    public virtual short ShortValue {
        get => 0;
    }

    public virtual int IntValue {
        get => 0;
    }

    public virtual long LongValue {
        get => 0;
    }

    public virtual BigInteger BigIntValue {
        get => (BigInteger) 0;
    }

    public virtual Symbol TypeFromTypeAsValue {
        get => null;
    }

    public virtual Symbol NamespaceFromNamespaceAsValue {
        get => null;
    }

    public virtual Symbol NamespaceSetFromNamespaceSetAsValue {
        get => null;
    }

    public virtual Symbol TypeFromTypeStaticThis {
        get => null;
    }

    public virtual Symbol Base {
        get => null;
    }

    public virtual Symbol Property {
        get => null;
    }

    public virtual Symbol PropertyDefinedByType {
        get => null;
    }

    public virtual Symbol ConversionTargetType {
        get => null;
    }

    public virtual bool ConversionIsOptional {
        get => false;
    }

    public virtual ConversionFromTo ConversionFromTo {
        get => VioletScript.Parser.Semantic.Logic.ConversionFromTo.ToNumTypeWithWiderRange;
    }

    public virtual List<Symbol> OpenNamespaces {
        get => new List<Symbol>{};
    }

    public virtual void OpenNamespace(Symbol ns) {
    }

    public virtual bool VariableHasExtendedLife(Symbol slot) {
        return false;
    }

    public virtual void AddExtendedLifeVariable(Symbol slot) {
    }

    public virtual Symbol TypeFromFrame {
        get => null;
    }

    public virtual Symbol NamespaceFromFrame {
        get => null;
    }

    public virtual Symbol PackageFromFrame {
        get => null;
    }

    public virtual Symbol ObjectFromWithFrame {
        get => null;
    }

    public virtual Symbol ParentFrame {
        get => null;
        set {}
    }

    /// <summary>
    /// Determines if union type is equivalent to <c>null|T</c>.
    /// </summary>
    public virtual bool IsNullableUnionType {
        get => false;
    }

    public virtual object EnumConstValue {
        get => null;
    }

    public virtual Dictionary<Symbol, Symbol> ImplicitConversionProxies {
        get => new Dictionary<Symbol, Symbol>{};
    }

    public virtual Dictionary<Symbol, Symbol> ExplicitConversionProxies {
        get => new Dictionary<Symbol, Symbol>{};
    }

    public bool IsInstantiationOf(Symbol parameterized) {
        return OriginalDefinition == parameterized;
    }

    /// <summary>
    /// For a static type's method, returns a type symbol.
    /// For an instance method, returns a <c>ThisValue</c> symbol.
    /// </summary>
    public virtual Symbol ActivationThisOrThisAsStaticType {
        get => null;
        set {}
    }

    public Symbol ResolveProperty(string name) {
        return PropertyResolution.Resolve(this, name);
    }

    public virtual Symbol ExpectedSignature {
        get => null;
    }

    public virtual bool TypeStructurallyEquals(Symbol other) {
        return false;
    }

    public bool PropertyIsVisibleTo(Symbol frame) {
        if (Visibility == Visibility.Public) {
            return true;
        }
        if (Visibility == Visibility.Internal) {
            Symbol ownerPackage = null;
            Symbol p = ParentDefinition;
            while (p != null) {
                if (p is Package) {
                    ownerPackage = p;
                    break;
                }
                p = p.ParentDefinition;
            }
            if (ownerPackage == null) {
                return true;
            }
            while (frame != null) {
                if (frame is PackageFrame && frame.PackageFromFrame == ownerPackage) {
                    return true;
                }
                frame = frame.ParentFrame;
            }
            return false;
        }
        if (Visibility == Visibility.Private) {
            Symbol ownerType = null;
            Symbol p = ParentDefinition;
            while (p != null) {
                if (p is Type) {
                    ownerType = p;
                    break;
                }
                p = p.ParentDefinition;
            }
            if (ownerType == null) {
                return false;
            }
            while (frame != null) {
                if (frame.TypeFromFrame == ownerType) {
                    return true;
                }
                frame = frame.ParentFrame;
            }
            return false;
        }
        if (Visibility == Visibility.Protected) {
            Symbol ownerType = null;
            Symbol p = ParentDefinition;
            while (p != null) {
                if (p is Type) {
                    ownerType = p;
                    break;
                }
                p = p.ParentDefinition;
            }
            if (ownerType == null) {
                return false;
            }
            while (frame != null) {
                if (frame.TypeFromFrame != null && (frame.TypeFromFrame == ownerType || frame.TypeFromFrame.IsSubtypeOf(ownerType))) {
                    return true;
                }
                frame = frame.ParentFrame;
            }
            return false;
        }
        return true;
    }

    public virtual Symbol ToNullableType() {
        return this;
    }
}