namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using System.Numerics;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public class Value : Symbol {
    private Symbol m_Type = null;

    public override Symbol StaticType {
        get => m_Type;
        set => m_Type = value;
    }
}

public class ConstantValue : Value {
}

public class UndefinedConstantValue : ConstantValue {
}

public class NullConstantValue : ConstantValue {
}

/// <summary>
/// Used to indicate the default value of a type parameter.
/// </summary>
public class TypeParameterConstantValue : ConstantValue {
}

public class StringConstantValue : ConstantValue {
    private string m_V;

    public StringConstantValue(string value) {
        m_V = value;
    }

    public override string StringValue {
        get => m_V;
    }
}

public class BooleanConstantValue : ConstantValue {
    private bool m_V;

    public BooleanConstantValue(bool value) {
        m_V = value;
    }

    public override bool BooleanValue {
        get => m_V;
    }
}

public class NumberConstantValue : ConstantValue {
    private double m_V;

    public NumberConstantValue(double value) {
        m_V = value;
    }

    public override double NumberValue {
        get => m_V;
    }
}

public class DecimalConstantValue : ConstantValue {
    private decimal m_V;

    public DecimalConstantValue(decimal value) {
        m_V = value;
    }

    public override decimal DecimalValue {
        get => m_V;
    }
}

public class ByteConstantValue : ConstantValue {
    private byte m_V;

    public ByteConstantValue(byte value) {
        m_V = value;
    }

    public override byte ByteValue {
        get => m_V;
    }
}

public class ShortConstantValue : ConstantValue {
    private short m_V;

    public ShortConstantValue(short value) {
        m_V = value;
    }

    public override short ShortValue {
        get => m_V;
    }
}

public class IntConstantValue : ConstantValue {
    private int m_V;

    public IntConstantValue(int value) {
        m_V = value;
    }

    public override int IntValue {
        get => m_V;
    }
}

public class LongConstantValue : ConstantValue {
    private long m_V;

    public LongConstantValue(long value) {
        m_V = value;
    }

    public override long LongValue {
        get => m_V;
    }
}

public class BigIntConstantValue : ConstantValue {
    private BigInteger m_V;

    public BigIntConstantValue(BigInteger value) {
        m_V = value;
    }

    public override BigInteger BigIntValue {
        get => m_V;
    }
}

public class EnumConstantValue : ConstantValue {
    private object m_V;

    public EnumConstantValue(object value) {
        m_V = value;
    }

    public override object EnumConstValue {
        get => m_V;
    }
}

public class TypeAsValue : Value {
    private Symbol m_Type;

    public TypeAsValue(Symbol type) {
        m_Type = type;
    }

    public override Symbol TypeFromTypeAsValue {
        get => m_Type;
    }
}

public class NamespaceAsValue : Value {
    private Symbol m_NS;

    public NamespaceAsValue(Symbol ns) {
        m_NS = ns;
    }

    public override Symbol NamespaceFromNamespaceAsValue {
        get => m_NS;
    }
}

public class NamespaceSetAsValue : Value {
    private Symbol m_NSS;

    public NamespaceSetAsValue(Symbol nss) {
        m_NSS = nss;
    }

    public override Symbol NamespaceSetFromNamespaceSetAsValue {
        get => m_NSS;
    }
}

public class ThisValue : Value {
    public ThisValue() {
    }
}

public class ReferenceValueFromNamespace : Value {
    private Symbol m_Base;
    private Symbol m_Property;

    public ReferenceValueFromNamespace(Symbol @base, Symbol property) {
        m_Base = @base;
        m_Property = property;
    }

    public override Symbol Base {
        get => m_Base;
    }

    public override Symbol Property {
        get => m_Property;
    }

    public override bool ReadOnly {
        get => m_Property.ReadOnly;
    }

    public override bool WriteOnly {
        get => m_Property.WriteOnly;
    }

    public override bool PropertyIsVisibleTo(Symbol frame) {
        return m_Property.PropertyIsVisibleTo(frame);
    }
}

public class ReferenceValueFromType : Value {
    private Symbol m_Base;
    private Symbol m_Property;
    private Symbol m_DefinedByType;

    public ReferenceValueFromType(Symbol @base, Symbol property, Symbol definedByType) {
        m_Base = @base;
        m_Property = property;
        m_DefinedByType = definedByType;
    }

    public override Symbol Base {
        get => m_Base;
    }

    public override Symbol Property {
        get => m_Property;
    }

    public override Symbol PropertyDefinedByType {
        get => m_DefinedByType;
    }

    public override bool ReadOnly {
        get => m_Property.ReadOnly;
    }

    public override bool WriteOnly {
        get => m_Property.WriteOnly;
    }

    public override bool PropertyIsVisibleTo(Symbol frame) {
        return m_Property.PropertyIsVisibleTo(frame);
    }
}

public class ReferenceValueFromFrame : Value {
    private Symbol m_Base;
    private Symbol m_Property;

    public ReferenceValueFromFrame(Symbol @base, Symbol property) {
        m_Base = @base;
        m_Property = property;
    }

    public override Symbol Base {
        get => m_Base;
    }

    public override Symbol Property {
        get => m_Property;
    }

    public override bool ReadOnly {
        get => m_Property.ReadOnly;
    }

    public override bool WriteOnly {
        get => m_Property.WriteOnly;
    }

    public override bool PropertyIsVisibleTo(Symbol frame) {
        return m_Property.PropertyIsVisibleTo(frame);
    }
}

public class ReferenceValue : Value {
    private Symbol m_Base;
    private Symbol m_Property;
    private Symbol m_DefinedByType;

    public ReferenceValue(Symbol @base, Symbol property, Symbol definedByType) {
        m_Base = @base;
        m_Property = property;
        m_DefinedByType = definedByType;
    }

    public override Symbol Base {
        get => m_Base;
    }

    public override Symbol Property {
        get => m_Property;
    }

    public override Symbol PropertyDefinedByType {
        get => m_DefinedByType;
    }

    public override bool ReadOnly {
        get => m_Property.ReadOnly;
    }

    public override bool WriteOnly {
        get => m_Property.WriteOnly;
    }

    public override bool PropertyIsVisibleTo(Symbol frame) {
        return m_Property.PropertyIsVisibleTo(frame);
    }
}

public class DynamicReferenceValue : Value {
    private Symbol m_Base;

    public DynamicReferenceValue(Symbol @base) {
        m_Base = @base;
    }

    public override Symbol Base {
        get => m_Base;
    }

    public override bool ReadOnly {
        get => false;
    }

    public override bool WriteOnly {
        get => false;
    }
}

public class IndexValue : Value {
    private Symbol m_Base;

    public IndexValue(Symbol @base) {
        m_Base = @base;
    }

    public override Symbol Base {
        get => m_Base;
    }

    public override bool ReadOnly {
        get => InheritedProxies.Find(m_Base.StaticType, Operator.ProxyToSetIndex) == null;
    }

    public override bool WriteOnly {
        get => InheritedProxies.Find(m_Base.StaticType, Operator.ProxyToGetIndex) == null;
    }
}

public class DynamicIndexValue : Value {
    private Symbol m_Base;

    public DynamicIndexValue(Symbol @base) {
        m_Base = @base;
    }

    public override Symbol Base {
        get => m_Base;
    }

    public override bool ReadOnly {
        get => false;
    }

    public override bool WriteOnly {
        get => false;
    }
}

public class ConversionValue : Value {
    private Symbol m_Base;
    private ConversionFromTo m_FromTo;
    private bool m_Opt;
    private Symbol m_Target;

    public ConversionValue(Symbol @base, ConversionFromTo fromTo, bool opt, Symbol targetType) {
        m_Base = @base;
        m_FromTo = fromTo;
        m_Opt = opt;
        m_Target = targetType;
    }

    public override Symbol Base {
        get => m_Base;
    }

    public override Symbol ConversionTargetType {
        get => m_Target;
    }

    public override bool ConversionIsOptional {
        get => m_Opt;
    }

    public override ConversionFromTo ConversionFromTo {
        get => m_FromTo;
    }
}

public class FunctionExpValue : Value {
}

public class NullUnwrappedValue : Value {
}