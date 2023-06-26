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
        get => (ParentDefinition != null && ParentDefinition.Name != null && ParentDefinition != this.ModelCore.GlobalPackage ? ParentDefinition.FullyQualifiedName + "." : "") + Name;
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

    public virtual bool DontInit {
        get => false;
        set {}
    }

    public virtual Symbol[] ImplementsInterfaces {
        get => new Symbol[]{};
    }

    public virtual Symbol[] TypeParameters {
        get => this.Property is MethodSlot ? this.Property.TypeParameters : null;
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

    public virtual int TupleElementIndex
    {
        get => 0;
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

    public virtual bool IsInstantiatedGenericMethod {
        get => false;
    }

    public virtual Symbol[] ArgumentsToGenericMethod {
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

    public virtual Symbol TypeFromClassStaticThis {
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

    /// <summary>
    /// Mapping from T to <c>convertImplicit</c> as in <c>proxy function convertImplicit(v: T): C</c>.
    /// This is present in a type's delegate, not in the type itself.
    /// </summary>
    public virtual Dictionary<Symbol, Symbol> ImplicitConversionProxies {
        get => new Dictionary<Symbol, Symbol>{};
    }

    /// <summary>
    /// Mapping from T to <c>convertExplicit</c> as in <c>proxy function convertExplicit(v: T): C</c>.
    /// This is present in a type's delegate, not in the type itself.
    /// </summary>
    public virtual Dictionary<Symbol, Symbol> ExplicitConversionProxies {
        get => new Dictionary<Symbol, Symbol>{};
    }

    public bool IsInstantiationOf(Symbol parameterized) {
        return OriginalDefinition == parameterized;
    }

    /// <summary>
    /// For a static type's method, returns a <c>ClassStaticThis</c> value symbol.
    /// For an instance method, returns a <c>ThisValue</c> symbol.
    /// For a normal function, returns null.
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

    /// <summary>
    /// Used for checking if types are similiar. This is futurely
    /// useful for giving hints on verification errors.
    /// </summary>
    public virtual bool TypeStructurallySimiliar(Symbol other) {
        return false;
    }

    public virtual bool PropertyIsVisibleTo(Symbol frame) {
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

    public virtual Symbol ToNonNullableType() {
        return this;
    }

    public Symbol FindActivation()
    {
        return this is ActivationFrame ? this : this.ParentFrame?.FindActivation();
    }

    public bool TypeCanUseObjectInitializer
    {
        get =>  this.IsInstantiationOf(this.ModelCore.MapType)
            ||  this.IsFlagsEnum
            ||  this is RecordType
            || (this.IsClassType && !this.DontInit)
            ||  this is AnyType;
    }

    public bool TypeCanUseArrayInitializer
    {
        get =>  this.IsInstantiationOf(this.ModelCore.ArrayType)
            ||  this.IsInstantiationOf(this.ModelCore.SetType)
            ||  this.IsFlagsEnum
            ||  this is TupleType
            ||  this is AnyType;
    }

    public virtual NameAndTypePair? RecordTypeGetField(string name)
    {
        return null;
    }

    public Symbol InheritConstructorDefinition()
    {
        return this.ConstructorDefinition ?? this.SuperType?.InheritConstructorDefinition();
    }

    /// <summary>
    /// Determines if a class's constructor has optional parameters only.
    /// </summary>
    public bool ClassHasParameterlessConstructor
    {
        get
        {
            var c = InheritConstructorDefinition();
            if (c == null)
            {
                return true;
            }
            return !c.StaticType.FunctionHasRequiredParameters;
        }
    }

    public Symbol GetIMarkupContainerChildType()
    {
        foreach (var itrfc in ImplementsInterfaces)
        {
            if (itrfc.IsInstantiationOf(this.ModelCore.IMarkupContainerType))
            {
                return itrfc.ArgumentTypes[0];
            }
        }
        return null;
    }

    public Symbol GetIteratorItemType()
    {
        if (this == this.ModelCore.IteratorType)
        {
            return this.TypeParameters[0];
        }
        if (this.IsInstantiationOf(this.ModelCore.IteratorType))
        {
            return this.ArgumentTypes[0];
        }
        foreach (var itrfc in ImplementsInterfaces)
        {
            if (itrfc.IsInstantiationOf(this.ModelCore.IteratorType))
            {
                return itrfc.ArgumentTypes[0];
            }
        }
        return null;
    }

    public bool IsGenericTypeOrMethod
    {
        get => this.Property is MethodSlot ? this.Property.IsGenericTypeOrMethod : this.TypeParameters != null;
    }

    public Symbol FindClassFrame()
    {
        return this is ClassFrame ? this : this.ParentFrame?.FindClassFrame();
    }

    public virtual Symbol EscapeAlias()
    {
        return this;
    }

    public bool CanBeASubtypeOf(Symbol type)
    {
        return !(type is AnyType) && !(this is UnionType) && !this.IsSubtypeOf(type);
    }

    public virtual Symbol ShadowsTypeParameter
    {
        get => null;
        set
        {
        }
    }

    public virtual bool IsClassType {
        get => false;
    }

    public virtual bool IsInterfaceType {
        get => false;
    }

    public virtual Symbol CloneTypeParameter()
    {
        throw new Exception("Unimplemented");
    }

    public object NumericConstantValueAsObject
    {
        get => this is NumberConstantValue ? this.NumberValue
            : this is DecimalConstantValue ? this.DecimalValue
            : this is ByteConstantValue ? this.ByteValue
            : this is ShortConstantValue ? this.ShortValue
            : this is IntConstantValue ? this.IntValue
            : this is LongConstantValue ? this.LongValue
            : this is BigIntConstantValue ? this.BigIntValue
            : null;
    }

    /// <summary>
    /// Determines if a signature is valid for a conversion proxy definition.
    /// </summary>
    public bool IsValidProxyConversionSignature(Symbol targetType)
    {
        if (this.FunctionHasOptParameters || this.FunctionRestParameter.HasValue || this.FunctionReturnType != targetType)
        {
            return false;
        }
        return this.FunctionCountOfRequiredParameters != 1 || this.FunctionRequiredParameters[0].Type == targetType;
    }

    /// <summary>
    /// Determines if a signature is valid for a non-conversion proxy definition.
    /// </summary>
    public bool IsValidProxySignature(Operator op, Symbol enclosingType)
    {
        var proxyNumParams = op.ProxyNumberOfParameters;
        // number of parameters.
        if (this.FunctionHasOptParameters || this.FunctionRestParameter.HasValue || this.FunctionCountOfRequiredParameters != proxyNumParams)
        {
            return false;
        }
        // iterateKeys or iterateValues must return Generator.<T>.
        if ((op == Operator.ProxyToIterateKeys || op == Operator.ProxyToIterateValues) && !this.FunctionReturnType.IsInstantiationOf(this.ModelCore.GeneratorType))
        {
            return false;
        }
        // has, deleteIndex and comparisons must return Boolean.
        if (op.AlwaysReturnsBoolean && this.FunctionReturnType != this.ModelCore.BooleanType)
        {
            return false;
        }
        // setIndex must return void.
        if (op == Operator.ProxyToSetIndex && this.FunctionReturnType != this.ModelCore.UndefinedType)
        {
            return false;
        }
        // most operators must have first parameter as current class type
        if (op.ProxyMustHaveFirstParamAsCurrentClass && this.FunctionRequiredParameters[0].Type != enclosingType)
        {
            return false;
        }
        // comparisons must have parameters of same type
        if (op.IsRelativityOperator && this.FunctionRequiredParameters[1].Type != enclosingType)
        {
            return false;
        }
        return true;
    }
}