namespace VioletScript.Parser.Operator;

using System.Collections.Generic;

public sealed class Operator {
    private static readonly Dictionary<int, Operator> m_ByValue = new Dictionary<int, Operator>();
    private static readonly Dictionary<Operator, bool> m_Unary = new Dictionary<Operator, bool>();
    private static readonly Dictionary<Operator, bool> m_AlwaysReturnBoolean = new Dictionary<Operator, bool>();

    public static readonly Operator Await = new Operator(0, "await");
    public static readonly Operator Yield = new Operator(1, "yield");
    public static readonly Operator As = new Operator(2, "as");
    public static readonly Operator AsStrict = new Operator(3, "as!");
    public static readonly Operator Instanceof = new Operator(4, "instanceof");
    public static readonly Operator Is = new Operator(5, "is");
    public static readonly Operator Delete = new Operator(6, "delete");
    public static readonly Operator Typeof = new Operator(7, "typeof");
    public static readonly Operator Void = new Operator(8, "void");
    public static readonly Operator LogicalNot = new Operator(9, "!");
    public static readonly Operator Positive = new Operator(0x0A, "+");
    public static readonly Operator Negate = new Operator(0x0B, "-");
    public static readonly Operator BitwiseNot = new Operator(0x0C, "~");
    public static readonly Operator Add = new Operator(0x0D, "+");
    public static readonly Operator Subtract = new Operator(0x0E, "-");
    public static readonly Operator Multiply = new Operator(0x0F, "*");
    public static readonly Operator Divide = new Operator(0x10, "/");
    public static readonly Operator Remainder = new Operator(0x11, "%");
    public static readonly Operator Pow = new Operator(0x12, "**");
    public static readonly Operator LogicalAnd = new Operator(0x13, "&&");
    public static readonly Operator LogicalXor = new Operator(0x14, "^^");
    public static readonly Operator LogicalOr = new Operator(0x15, "||");
    public static readonly Operator BitwiseAnd = new Operator(0x16, "&");
    public static readonly Operator BitwiseXor = new Operator(0x17, "^");
    public static readonly Operator BitwiseOr = new Operator(0x18, "|");
    public static readonly Operator LeftShift = new Operator(0x19, "<<");
    public static readonly Operator RightShift = new Operator(0x1A, ">>");
    public static readonly Operator UnsignedRightShift = new Operator(0x1B, ">>>");
    public static new readonly Operator Equals = new Operator(0x1C, "==");
    public static readonly Operator NotEquals = new Operator(0x1D, "!=");
    public static readonly Operator StrictEquals = new Operator(0x1E, "===");
    public static readonly Operator StrictNotEquals = new Operator(0x1F, "!==");
    public static readonly Operator Lt = new Operator(0x20, "<");
    public static readonly Operator Gt = new Operator(0x21, ">");
    public static readonly Operator Le = new Operator(0x22, "<=");
    public static readonly Operator Ge = new Operator(0x23, ">=");
    public static readonly Operator NonNull = new Operator(0x24, "!");
    public static readonly Operator PreIncrement = new Operator(0x25, "++");
    public static readonly Operator PreDecrement = new Operator(0x26, "--");
    public static readonly Operator PostIncrement = new Operator(0x27, "++");
    public static readonly Operator PostDecrement = new Operator(0x28, "--");
    public static readonly Operator ProxyToGetIndex = new Operator(0x29, "getIndex");
    public static readonly Operator ProxyToSetIndex = new Operator(0x2A, "setIndex");
    public static readonly Operator ProxyToDeleteIndex = new Operator(0x2B, "deleteIndex");
    public static readonly Operator ProxyToIterateKeys = new Operator(0x2C, "iterateKeys");
    public static readonly Operator ProxyToIterateValues = new Operator(0x2D, "iterateValues");
    /// <summary>`in` operator. Variant also used for proxy `has` public static readonly Operator declarations.</summary>
    public static readonly Operator In = new Operator(0x2E, "in");
    public static readonly Operator ProxyToConvertImplicit = new Operator(0x2F, "convertImplicit");
    public static readonly Operator ProxyToConvertExplicit = new Operator(0x30, "convertExplicit");
    public static readonly Operator NullCoalescing = new Operator(0x31, "??");

    private int m_V;
    private string m_S;

    public Operator(int value, string stringValue) {
        m_ByValue[value] = this;
        m_V = value;
        m_S = stringValue;
    }

    static Operator() {
        m_Unary[Operator.Await] = true;
        m_Unary[Operator.Yield] = true;
        m_Unary[Operator.Delete] = true;
        m_Unary[Operator.Typeof] = true;
        m_Unary[Operator.Void] = true;
        m_Unary[Operator.LogicalNot] = true;
        m_Unary[Operator.Positive] = true;
        m_Unary[Operator.Negate] = true;
        m_Unary[Operator.BitwiseNot] = true;
        m_Unary[Operator.NonNull] = true;
        m_Unary[Operator.PreIncrement] = true;
        m_Unary[Operator.PreDecrement] = true;
        m_Unary[Operator.PostIncrement] = true;
        m_Unary[Operator.PostDecrement] = true;

        m_AlwaysReturnBoolean[Operator.Equals] = true;
        m_AlwaysReturnBoolean[Operator.NotEquals] = true;
        m_AlwaysReturnBoolean[Operator.StrictEquals] = true;
        m_AlwaysReturnBoolean[Operator.StrictNotEquals] = true;
        m_AlwaysReturnBoolean[Operator.Lt] = true;
        m_AlwaysReturnBoolean[Operator.Gt] = true;
        m_AlwaysReturnBoolean[Operator.Le] = true;
        m_AlwaysReturnBoolean[Operator.Ge] = true;
        m_AlwaysReturnBoolean[Operator.Delete] = true;
        m_AlwaysReturnBoolean[Operator.LogicalNot] = true;
        m_AlwaysReturnBoolean[Operator.In] = true;
        m_AlwaysReturnBoolean[Operator.Is] = true;
        m_AlwaysReturnBoolean[Operator.Instanceof] = true;
    }

    public static Operator ValueOf(int v) {
        return m_ByValue[v];
    }

    public int ValueOf() {
        return m_V;
    }

    public bool IsUnary {
        get => m_Unary[this] == true;
    }

    public bool AlwaysReturnsBoolean {
        get => m_AlwaysReturnBoolean[this] == true;
    }

    public bool IsConversionProxy
    {
        get => this == ProxyToConvertImplicit || this == ProxyToConvertExplicit;
    }

    public int ProxyNumberOfParameters {
        get {
            if (this == Operator.ProxyToGetIndex
            ||  this == Operator.ProxyToDeleteIndex
            ||  this == Operator.ProxyToConvertImplicit
            ||  this == Operator.ProxyToConvertExplicit
            ||  this == Operator.In)
            {
                return 1;
            }
            if (this == Operator.ProxyToIterateKeys
            ||  this == Operator.ProxyToIterateValues)
            {
                return 0;
            }
            return IsUnary ? 1 : 2;
        }
    }

    public override string ToString() {
        return m_S;
    }
}