namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using System.Numerics;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public sealed class Factory {
    private ModelCore ModelCore;

    public Factory(ModelCore modelCore) {
        ModelCore = modelCore;
    }

    public Symbol AmbiguousReferenceIssue(string name) {
        Symbol r = new AmbiguousReferenceIssue(name);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol CannotOverrideGenericMethodIssue(string name) {
        Symbol r = new CannotOverrideGenericMethodIssue(name);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol CannotOverrideFinalMethodIssue(string name) {
        Symbol r = new CannotOverrideFinalMethodIssue(name);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol MustOverrideAMethodIssue(string name) {
        Symbol r = new MustOverrideAMethodIssue(name);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol IncompatibleOverrideSignatureIssue(string name, Symbol expectedSignature) {
        Symbol r = new IncompatibleOverrideSignatureIssue(name, expectedSignature);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol Alias(string name, Symbol to) {
        Symbol r = new Alias(name, to);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol Delegate(Symbol associatedType) {
        Symbol r = new Delegate(associatedType);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol AnyType() {
        if (ModelCore.AnyType == null) {
            ModelCore.AnyType = new AnyType();
            ModelCore.AnyType.ModelCore = ModelCore;
        }
        return ModelCore.AnyType;
    }

    public Symbol UndefinedType() {
       if (ModelCore.UndefinedType == null) {
            ModelCore.UndefinedType = new UndefinedType();
            ModelCore.UndefinedType.ModelCore = ModelCore;
        }
        return ModelCore.UndefinedType;
    }

    public Symbol NullType() {
       if (ModelCore.NullType == null) {
            ModelCore.NullType = new NullType();
            ModelCore.NullType.ModelCore = ModelCore;
        }
        return ModelCore.NullType;
    }

    public Symbol ClassType(string name, bool isFinal = false, bool isValue = false) {
        Symbol r = new ClassType(name, isFinal || isValue, isValue);
        r.ModelCore = ModelCore;
        r.Delegate = Delegate(r);
        r.SuperType = r == ModelCore.ObjectType ? null : ModelCore.ObjectType;
        return r;
    }

    public Symbol EnumType(string name, bool isFlags = false, Symbol reprType = null) {
        Symbol r = new EnumType(name, isFlags, reprType ?? ModelCore.NumberType);
        r.ModelCore = ModelCore;
        r.Delegate = Delegate(r);
        /* r.EnumInitializeMethods(); */
        return r;
    }

    public Symbol InterfaceType(string name) {
        Symbol r = new InterfaceType(name);
        r.ModelCore = ModelCore;
        r.Delegate = Delegate(r);
        return r;
    }

    public Symbol FunctionType(NameAndTypePair[] requiredParams, NameAndTypePair[] optParams, NameAndTypePair? restParam, Symbol returnType) {
        return ModelCore.InternFunctionType(requiredParams, optParams, restParam, returnType);
    }

    public Symbol TupleType(Symbol[] types) {
        return ModelCore.InternTupleType(types);
    }

    public Symbol RecordType(NameAndTypePair[] fields) {
        return ModelCore.InternRecordType(fields);
    }

    public Symbol UnionType(Symbol[] types) {
        return ModelCore.InternUnionType(types);
    }

    public Symbol TypeWithArguments(Symbol origin, Symbol[] argumentTypes) {
        return ModelCore.InternTypeWithArguments(origin, argumentTypes);
    }

    public Symbol TypeParameter(string name) {
        var r = new TypeParameter(name);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol Namespace(string name) {
        Symbol r = new Namespace(name);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol Package(string name) {
        Symbol r = new Package(name);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol NamespaceSet(string name, Symbol[] set) {
        Symbol r = new NamespaceSet(name, set);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol VariableSlot(string name, bool readOnly, Symbol staticType) {
        Symbol r = new NormalVariableSlot(name, readOnly, staticType);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol VirtualSlot(string name, Symbol staticType) {
        Symbol r = new NormalVirtualSlot(name, staticType);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol MethodSlot(string name, Symbol staticType, MethodSlotFlags flags = ((MethodSlotFlags) 0)) {
        Symbol r = new NormalMethodSlot(name, staticType, flags);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol Value(Symbol type) {
        Symbol r = new Value();
        r.StaticType = type;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol NullUnwrappedValue(Symbol type) {
        Symbol r = new NullUnwrappedValue();
        r.StaticType = type;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol UndefinedConstantValue(Symbol type = null) {
        Symbol r = new UndefinedConstantValue();
        r.StaticType = type ?? ModelCore.UndefinedType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol NullConstantValue(Symbol type = null) {
        Symbol r = new NullConstantValue();
        r.StaticType = type ?? ModelCore.NullType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol TypeParameterConstantValue(Symbol type) {
        Symbol r = new TypeParameterConstantValue();
        r.StaticType = type;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol StringConstantValue(string value, Symbol type = null) {
        Symbol r = new StringConstantValue(value);
        r.StaticType = type ?? ModelCore.StringType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol BooleanConstantValue(bool value, Symbol type = null) {
        Symbol r = new BooleanConstantValue(value);
        r.StaticType = type ?? ModelCore.BooleanType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol NumberConstantValue(double value, Symbol type = null) {
        Symbol r = new NumberConstantValue(value);
        r.StaticType = type ?? ModelCore.NumberType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol DecimalConstantValue(decimal value, Symbol type = null) {
        Symbol r = new DecimalConstantValue(value);
        r.StaticType = type ?? ModelCore.DecimalType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol ByteConstantValue(byte value, Symbol type = null) {
        Symbol r = new ByteConstantValue(value);
        r.StaticType = type ?? ModelCore.ByteType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol ShortConstantValue(short value, Symbol type = null) {
        Symbol r = new ShortConstantValue(value);
        r.StaticType = type ?? ModelCore.ShortType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol IntConstantValue(int value, Symbol type = null) {
        Symbol r = new IntConstantValue(value);
        r.StaticType = type ?? ModelCore.IntType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol LongConstantValue(long value, Symbol type = null) {
        Symbol r = new LongConstantValue(value);
        r.StaticType = type ?? ModelCore.LongType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol BigIntConstantValue(BigInteger value, Symbol type = null) {
        Symbol r = new BigIntConstantValue(value);
        r.StaticType = type ?? ModelCore.BigIntType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol EnumConstantValue(object value, Symbol type) {
        Symbol r = new EnumConstantValue(value);
        r.StaticType = type;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol TypeAsValue(Symbol type) {
        Symbol r = new TypeAsValue(type);
        r.StaticType = ModelCore.ClassType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol NamespaceAsValue(Symbol ns) {
        Symbol r = new NamespaceAsValue(ns);
        r.StaticType = ModelCore.AnyType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol NamespaceSetAsValue(Symbol nss) {
        Symbol r = new NamespaceSetAsValue(nss);
        r.StaticType = ModelCore.AnyType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol ThisValue(Symbol type) {
        Symbol r = new ThisValue();
        r.StaticType = type;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol ReferenceValueFromNamespace(Symbol @base, Symbol property) {
        if ((property is Type) || (property is Namespace) || (property is NamespaceSet) || (property is Alias)) {
            return property;
        }
        Symbol r = new ReferenceValueFromNamespace(@base, property);
        r.StaticType = property.StaticType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol ReferenceValueFromType(Symbol @base, Symbol property, Symbol definedByType) {
        if ((property is Type) || (property is Namespace) || (property is NamespaceSet) || (property is Alias)) {
            return property;
        }
        Symbol r = new ReferenceValueFromType(@base, property, definedByType);
        r.StaticType = property.StaticType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol ReferenceValueFromFrame(Symbol @base, Symbol property) {
        if ((property is Type) || (property is Namespace) || (property is NamespaceSet) || (property is Alias)) {
            return property;
        }
        Symbol r = new ReferenceValueFromFrame(@base, property);
        r.StaticType = property.StaticType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol ReferenceValue(Symbol @base, Symbol property, Symbol propertyDefinedByType) {
        if ((property is Type) || (property is Namespace) || (property is NamespaceSet) || (property is Alias)) {
            return property;
        }
        Symbol r = new ReferenceValue(@base, property, propertyDefinedByType);
        r.StaticType = property.StaticType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol DynamicReferenceValue(Symbol @base) {
        Symbol r = new DynamicReferenceValue(@base);
        r.StaticType = ModelCore.AnyType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol IndexValue(Symbol @base, Symbol type) {
        Symbol r = new IndexValue(@base);
        r.StaticType = type;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol DynamicIndexValue(Symbol @base) {
        Symbol r = new DynamicIndexValue(@base);
        r.StaticType = ModelCore.AnyType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol ConversionValue(Symbol @base, Symbol targetConversionType, ConversionFromTo fromTo, bool isOptional = false) {
        Symbol r = new ConversionValue(@base, fromTo, isOptional, targetConversionType);
        r.StaticType = isOptional ? targetConversionType.ToNullableType() : targetConversionType;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol FunctionExpValue(Symbol type) {
        Symbol r = new FunctionExpValue();
        r.StaticType = type;
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol Frame() {
        Symbol r = new Frame();
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol ActivationFrame() {
        Symbol r = new ActivationFrame();
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol ClassFrame(Symbol type) {
        Symbol r = new ClassFrame(type);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol EnumFrame(Symbol type) {
        Symbol r = new EnumFrame(type);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol InterfaceFrame(Symbol type) {
        Symbol r = new InterfaceFrame(type);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol NamespaceFrame(Symbol ns) {
        Symbol r = new NamespaceFrame(ns);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol PackageFrame(Symbol package) {
        Symbol r = new PackageFrame(package);
        r.ModelCore = ModelCore;
        return r;
    }

    public Symbol WithFrame(Symbol @object) {
        Symbol r = new WithFrame(@object);
        r.ModelCore = ModelCore;
        return r;
    }
}