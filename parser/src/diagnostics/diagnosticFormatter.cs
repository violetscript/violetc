namespace VioletScript.Parser.Diagnostic;

using VioletScript.Parser.Source;
using VioletScript.Parser.Tokenizer;
using VioletScript.Parser.Ast;
using static VioletScript.Parser.Ast.AnnotatableDefinitionModifierMethods;
using static VioletScript.Parser.Ast.AnnotatableDefinitionAccessModifierMethods;

using System.Collections.Generic;
using System.Text.RegularExpressions;

public interface IDiagnosticFormatter {
    string Format(Diagnostic p) {
        var msg = FormatArguments(p);
        msg = msg.Substring(0, 1).ToUpper() + msg.Substring(1);
        String file = p.Span.Script.FilePath;
        if (file.StartsWith("/"))
        {
            file = file.Substring(1);
        }
        file = new System.Uri("file://" + file).AbsoluteUri;
        if (file.EndsWith('/'))
        {
            file = file.Substring(0, file.Count() - 1);
        }
        var line = p.Span.FirstLine.ToString();
        var column = (p.Span.FirstColumn + 1).ToString();
        var k = p.KindString;
        return $"{file}:{line}:{column}: {k} #{p.Id.ToString()}: {msg}";
    }

    string FormatRelative(Diagnostic p, string basePath) {
        var msg = FormatArguments(p);
        msg = msg.Substring(0, 1).ToUpper() + msg.Substring(1);
        String file = Path.GetRelativePath(basePath, p.Span.Script.FilePath);
        var line = p.Span.FirstLine.ToString();
        var column = (p.Span.FirstColumn + 1).ToString();
        var k = p.KindString;
        return $"{file}:{line}:{column}: {k} #{p.Id.ToString()}: {msg}";
    }

    string FormatArguments(Diagnostic p) {
        var regex = new Regex(@"\$(\$|[a-z0-9_\-]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        return regex.Replace(ResolveId(p.Id), delegate(Match m) {
            var s = m.Captures[0].ToString().Substring(1);
            if (s == "$") {
                return "$";
            }
            if (!p.FormatArguments.ContainsKey(s)) {
                return "undefined";
            }
            return FormatArgument(p.FormatArguments[s]);
        });
    }

    string FormatArgument(object arg) {
        if (arg is TokenData) {
            var td = (TokenData) arg;
            if (td.Type == Token.Operator) {
                return FormatArgument(td.Operator);
            }
            if (td.Type == Token.CompoundAssignment) {
                return FormatArgument(td.Operator) + "=";
            }
            if (td.Type == Token.Keyword) {
                return "'" + td.StringValue + "'";
            }
            return FormatArgument(td.Type);
        }
        if (arg is AnnotatableDefinitionModifier) {
            return ((AnnotatableDefinitionModifier) arg).Name();
        }
        if (arg is AnnotatableDefinitionAccessModifier) {
            return ((AnnotatableDefinitionAccessModifier) arg).Name();
        }
        if (arg is Token) {
            return DefaultDiagnosticFormatterStatics.TokenTypesAsArguments.ContainsKey((Token) arg) ? DefaultDiagnosticFormatterStatics.TokenTypesAsArguments[(Token) arg] : "undefined";
        }
        return arg == null ? "null" : arg.ToString();
    }

    string ResolveId(int id) {
        return DefaultDiagnosticFormatterStatics.Messages.ContainsKey(id) ? DefaultDiagnosticFormatterStatics.Messages[id] : "undefined";
    }
}

public static class DefaultDiagnosticFormatterStatics {
    public static IDiagnosticFormatter Formatter = new DefaultDiagnosticFormatter();
    public class DefaultDiagnosticFormatter : IDiagnosticFormatter {
    }
    public static readonly Dictionary<int, string> Messages = new Dictionary<int, string> {
        [0] = "Unexpected $t",
        [1] = "Invalid or unexpected token",
        [2] = "Keyword must not contain escaped characters",
        [3] = "String must not contain line breaks",
        [4] = "Required parameter must not follow optional parameter",
        [5] = "Invalid destructuring assignment target",
        [6] = "\'await\' must not appear in generator",
        [7] = "\'yield\' must not appear in asynchronous function",
        [8] = "\'throws\' clause must not repeat",
        [9] = "Undefined label \'$label\'",
        [10] = "Try statement must have at least one \'catch\' or \'finally\' clause",
        [11] = "Mal-formed for..in binding",
        [12] = "Unexpected super statement",
        [13] = "Token must be inline",
        [14] = "Enum must not define instance variables",
        [15] = "",
        [16] = "Definition appears in unallowed context",
        [17] = "\'yield\' operator unexpected here",
        [18] = "Definition must not have modifier \'$m\'",
        [19] = "Native function must not have body",
        [20] = "Function must have body",
        [21] = "Interface method must have no attributes",
        [22] = "Proxy method has wrong number of parameters",
        [23] = "Unrecognized proxy \'$p\'",
        [24] = "\'extends\' clause must not duplicate",
        [25] = "Failed to resolve include source",
        [26] = "Function must not use \'await\'",
        [27] = "Function must not use \'yield\'",
        [28] = "Invalid arrow function parameter",
        [29] = "Rest parameter must not have initializer",
        [30] = "Value class must not have \'extends\' clause",
        [31] = "",
        [32] = "Illegal break statement",
        [33] = "Illegal continue statement: no surrounding iteration statement",
        [34] = "Getter must not have parameters",
        [35] = "Setter must have exactly one required parameter",
        [36] = "Package is not allowed here",
        [37] = "Return not allowed on top-level",
        [38] = "Await not allowed on top-level",
        [39] = "The source '$path' has already been included before",

        // # Verification Diagnostics

        // undefined property
        [128] = "'$name' not found",

        [129] = "Ambiguous reference to '$name'",
        [130] = "'$name' is private",
        [131] = "'$name' is not a type constant",
        [132] = "'$name' is a generic type or function, therefore must be argumented",
        [133] = "'$t' is not a generic type",
        [134] = "Expression is not a type constant",
        [135] = "Wrong number of arguments: expected $expectedN, got $gotN",
        [136] = "Argument type must be a subtype of $t",
        [137] = "Typed type expression must not be used here",
        [138] = "Variable must have type annotation",
        [139] = "Duplicate definition for '$name'",
        [140] = "Inferred type and type annotation must match: $i is not equals to $a",
        [141] = "Cannot apply array destructuring to type '$t'",
        [142] = "Tuple destructuring pattern cannot have more than $limit elements",
        [143] = "Tuple destructuring pattern must not have a rest element",
        [144] = "Rest element must be last element",
        [145] = "Record destructuring key here must be an identifier",
        [146] = "'$name' is a generic function, therefore must be argumented",
        [147] = "Assigning read-only property '$name'",
        [148] = "Destructuring assignment target must be lexical",
        [149] = "Target reference must be of type '$e'",
        [150] = "Expression not supported as a compile-time expression",
        [151] = "'$name' is not a compile-time constant",
        [152] = "Identifier must not be typed here",
        [153] = "Operation not supported as a compile-time expression: $op",
        [154] = "Operand must be numeric",
        [155] = "Compile-time \"in\" operation is only supported for flags",
        [156] = "Left operand must not be undefined or null",
        [157] = "A compile-time binary expression must have constant operand values",
        [158] = "A compile-time binary expression must have non-mixed operand values",
        [159] = "Type '$t' has no default value",
        [160] = "No inferred type for object or array initializer",
        [161] = "Compile-time object or array initializer can only be used with flags type",
        [162] = "Spread cannot appear in compile-time object or array initializer",
        [163] = "Compile-time object field key must be an identifier or string literal",
        [164] = "'$et' has no variant named '$name'",
        [165] = "Shorthand field not allowed at compile-time object initializer",
        [166] = "Compile-time array element must be a string literal",
        [167] = "Compile-time constant must be a value",
        [168] = "Incompatible types: expected '$expected', got '$got'",
        [169] = "Optional member base must be a value",
        [170] = "Type '$t' does not include null nor undefined",
        [171] = "Cannot embed resource as type '$t'",
        [172] = "Embed expression has no type to infer from",
        [173] = "Value must be a Promise",
        [174] = "Reference is write-only",
        [175] = "Reference is read-only",
        [176] = "Delete operand must be a brackets operation",
        [177] = "Delete operator not supported on object of type '$t'",
        [178] = "Type '$t' does not support operator '$op'",
        [179] = "Type '$t' is not a numeric type",
        [180] = "Expression must produce a value",
        [181] = "Left-hand side is never of type '$right'",
        [182] = "Left-hand side is always of type *",
        [183] = "Left-hand side is already of type '$right'",
        [184] = "",
        [185] = "Rest parameter must be of Array type",
        [186] = "Object or array initializer inferred no type",
        [187] = "Object initializer cannot be used for '$t'",
        [188] = "Object field for record or user type must be an identifier or string literal",
        [189] = "Object initializer must initialize the field '$name'",
        [190] = "Array initializer cannot be used for '$t'",
        [191] = "Wrong number of tuple elements: expected $expected, got $got",
        [192] = "Spread expression not currently supported in tuple initializer",
        [193] = "Cannot use markup initializer for type '$t'",
        [194] = "Markup cannot have children since type '$t' does not implement 'IMarkupContainer'",
        [195] = "Field must be a variable",
        [196] = "Property must be either variable or virtual",
        [197] = "Markup attribute without value requires a Boolean property; '$name' is of type '$t'",

        // undefined property on base type
        [198] = "'$name' not found on base type '$t'",

        [199] = "Could not resolve expression static type",
        [200] = "Markup list initializer must apply to an Array type",
        [201] = "Type '$t' does not support indexing",
        [202] = "Expected at least $atLeast argument(s)",
        [203] = "Expected at most $atMost argument(s)",
        [204] = "Call over enumeration takes exactly one argument",
        [205] = "Cannot convert from type '$from' to '$to'",
        [206] = "Cannot call expression",
        [207] = "Cannot call value of type '$t'",
        [208] = "Cannot use 'this' literal at this context",
        [209] = "Conditional expression types are incompatible: '$c' and '$a'",
        [210] = "$item is not generic",
        [211] = "Cannot construct type '$t'",
        [212] = "Cannot use 'new' operator with this item",
        [213] = "Cannot use 'super' expression here",
        [214] = "Package '$p' has no property '$name'",
        [215] = "Cannot import from non-package",
        [216] = "Import item must be a package",
        [217] = "Import item must not be a package",
        [218] = "Return must not be empty",
        [219] = "Cannot iterate keys on type '$t'",
        [220] = "Cannot iterate values on type '$t'",
        [221] = "Switch discriminant is never of type '$t'",
        [222] = "Item is not a namespace",
        [223] = "Not all code paths return a value",
        [224] = "Invalid generic bound type: '$t'",
        [225] = "Item must be a type parameter",
        [226] = "Generic function not allowed here",
        [227] = "Enum variant number must be one or power of 2",
        [228] = "Another enum variant has the same number",
        [229] = "Another enum variant has the same string",
        [230] = "$t is not an interface type",
        [231] = "Type must not inherit itself",
        [232] = "$t is not a class type",
        [233] = "Cannot inherit $t as it is marked final",
        [234] = "The class $c already implements the interface $i",
        [235] = "Missing method '$name' for implemented interface $itrfc",
        [236] = "Missing getter '$name' for implemented interface $itrfc",
        [237] = "Missing setter '$name' for implemented interface $itrfc",
        [238] = "Implemented interface $itrfc requires '$name' as a method",
        [239] = "Implemented interface $itrfc requires '$name' as a virtual property",
        [240] = "Implemented interface $itrfc requires '$name' as a method of signature '$s'",
        [241] = "Implemented interface $itrfc requires '$name' as a getter of signature '$s'",
        [242] = "Implemented interface $itrfc requires '$name' as a setter of signature '$s'",
        [243] = "Did not match generic method '$name' of interface $itrfc",
        [244] = "Property must have an initializer",
        [245] = "Property annotated type does not match actual property type",
        [246] = "Shadowing already defined property '$name' in super type",
        [247] = "Accessing tuple requires a constant numeric literal key",
        [248] = "Accessing tuple index out of bounds: $type",

        // warning
        [249] = "Variable has no type annotation",

        // warning
        [250] = "Function has no return type annotation",

        [251] = "'$name' must override a method",
        [252] = "Cannot override method '$name' as it's generic",
        [253] = "Incompatible override: expected signature $type",
        [254] = "Cannot override method '$name' as it's marked final",
        [255] = "Variables must be read-only under a value class",
        [256] = "Duplicate constructor",
        [257] = "Constructor must call super",
        [258] = "Duplicate proxy",
        [259] = "Illegal proxy signature",
        [260] = "'setIndex' proxy's first parameter must be of type $type",
        [261] = "'deleteIndex' proxy requires a matching 'getIndex' proxy",
        [262] = "Wrong virtual property visibility",
        [263] = "Setter must return void",
        [264] = "Setter does not have the same type as the getter",
        [265] = "Shadowing definition '$name'; if this is intended, use 'use shadowing'",

        // undefined property on base package
        [266] = "'$name' not found on base package $base",

        // undefined property on base namespace
        [267] = "'$name' not found on base namespace $base",

        [268] = "Item is not generic",
        [269] = "Non-null destructuring from a base of type $type",
    };

    public static readonly Dictionary<Token, string> TokenTypesAsArguments = new Dictionary<Token, string> {
        [Token.Eof] = "end of program",
        [Token.Identifier] = "identifier",
        [Token.StringLiteral] = "string literal",
        [Token.NumericLiteral] = "numeric literal",
        [Token.RegExpLiteral] = "regular expression",
        [Token.Keyword] = "keyword",
        [Token.Operator] = "operator",
        [Token.CompoundAssignment] = "assignment",
        [Token.LCurly] = "{",
        [Token.RCurly] = "}",
        [Token.LParen] = "(",
        [Token.RParen] = ")",
        [Token.LSquare] = "[",
        [Token.RSquare] = "]",
        [Token.Dot] = ".",
        [Token.QuestionDot] = "?.",
        [Token.Ellipsis] = "...",
        [Token.Semicolon] = ";",
        [Token.Comma] = ",",
        [Token.QuestionMark] = "?",
        [Token.ExclamationMark] = "!",
        [Token.Colon] = ":",
        [Token.Assign] = "=",
        [Token.FatArrow] = "=>",
        [Token.LtSlash] = "</",
    };
}