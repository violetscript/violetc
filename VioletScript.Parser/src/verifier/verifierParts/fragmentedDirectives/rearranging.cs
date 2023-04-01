namespace VioletScript.Parser.Verifier;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Diagnostic;
using VioletScript.Parser.Semantic.Logic;
using VioletScript.Parser.Semantic.Model;
using VioletScript.Parser.Source;
using Ast = VioletScript.Parser.Ast;

using DiagnosticArguments = Dictionary<string, object>;

public partial class Verifier
{
    // gather directives to which other directives depend
    // in contexts such as in package frame or top-level program frame.
    // the following kinds of directives are manipulated here:
    // - `import`
    // - `use namespace`
    // - `namespace` alias definition
    // - `type` definition
    //
    // after gathering to which other directives each of these directives
    // depend, build a tree which can be fully verified
    // in ascending order.
}