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
                if (field.Key is Ast.StringLiteral key)
                {
                    field.SemanticFrameAssignedReference = AssignmentRecordDestructuringLexicalRef(key.Name, field.Span.Value);
                    // assigning any to a property of any type
                }
                // key must be an identifier
                else
                {
                    VerifyError(field.Key.Span.Value.Script, 145, field.Key.Span.Value, new DiagnosticArguments {});
                    VerifyExp(field.Key);
                }
            }
            else
            {
                VerifyExp(field.Key);
                VerifyAssignmentDestructuringPattern(field.Subpattern, m_ModelCore.AnyType);
            }
        }
    }

    private void VerifyAssignmentRecordDestructuringPatternForMap(Ast.RecordDestructuringPattern pattern, Symbol mapType)
    {
        Symbol keyType = mapType.ArgumentTypes[0];
        Symbol valueType = mapType.ArgumentTypes[1];
        // accessing keys should produce `v:undefined|V`
        Symbol undefinedOrValueType = m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.UndefinedType, valueType});

        foreach (var field in pattern.Fields)
        {
            if (field.Subpattern == null)
            {
                if (field.Key is Ast.StringLiteral key)
                {
                    field.SemanticFrameAssignedReference = AssignmentRecordDestructuringLexicalRef(key.Name, field.Span.Value);
                    // ensure the lexical reference has the correct type
                    if (field.SemanticFrameAssignedReference != null && field.SemanticFrameAssignedReference.StaticType != undefinedOrValueType)
                    {
                        VerifyError(null, 149, field.Key.Span.Value, new DiagnosticArguments { ["e"] = undefinedOrValueType });
                    }
                }
                // key must be an identifier
                else
                {
                    VerifyError(null, 145, field.Key.Span.Value, new DiagnosticArguments {});
                    VerifyExp(field.Key);
                }
            }
            else
            {
                LimitExpType(field.Key, keyType);
                VerifyAssignmentDestructuringPattern(field.Subpattern, undefinedOrValueType);
            }
        }
    }
}