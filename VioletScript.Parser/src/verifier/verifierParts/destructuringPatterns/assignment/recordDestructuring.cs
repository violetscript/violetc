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
    // verify a destructuring pattern for assignment expressions.
    private void VerifyAssignmentRecordDestructuringPattern(Ast.RecordDestructuringPattern pattern, Symbol type)
    {
        Symbol annotatedType = null;
        if (pattern.Type != null)
        {
            annotatedType = VerifyTypeExp(pattern.Type);
            // inferred type and type annotation must be the same
            if (annotatedType != type)
            {
                VerifyError(pattern.Span.Value.Script, 140, pattern.Span.Value, new DiagnosticArguments { ["i"] = type, ["a"] = annotatedType });
            }
        }
        if (type == m_ModelCore.AnyType)
        {
            VerifyAssignmentRecordDestructuringPatternForAny(pattern, type);
        }
        else if (type.IsInstantiationOf(m_ModelCore.MapType))
        {
            VerifyAssignmentRecordDestructuringPatternForMap(pattern, type);
        }
        else
        {
            VerifyAssignmentRecordDestructuringPatternForCompileTime(pattern, type);
        }
    }

    private void VerifyAssignmentRecordDestructuringPatternForAny(Ast.RecordDestructuringPattern pattern, Symbol type)
    {
        foreach (var field in pattern.Fields)
        {
            if (field.Subpattern == null)
            {
                // ...
            }
            else
            {
                VerifyExp(field.Key);
                VerifyAssignmentDestructuringPattern(field.Subpattern, m_ModelCore.AnyType);
            }
        }
    }
}