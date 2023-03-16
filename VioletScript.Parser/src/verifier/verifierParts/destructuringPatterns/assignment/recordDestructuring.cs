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
            annotatedType = VerifyTypeExp(pattern.Type) ?? m_ModelCore.AnyType;
            // inferred type and type annotation must be the same
            if (annotatedType != type)
            {
                VerifyError(null, 140, pattern.Span.Value, new DiagnosticArguments { ["i"] = type, ["a"] = annotatedType });
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
                    field.SemanticFrameAssignedReference = AssignmentRecordDestructuringLexicalRef(key.Value, field.Span.Value);
                    // assigning any to a property of any type
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
                    field.SemanticFrameAssignedReference = AssignmentRecordDestructuringLexicalRef(key.Value, field.Span.Value);
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

    private void VerifyAssignmentRecordDestructuringPatternForCompileTime(Ast.RecordDestructuringPattern pattern, Symbol type)
    {
        foreach (var field in pattern.Fields)
        {
            if (field.Key is Ast.StringLiteral key)
            {
                var throwawayProp = m_ModelCore.Factory.Value(type).ResolveProperty(key.Value);
                Symbol fieldType = null;
                if (throwawayProp == null)
                {
                    // VerifyError: undefined reference
                    VerifyError(key.Span.Value.Script, 128, key.Span.Value, new DiagnosticArguments { ["name"] = key.Value });
                    fieldType = m_ModelCore.AnyType;
                }
                else
                {
                    // we're assuming 'throwaway' is neither a namespace, a package nor a type.
                    if (throwawayProp is Namespace || throwawayProp is Type)
                    {
                        throw new Exception("Unimplemented handling of namespace/type fields on record destructuring");
                    }
                    if (!throwawayProp.PropertyIsVisibleTo(m_Frame))
                    {
                        // VerifyError: accessing private property
                        VerifyError(key.Span.Value.Script, 130, key.Span.Value, new DiagnosticArguments { ["name"] = key.Value });
                    }
                    throwawayProp = throwawayProp is Alias ? throwawayProp.AliasToSymbol : throwawayProp;
                    // VerifyError: unargumented function
                    if (throwawayProp.TypeParameters != null)
                    {
                        VerifyError(key.Span.Value.Script, 146, key.Span.Value, new DiagnosticArguments { ["name"] = key.Value });
                    }
                    fieldType = throwawayProp.StaticType;
                }

                field.SemanticFrameAssignedReference = AssignmentRecordDestructuringLexicalRef(key.Value, field.Span.Value);
                // ensure the lexical reference has the correct type
                if (field.SemanticFrameAssignedReference != null && field.SemanticFrameAssignedReference.StaticType != fieldType)
                {
                    VerifyError(null, 149, field.Key.Span.Value, new DiagnosticArguments { ["e"] = fieldType });
                }
            }
            // key must be an identifier
            else
            {
                VerifyError(null, 145, field.Key.Span.Value, new DiagnosticArguments {});
                VerifyExp(field.Key);
                if (field.Subpattern != null)
                {
                    VerifyAssignmentDestructuringPattern(field.Subpattern, m_ModelCore.AnyType);
                }
            }
        }
    }
}