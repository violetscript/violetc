namespace VioletScript.Parser.Operator;

using System.Collections.Generic;

/// <summary>
/// Operator precedence. Based on
/// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/Operator_Precedence
/// </summary>
public sealed class OperatorPrecedence {
    private static readonly Dictionary<int, OperatorPrecedence> m_ByValue = new Dictionary<int, OperatorPrecedence>();

    public static readonly OperatorPrecedence Postfix = new OperatorPrecedence(17);
    public static readonly OperatorPrecedence Unary = new OperatorPrecedence(16);
    public static readonly OperatorPrecedence Exponentiation = new OperatorPrecedence(15);
    public static readonly OperatorPrecedence Multiplicative = new OperatorPrecedence(14);
    public static readonly OperatorPrecedence Additive = new OperatorPrecedence(13);
    public static readonly OperatorPrecedence Shift = new OperatorPrecedence(12);
    public static readonly OperatorPrecedence Relational = new OperatorPrecedence(11);
    public static readonly OperatorPrecedence Equality = new OperatorPrecedence(10);
    public static readonly OperatorPrecedence BitwiseAnd = new OperatorPrecedence(9);
    public static readonly OperatorPrecedence BitwiseXor = new OperatorPrecedence(8);
    public static readonly OperatorPrecedence BitwiseOr = new OperatorPrecedence(7);
    public static readonly OperatorPrecedence LogicalAnd = new OperatorPrecedence(6);
    public static readonly OperatorPrecedence LogicalXor = new OperatorPrecedence(5);
    public static readonly OperatorPrecedence LogicalOr = new OperatorPrecedence(4);
    public static readonly OperatorPrecedence NullCoalescing = new OperatorPrecedence(3);
    public static readonly OperatorPrecedence AssignmentOrConditionalOrYieldOrFunction = new OperatorPrecedence(2);
    public static readonly OperatorPrecedence List = new OperatorPrecedence(1);

    private int m_V;

    public OperatorPrecedence(int v) {
        m_ByValue[v] = this;
        m_V = v;
    }

    public static OperatorPrecedence ValueOf(int v) {
        return m_ByValue[v];
    }

    public int ValueOf() {
        return m_V;
    }
}