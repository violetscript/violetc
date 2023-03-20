namespace VioletScript.Parser.Ast;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Source;
using VioletScript.Parser.Semantic.Model;

public class Node {
    public Span? Span = null;

    public virtual string Name {
        get => "";
        set {}
    }
}

public class TypeExpression : Node {
    public bool SemanticResolved = false;
    public Symbol SemanticSymbol = null;
}

public class TypedTypeExpression : TypeExpression {
    public TypeExpression Base;
    public TypeExpression Type;

    public TypedTypeExpression(TypeExpression @base, TypeExpression type) : base() {
        Base = @base;
        Type = type;
    }
}

public class IdentifierTypeExpression : TypeExpression {
    private string m_Name;

    public IdentifierTypeExpression(string name) : base() {
        m_Name = name;
    }

    public override string Name {
        get => m_Name;
    }
}

/// <summary>
/// Member type expression.
/// If the base object is a package, then the member may be a subpackage. 
/// </summary>
public class MemberTypeExpression : TypeExpression {
    public TypeExpression Base;
    public Identifier Id;

    public MemberTypeExpression(TypeExpression @base, Identifier id) : base() {
        Base = @base;
        Id = id;
    }
}

public class AnyTypeExpression : TypeExpression {
}

public class VoidTypeExpression : TypeExpression {
}

public class UndefinedTypeExpression : TypeExpression {
}

public class NullTypeExpression : TypeExpression {
}

public class FunctionTypeExpression : TypeExpression {
    public List<Identifier> Params;
    public List<Identifier> OptParams;
    public Identifier RestParam;
    /// <summary>
    /// Return type. Omitting this should give the type <c>*</c>.
    /// </summary>
    public TypeExpression ReturnType;

    public FunctionTypeExpression(List<Identifier> @params, List<Identifier> optParams, Identifier restParam, TypeExpression returnType) : base() {
        Params = @params;
        OptParams = optParams;
        RestParam = restParam;
        ReturnType = returnType;
    }
}

public class ArrayTypeExpression : TypeExpression {
    public TypeExpression ItemType;

    public ArrayTypeExpression(TypeExpression itemType) : base() {
        ItemType = itemType;
    }
}

public class TupleTypeExpression : TypeExpression {
    public List<TypeExpression> ItemTypes;

    public TupleTypeExpression(List<TypeExpression> itemTypes) : base() {
        ItemTypes = itemTypes;
    }
}

public class RecordTypeExpression : TypeExpression {
    public List<Identifier> Fields;

    public RecordTypeExpression(List<Identifier> fields) : base() {
        Fields = fields;
    }
}

public class UnionTypeExpression : TypeExpression {
    public List<TypeExpression> Types;

    public UnionTypeExpression(List<TypeExpression> types) : base() {
        Types = types;
    }
}

public class GenericInstantiationTypeExpression : TypeExpression {
    public TypeExpression Base;
    public List<TypeExpression> ArgumentsList;

    public GenericInstantiationTypeExpression(TypeExpression @base, List<TypeExpression> argumentsList) : base() {
        Base = @base;
        ArgumentsList = argumentsList;
    }
}

public class NullableTypeExpression : TypeExpression {
    public TypeExpression Base;

    public NullableTypeExpression(TypeExpression @base) : base() {
        Base = @base;
    }
}

public class NonNullableTypeExpression : TypeExpression {
    public TypeExpression Base;

    public NonNullableTypeExpression(TypeExpression @base) : base() {
        Base = @base;
    }
}

public class ParensTypeExpression : TypeExpression {
    public TypeExpression Base;

    public ParensTypeExpression(TypeExpression @base) : base() {
        Base = @base;
    }
}

public class DestructuringPattern : Node {
    public TypeExpression Type;

    /// <summary>
    /// For a binding, indicates a variable slot.
    /// For an assignment, <c>SemanticProperty</c> is null.
    /// </summary>
    public Symbol SemanticProperty = null;

    public DestructuringPattern(TypeExpression type) : base() {
        Type = type;
    }
}

/// <summary>
/// Either a binding or assignment pattern.
/// </summary>
public class BindPattern : DestructuringPattern {
    private string m_Name;

    /// <summary>
    /// For an assignment pattern, indicates a reference value from a lexical frame.
    /// </summary>
    public Symbol SemanticFrameAssignedReference = null;

    public BindPattern(string name, TypeExpression type) : base(type) {
        m_Name = name;
    }

    public override string Name {
        get => m_Name;
    }
}

public class RecordDestructuringPattern : DestructuringPattern {
    public List<RecordDestructuringPatternField> Fields;

    public RecordDestructuringPattern(List<RecordDestructuringPatternField> fields, TypeExpression type) : base(type) {
        Fields = fields;
    }
}

public class RecordDestructuringPatternField : Node {
    /// <summary>
    /// Key. For variable bindings, this is only allowed to be
    /// a string literal.
    /// </summary>
    public Expression Key;
    /// <summary>Optional pattern.</summary>
    public DestructuringPattern Subpattern;

    /// <summary>
    /// Semantic property. This is null if there is a subpattern.
    /// </summary>
    public Symbol SemanticProperty;

    /// <summary>
    /// For an assignment pattern, indicates a reference value from a lexical frame.
    /// </summary>
    public Symbol SemanticFrameAssignedReference = null;

    public RecordDestructuringPatternField(Expression key, DestructuringPattern subpattern) : base() {
        Key = key;
        Subpattern = subpattern;
    }
}

public class ArrayDestructuringPattern : DestructuringPattern {
    /// <summary>Item. Either <c>null</c>, <c>DestructuringPattern</c> or <c>ArrayDestructuringSpread</c>.</summary>
    public List<Node> Items;

    public ArrayDestructuringPattern(List<Node> items, TypeExpression type) : base(type) {
        Items = items;
    }
}

public class ArrayDestructuringSpread : Node {
    public DestructuringPattern Pattern;

    public ArrayDestructuringSpread(DestructuringPattern pattern) : base() {
        Pattern = pattern;
    }
}

public class VariableBinding : Node {
    public DestructuringPattern Pattern;
    /// <summary>Optional initializer.</summary>
    public Expression Init;

    public bool SemanticVerified = false;

    public VariableBinding(DestructuringPattern pattern, Expression init) : base() {
        Pattern = pattern;
        Init = init;
    }
}

public class Expression : Node {
    public Symbol SemanticSymbol = null;
    /// <summary>
    /// Internally used by the verifier.
    /// </summary>
    public bool SemanticExpResolved = false;
    /// <summary>
    /// Internally used by the verifier.
    /// </summary>
    public bool SemanticConstantExpResolved = false;
}

public class Identifier : Expression {
    private string m_Name;
    public TypeExpression Type;

    public Identifier(string name, TypeExpression type = null) : base() {
        m_Name = name;
        Type = type;
    }

    public override string Name {
        get => m_Name;
    }
}

public class EmbedExpression : Expression {
    public string Source;
    /// <summary>Optional type annotation.</summary>
    public TypeExpression Type;

    public EmbedExpression(string source, TypeExpression type) : base() {
        Source = source;
        Type = type;
    }
}

public class UnaryExpression : Expression {
    public Operator Operator;
    public Expression Operand;

    public UnaryExpression(Operator op, Expression operand) : base() {
        Operator = op;
        Operand = operand;
    }
}

/// <summary>
/// Binary expression.
/// </summary>
///
public class BinaryExpression : Expression {
    public Operator Operator;
    public Expression Left;
    public Expression Right;

    public BinaryExpression(Operator op, Expression l, Expression r) : base() {
        Operator = op;
        Left = l;
        Right = r;
    }
}

public class TypeBinaryExpression : Expression {
    public Operator Operator;
    public Expression Left;
    public TypeExpression Right;
    /// <summary>
    /// Non-null if this node was originated from an expression like
    /// <c>x is y:C</c>.
    /// </summary>
    public Identifier BindsTo;

    public TypeBinaryExpression(Operator op, Expression l, TypeExpression r, Identifier bindsTo) : base() {
        Operator = op;
        Left = l;
        Right = r;
        BindsTo = bindsTo;
    }
}

public class DefaultExpression : Expression {
    public TypeExpression Type;

    public DefaultExpression(TypeExpression type) : base() {
        Type = type;
    }
}

public class FunctionExpression : Expression {
    /// <summary>Optional name.</summary>
    public Identifier Id;
    public FunctionCommon Common;

    public FunctionExpression(Identifier id, FunctionCommon common) : base() {
        Id = id;
        Common = common;
    }
}

public class ObjectInitializer : Expression {
    /// <summary>List of <c>ObjectField</c> and <c>Spread</c></summary>
    public List<Node> Fields;
    public TypeExpression Type;

    public ObjectInitializer(List<Node> fields, TypeExpression type) : base() {
        Fields = fields;
        Type = type;
    }
}

public class ObjectField : Node {
    /// <summary>
    /// Field key. If source was a valid identifier,
    /// it is constructed as a <c>StringLiteral</c> node.
    /// </summary>
    public Expression Key;
    /// <summary>
    /// Value, optional for shorthand fields.
    /// </summary>
    public Expression Value;

    /// <summary>
    /// For a shorthand field, holds the assigned value from the lexical reference.
    /// </summary>
    /// <remarks>
    /// The verifier performs implicit conversion when the shorthand reference
    /// is assigned to an object field expecting a slightly different type.
    /// </remarks>
    public Symbol SemanticShorthand = null;

    public ObjectField(Expression key, Expression value) : base() {
        Key = key;
        Value = value;
    }
}

public class Spread : Expression {
    public Expression Expression;

    public Spread(Expression expression) : base() {
        Expression = expression;
    }
}

public class ArrayInitializer : Expression {
    /// <summary>List of null (hole), <c>Expression</c> and <c>Spread</c>.</summary>
    public List<Expression> Items;
    public TypeExpression Type;

    public ArrayInitializer(List<Expression> items, TypeExpression type) : base() {
        Items = items;
        Type = type;
    }
}

public class MarkupListInitializer : Expression {
    /// <summary>List of <c>Expression</c> and <c>Spread</c> with curly brackets.</summary>
    public List<Expression> Children;

    public MarkupListInitializer(List<Expression> children) : base() {
        Children = children;
    }
}

public class MarkupInitializer : Expression {
    /// <summary>
    /// Member expression or identifier.
    /// </summary>
    public Expression Id;
    public List<MarkupAttribute> Attributes;
    /// <summary>Null if node is empty. Contains child node initializers and <c>Spread</c> with curly brackets.</summary>
    public List<Expression> Children;

    public MarkupInitializer(Expression id, List<MarkupAttribute> attributes, List<Expression> children) : base() {
        Id = id;
        Attributes = attributes;
        Children = children;
    }
}

public class MarkupAttribute : Node {
    public Identifier Id;
    /// <summary>Attribute value. If null, equivalent to <c>attrib={true}</c>.</summary>
    public Expression Value;

    public MarkupAttribute(Identifier id, Expression @value) : base() {
        Id = id;
        Value = @value;
    }
}

/// <summary>
/// Member expression. If the base object is a package, then the member may be a subpackage. 
/// </summary>
/// <remarks>
/// <para>Optional member access:</para>
///
/// <list type="bullet">
/// <item>If the member access is optional (using <c>?.</c> syntax),
/// the member result type unifies with either null or undefined or both, and the <c>SemanticThrowawayNonNullBase</c>
/// and <c>SemanticOptNonNullUnifiedSymbol</c> properties of this node are assigned to some symbol.</item>
/// <item>If the base includes undefined but not null, the result unifies with undefined as <c>undefined|R</c>.</item>
/// <item>If the base includes null but not undefined, the result unifies with null as <c>null|R</c>.</item>
/// <item>If the base includes both undefined and null, the result unifies first with undefined and then null, as <c>undefined|null|R</c>.</item>
/// </list>
/// </remarks>
public class MemberExpression : Expression {
    public Expression Base;
    public Identifier Id;
    public bool Optional;

    /// <summary>
    /// If this is an optional member, this stores
    /// a throw-away non-null value corresponding to the base value;
    /// that is, it is a value of type <c>Base.SemanticSymbol.ToNonNullableType()</c>.
    /// </summary>
    public Symbol SemanticThrowawayNonNullBase = null;

    /// <summary>
    /// If this is an optional member, this stores
    /// the resolved symbol without unifying it to null or undefined types.
    /// </summary>
    public Symbol SemanticOptNonNullUnifiedSymbol = null;

    public MemberExpression(Expression @base, Identifier id, bool optional = false) : base() {
        Base = @base;
        Id = id;
        Optional = optional;
    }
}

/// <summary>
/// Index expression.
/// </summary>
/// <remarks>
/// <para>Optional indexing:</para>
///
/// <list type="bullet">
/// <item>If the index access is optional (using <c>?.[k]</c> syntax),
/// the result type unifies with either null or undefined or both, and the <c>SemanticThrowawayNonNullBase</c>
/// property of this node are assigned to some symbol.</item>
/// <item>If the base includes undefined but not null, the result unifies with undefined as <c>undefined|R</c>.</item>
/// <item>If the base includes null but not undefined, the result unifies with null as <c>null|R</c>.</item>
/// <item>If the base includes both undefined and null, the result unifies first with undefined and then null, as <c>undefined|null|R</c>.</item>
/// </list>
/// </remarks>
public class IndexExpression : Expression {
    public Expression Base;
    public Expression Key;
    public bool Optional;

    /// <summary>
    /// If this is an optional index, this stores
    /// a throw-away non-null value corresponding to the base value;
    /// that is, it is a value of type <c>Base.SemanticSymbol.ToNonNullableType()</c>.
    /// </summary>
    public Symbol SemanticThrowawayNonNullBase = null;

    public IndexExpression(Expression @base, Expression key, bool optional = false) : base() {
        Base = @base;
        Key = key;
        Optional = optional;
    }
}

public class CallExpression : Expression {
    public Expression Base;
    public List<Expression> ArgumentsList;
    public bool Optional;

    /// <summary>
    /// If this is an optional call, this stores
    /// a throw-away non-null value corresponding to the base value;
    /// that is, it is a value of type <c>Base.SemanticSymbol.ToNonNullableType()</c>.
    /// </summary>
    public Symbol SemanticThrowawayNonNullBase = null;

    public CallExpression(Expression @base, List<Expression> argumentsList, bool optional = false) : base() {
        Base = @base;
        ArgumentsList = argumentsList;
        Optional = optional;
    }
}

public class ImportMetaExpression : Expression {
}

public class ThisLiteral : Expression {
}

public class StringLiteral : Expression {
    public string Value;

    public StringLiteral(string v) : base() {
        Value = v;
    }
}

public class NullLiteral : Expression {
}

public class BooleanLiteral : Expression {
    public bool Value;

    public BooleanLiteral(bool v) : base() {
        Value = v;
    }
}

public class NumericLiteral : Expression {
    public double Value;

    public NumericLiteral(double v) : base() {
        Value = v;
    }
}

public class RegExpLiteral : Expression {
    public string Body;
    public string Flags;

    public RegExpLiteral(string body, string flags) : base() {
        Body = body;
        Flags = flags;
    }
}

public class ConditionalExpression : Expression {
    public Expression Test;
    public Expression Consequent;
    public Expression Alternative;

    /// <summary>
    /// After verification, if non-null, indicates the
    /// dominant result type is the alternative's type.
    /// </summary>
    public Symbol SemanticConseqToAltConv = null;
    /// <summary>
    /// After verification, if non-null, indicates the
    /// dominant result type is the consequent's type.
    /// </summary>
    public Symbol SemanticAltToConseqConv = null;

    public ConditionalExpression(Expression test, Expression consequent, Expression alternative) : base() {
        Test = test;
        Consequent = consequent;
        Alternative = alternative;
    }
}

public class ParensExpression : Expression {
    public Expression Expression;

    public ParensExpression(Expression expression) : base() {
        Expression = expression;
    }
}

public class ListExpression : Expression {
    public List<Expression> Expressions;

    public ListExpression(List<Expression> expressions) : base() {
        Expressions = expressions;
    }
}

public class GenericInstantiationExpression : Expression {
    public Expression Base;
    public List<TypeExpression> ArgumentsList;

    public GenericInstantiationExpression(Expression @base, List<TypeExpression> argumentsList) : base() {
        Base = @base;
        ArgumentsList = argumentsList;
    }
}

public class AssignmentExpression : Expression {
    /// <summary>
    /// Left, either a <c>Expression</c> or <c>DestructuringPattern</c>.
    /// This is never a <c>BindPattern</c> node.
    /// </summary>
    public Node Left;
    /// <summary>
    /// Compound operator. If left operand is a destructuring pattern,
    /// the compound operator is always null.
    /// </summary>
    public Operator Compound;
    public Expression Right;

    public AssignmentExpression(Node left, Operator compound, Expression right) : base() {
        Left = left;
        Compound = compound;
        Right = right;
    }
}

public class NewExpression : Expression {
    public Expression Base;
    public List<Expression> ArgumentsList;

    public NewExpression(Expression @base, List<Expression> argumentsList) : base() {
        Base = @base;
        ArgumentsList = argumentsList;
    }
}

public class SuperExpression : Expression {
}

public class Program : Node {
    public List<PackageDefinition> Packages;
    /// <summary>
    /// Optional sequence of statements.
    /// </summary>
    public List<Statement> Statements;

    public Program(List<PackageDefinition> packages, List<Statement> statements) : base() {
        Packages = packages;
        Statements = statements;
    }
}

public class PackageDefinition: Node {
    /// <summary>
    /// Dot-delimited identifier, empty for <c>package {}</c>.
    /// </summary>
    public string[] Id;
    public Block Block;

    public PackageDefinition(string[] id, Block block) : base() {
        Id = id;
        Block = block;
    }
}

public class Statement : Node {
}

public class AnnotatableDefinition : Statement {
    /// <summary>Possibly null list of decorators.</summary>
    public List<Expression> Decorators;
    public AnnotatableDefinitionModifier Modifiers;
    public AnnotatableDefinitionAccessModifier? AccessModifier;
}

[Flags]
public enum AnnotatableDefinitionModifier {
    Final = 1,
    Native = 2,
    Override = 4,
    Proxy = 8,
    Static = 16,
}

public static class AnnotatableDefinitionModifierMethods {
    public static string Name(this AnnotatableDefinitionModifier m) {
        if (m == AnnotatableDefinitionModifier.Final) return "final";
        if (m == AnnotatableDefinitionModifier.Native) return "native";
        if (m == AnnotatableDefinitionModifier.Override) return "override";
        if (m == AnnotatableDefinitionModifier.Proxy) return "proxy";
        if (m == AnnotatableDefinitionModifier.Static) return "static";
        return "";
    }
}

public enum AnnotatableDefinitionAccessModifier {
    Public,
    Private,
    Protected,
    Internal,
}

public static class AnnotatableDefinitionAccessModifierMethods {
    public static string Name(this AnnotatableDefinitionAccessModifier m) {
        if (m == AnnotatableDefinitionAccessModifier.Public) return "public";
        if (m == AnnotatableDefinitionAccessModifier.Private) return "private";
        if (m == AnnotatableDefinitionAccessModifier.Protected) return "protected";
        if (m == AnnotatableDefinitionAccessModifier.Internal) return "internal";
        return "";
    }
}

public class NamespaceDefinition : AnnotatableDefinition {
    public Identifier Id;
    public Block Block;

    public NamespaceDefinition(Identifier id, Block block) : base() {
        Id = id;
        Block = block;
    }
}

public class NamespaceAliasDefinition : AnnotatableDefinition {
    public Identifier Id;
    public Expression Expression;

    public NamespaceAliasDefinition(Identifier id, Expression expression) : base() {
        Id = id;
        Expression = expression;
    }
}

public class VariableDefinition : AnnotatableDefinition {
    public bool ReadOnly;
    public List<VariableBinding> Bindings;

    public VariableDefinition(bool readOnly, List<VariableBinding> bindings) : base() {
        ReadOnly = readOnly;
        Bindings = bindings;
    }
}

public class FunctionDefinition : AnnotatableDefinition {
    public Identifier Id;
    public Generics Generics;
    public FunctionCommon Common;

    public FunctionDefinition(Identifier id, Generics generics, FunctionCommon common) : base() {
        Id = id;
        Generics = generics;
        Common = common;
    }
}

public class ConstructorDefinition : AnnotatableDefinition {
    public Identifier Id;
    public FunctionCommon Common;

    public ConstructorDefinition(Identifier id, FunctionCommon common) : base() {
        Id = id;
        Common = common;
    }
}

public class GetterDefinition : AnnotatableDefinition {
    public Identifier Id;
    public FunctionCommon Common;

    public GetterDefinition(Identifier id, FunctionCommon common) : base() {
        Id = id;
        Common = common;
    }
}

public class SetterDefinition : AnnotatableDefinition {
    public Identifier Id;
    public FunctionCommon Common;

    public SetterDefinition(Identifier id, FunctionCommon common) : base() {
        Id = id;
        Common = common;
    }
}

public class ProxyDefinition : AnnotatableDefinition {
    public Identifier Id;
    /// <summary>
    /// Operator. For <c>proxy function has()</c>, this property is set to <c>Operator.In</c>.
    /// </summary>
    public Operator Operator;
    public FunctionCommon Common;

    public ProxyDefinition(Identifier id, Operator op, FunctionCommon common) : base() {
        Id = id;
        Operator = op;
        Common = common;
    }
}

public class FunctionCommon : Node {
    public bool UsesAwait;
    public bool UsesYield;
    public List<VariableBinding> Params;
    public List<VariableBinding> OptParams;
    public VariableBinding RestParam;
    public TypeExpression ReturnType;
    public TypeExpression ThrowsType;
    public Node Body;

    public Symbol SemanticActivation = null;

    public FunctionCommon(
        bool usesAwait,
        bool usesYield,
        List<VariableBinding> @params,
        List<VariableBinding> optParams,
        VariableBinding restParam,
        TypeExpression returnType,
        TypeExpression throwsType,
        Node body
    ) : base() {
        UsesAwait = usesAwait;
        UsesYield = usesYield;
        Params = @params;
        OptParams = optParams;
        RestParam = restParam;
        ReturnType = returnType;
        ThrowsType = throwsType;
        Body = body;
    }
}

public class ClassDefinition : AnnotatableDefinition {
    public Identifier Id;
    public bool IsValue;
    public bool DontInit;
    public Generics Generics;
    public TypeExpression ExtendsType;
    public List<TypeExpression> ImplementsList;
    public Block Block;

    public ClassDefinition(
        Identifier id,
        bool isValue,
        bool dontInit,
        Generics generics,
        TypeExpression extendsType,
        List<TypeExpression> implementsList,
        Block block
    ) : base() {
        Id = id;
        IsValue = isValue;
        DontInit = dontInit;
        Generics = generics;
        ExtendsType = extendsType;
        ImplementsList = implementsList;
        Block = block;
    }
}

public class InterfaceDefinition : AnnotatableDefinition {
    public Identifier Id;
    public Generics Generics;
    public List<TypeExpression> ExtendsList;
    public Block Block;

    public InterfaceDefinition(
        Identifier id,
        Generics generics,
        List<TypeExpression> extendsList,
        Block block
    ) : base() {
        Id = id;
        Generics = generics;
        ExtendsList = extendsList;
        Block = block;
    }
}

public class EnumDefinition : AnnotatableDefinition {
    public Identifier Id;
    public bool IsFlags;
    public TypeExpression NumericType;
    public Block Block;

    public EnumDefinition(Identifier id, bool isFlags, TypeExpression numericType, Block block) : base() {
        Id = id;
        IsFlags = isFlags;
        NumericType = numericType;
        Block = block;
    }
}

public class TypeDefinition : AnnotatableDefinition {
    public Identifier Id;
    public Generics Generics;
    public TypeExpression Type;

    public TypeDefinition(Identifier id, Generics generics, TypeExpression type) : base() {
        Id = id;
        Generics = generics;
        Type = type;
    }
}

/// <summary>
/// Covers the generic parts of a declaration. This includes both
/// the nodes that declare type parameters and the nodes that specify
/// bounds.
/// </summary>
public class Generics : Node {
    public List<GenericTypeParameter> Params;
    /// <summary>Possibly null list of bounds.</summary>
    public List<GenericTypeParameterBound> Bounds = null;

    public Generics(List<GenericTypeParameter> @params) : base() {
        Params = @params;
    }
}

public class GenericTypeParameter : Node {
    public Identifier Id;
    /// <summary>Optional type annotation following the identifier.</summary>
    public TypeExpression DefaultIsBound;

    public GenericTypeParameter(Identifier id, TypeExpression defaultIsBound) : base() {
        Id = id;
        DefaultIsBound = defaultIsBound;
    }
}

public class GenericTypeParameterBound : Node {
}

/// <summary>
/// Node for the clause <c>where T is B</c>.
/// </summary>
public class GenericTypeParameterIsBound : GenericTypeParameterBound {
    public Identifier Id;
    public TypeExpression Type;

    public GenericTypeParameterIsBound(Identifier id, TypeExpression type) : base() {
        Id = id;
        Type = type;
    }
}

public class ExpressionStatement : Statement {
    public Expression Expression;

    public ExpressionStatement(Expression expression) : base() {
        Expression = expression;
    }
}

public class EmptyStatement : Statement {
}

public class Block : Statement {
    public List<Statement> Statements;

    public Block(List<Statement> statements) : base() {
        Statements = statements;
    }
}

public class SuperStatement : Statement {
    public List<Expression> ArgumentsList;

    public SuperStatement(List<Expression> argumentsList) : base() {
        ArgumentsList = argumentsList;
    }
}

public class ImportStatement : Statement {
    /// <summary>Possibly null alias.</summary>
    public Identifier Alias;
    public string[] ImportName;
    public bool Wildcard;

    public ImportStatement(Identifier alias, string[] importName, bool wildcard) : base() {
        Alias = alias;
        ImportName = importName;
        Wildcard = wildcard;
    }
}

public class IfStatement : Statement {
    public Expression Test;
    public Statement Consequent;
    public Statement Alternative;

    public IfStatement(Expression test, Statement consequent, Statement alternative) : base() {
        Test = test;
        Consequent = consequent;
        Alternative = alternative;
    }
}

public class DoStatement : Statement {
    public Statement Body;
    public Expression Test;

    public DoStatement(Statement body, Expression test) : base() {
        Body = body;
        Test = test;
    }
}

public class WhileStatement : Statement {
    public Expression Test;
    public Statement Body;

    public WhileStatement(Expression test, Statement body) : base() {
        Test = test;
        Body = body;
    }
}

public class BreakStatement : Statement {
    /// <summary>Possibly null.</summary>
    public string Label;

    public BreakStatement(string label) : base() {
        Label = label;
    }
}

public class ContinueStatement : Statement {
    /// <summary>Possibly null.</summary>
    public string Label;

    public ContinueStatement(string label) : base() {
        Label = label;
    }
}

public class ReturnStatement : Statement {
    /// <summary>Possibly null.</summary>
    public Expression Expression;

    public ReturnStatement(Expression expression) : base() {
        Expression = expression;
    }
}

public class ThrowStatement : Statement {
    public Expression Expression;

    public ThrowStatement(Expression expression) : base() {
        Expression = expression;
    }
}

public class TryStatement : Statement {
    public Block Block;
    public List<CatchClause> CatchClauses;
    public Block FinallyBlock;

    public TryStatement(Block block, List<CatchClause> catchClauses, Block finallyBlock) : base() {
        Block = block;
        CatchClauses = catchClauses;
        FinallyBlock = finallyBlock;
    }
}

public class CatchClause : Node {
    public DestructuringPattern Pattern;
    public Block Block;

    public CatchClause(DestructuringPattern pattern, Block block) : base() {
        Pattern = pattern;
        Block = block;
    }
}

public class LabeledStatement : Statement {
    public string Label;
    public Statement Statement;

    public LabeledStatement(string label, Statement statement) : base() {
        Label = label;
        Statement = statement;
    }
}

public class ForStatement : Statement {
    /// <summary>
    /// Either null, <c>Expression</c> or <c>SimpleVariableDeclaration</c>.
    /// </summary>
    public Node Init;
    /// <summary>Possibly null.</summary>
    public Expression Test;
    /// <summary>Possibly null.</summary>
    public Expression Update;
    public Statement Body;

    public ForStatement(Node init, Expression test, Expression update, Statement body) : base() {
        Init = init;
        Test = test;
        Update = update;
        Body = body;
    }
}

public class ForInStatement : Statement {
    public bool IteratesKeys;
    /// <summary>
    /// Either <c>Expression</c> or <c>SimpleVariableDeclaration</c>.
    /// </summary>
    public Node Left;
    public Expression Right;
    public Statement Body;

    public ForInStatement(bool iteratesKeys, Node left, Expression right, Statement body) : base() {
        IteratesKeys = iteratesKeys;
        Left = left;
        Right = right;
        Body = body;
    }
}

public class SimpleVariableDeclaration : Statement {
    public bool ReadOnly;
    public List<VariableBinding> Bindings;

    public SimpleVariableDeclaration(bool readOnly, List<VariableBinding> bindings) : base() {
        ReadOnly = readOnly;
        Bindings = bindings;
    }
}

public class SwitchStatement : Statement {
    public Expression Discriminant;
    public List<SwitchCase> Cases;

    public SwitchStatement(Expression discriminant, List<SwitchCase> cases) : base() {
        Discriminant = discriminant;
        Cases = cases;
    }
}

public class SwitchCase : Node {
    /// <summary>Test. Null for a <c>default</c> case.</summary>
    public Expression Test;
    public List<Statement> Consequent;

    public SwitchCase(Expression test, List<Statement> consequent) : base() {
        Test = test;
        Consequent = consequent;
    }
}

public class SwitchTypeStatement : Statement {
    public Expression Discriminant;
    public List<SwitchTypeCase> Cases;

    public SwitchTypeStatement(Expression discriminant, List<SwitchTypeCase> cases) : base() {
        Discriminant = discriminant;
        Cases = cases;
    }
}

public class SwitchTypeCase : Node {
    /// <summary>Destructuring pattern. Null for a <c>default</c> case.</summary>
    public DestructuringPattern Pattern;
    public Block Block;

    public SwitchTypeCase(DestructuringPattern pattern, Block block) : base() {
        Pattern = pattern;
        Block = block;
    }
}

public class IncludeStatement : Statement {
    public string Source;
    public Script InnerScript = null;
    public List<Statement> InnerStatements = new List<Statement> {};

    public IncludeStatement(string source) : base() {
        Source = source;
    }
}

public class UseNamespaceStatement : Statement {
    public Expression Expression;

    public UseNamespaceStatement(Expression expression) : base() {
        Expression = expression;
    }
}

public class UseResourceStatement : Statement {
    public List<VariableBinding> Bindings;
    public Block Block;

    public UseResourceStatement(List<VariableBinding> bindings, Block block) : base() {
        Bindings = bindings;
        Block = block;
    }
}

public class WithStatement : Statement {
    public Expression Object;
    public Statement Body;

    public WithStatement(Expression @object, Statement body) : base() {
        Object = @object;
        Body = body;
    }
}