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
    // verify an array destructuring pattern; ensure:
    // - if there is both a type annotation and an inferred type, ensure they are equals.
    // - if there is no type annotation and no inferred type, throw a VerifyError.
    // - only one rest element is allowed and must be at the end of the pattern.
    private void VerifyRecordDestructuringPattern
    (
        Ast.RecordDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol inferredType = null
    )
    {
        Symbol type = null;
        if (pattern.Type != null)
        {
            type = VerifyTypeExp(pattern.Type) ?? m_ModelCore.AnyType;
        }
        if (type == null)
        {
            type = inferredType;
            if (type == null)
            {
                VerifyError(pattern.Span.Value.Script, 138, pattern.Span.Value, new DiagnosticArguments { ["name"] = pattern.Name });
                type = m_ModelCore.AnyType;
            }
        }
        // inferred type and type annotation must be the same
        else if (inferredType != null && inferredType != type)
        {
            VerifyError(pattern.Span.Value.Script, 140, pattern.Span.Value, new DiagnosticArguments { ["i"] = inferredType, ["a"] = type });
        }
        type ??= m_ModelCore.AnyType;

        pattern.SemanticProperty = m_ModelCore.Factory.VariableSlot("", readOnly, type);

        if (type == m_ModelCore.AnyType)
        {
            VerifyRecordDestructuringPatternForAny(pattern, readOnly, output, visibility);
        }
        else if (type.IsInstantiationOf(m_ModelCore.MapType))
        {
            VerifyRecordDestructuringPatternForMap(pattern, readOnly, output, visibility, type);
        }
        else
        {
            VerifyRecordDestructuringPatternForCompileTime(pattern, readOnly, output, visibility, type);
        }
    }

    private void VerifyRecordDestructuringPatternForAny
    (
        Ast.RecordDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility
    )
    {
        foreach (var field in pattern.Fields)
        {
            if (field.Key is Ast.StringLiteral key)
            {
                if (field.Subpattern == null)
                {
                    Symbol newDefinition = null;
                    var previousDefinition = output[key.Value];
                    if (previousDefinition != null)
                    {
                        // VerifyError: duplicate definition
                        newDefinition = previousDefinition is VariableSlot ? previousDefinition : null;
                        if (!m_Options.AllowDuplicates)
                        {
                            VerifyError(key.Span.Value.Script, 139, key.Span.Value, new DiagnosticArguments { ["name"] = key.Value });
                        }
                    }
                    else
                    {
                        newDefinition = m_ModelCore.Factory.VariableSlot(key.Value, readOnly, m_ModelCore.AnyType);
                        newDefinition.Visibility = visibility;
                        output[pattern.Name] = newDefinition;
                    }
                    field.SemanticProperty = newDefinition;
                }
                else
                {
                    VerifyDestructuringPattern(field.Subpattern, readOnly, output, visibility, m_ModelCore.AnyType);
                }
            }
            else
            {
                VerifyExp(field.Key);
                if (field.Subpattern != null)
                {
                    VerifyDestructuringPattern(field.Subpattern, readOnly, output, visibility, m_ModelCore.AnyType);
                }
                else
                {
                    // VerifyError: key is not an identifier
                    VerifyError(field.Key.Span.Value.Script, 145, field.Key.Span.Value, new DiagnosticArguments {});
                }
            }
        }
    }

    private void VerifyRecordDestructuringPatternForMap
    (
        Ast.RecordDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol mapType
    )
    {
        Symbol keyType = mapType.ArgumentTypes[0];
        Symbol valueType = mapType.ArgumentTypes[1];
        // accessing keys should produce `v:undefined|V`
        Symbol undefinedOrValueType = m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.UndefinedType, valueType});

        foreach (var field in pattern.Fields)
        {
            if (field.Key is Ast.StringLiteral key)
            {
                if (field.Subpattern == null)
                {
                    Symbol newDefinition = null;
                    var previousDefinition = output[key.Value];
                    if (previousDefinition != null)
                    {
                        // VerifyError: duplicate definition
                        newDefinition = previousDefinition is VariableSlot ? previousDefinition : null;
                        if (!m_Options.AllowDuplicates)
                        {
                            VerifyError(key.Span.Value.Script, 139, key.Span.Value, new DiagnosticArguments { ["name"] = key.Value });
                        }
                    }
                    else
                    {
                        newDefinition = m_ModelCore.Factory.VariableSlot(key.Value, readOnly, undefinedOrValueType);
                        newDefinition.Visibility = visibility;
                        output[pattern.Name] = newDefinition;
                    }
                    field.SemanticProperty = newDefinition;
                }
                else
                {
                    VerifyDestructuringPattern(field.Subpattern, readOnly, output, visibility, undefinedOrValueType);
                }
            }
            else
            {
                LimitExpType(field.Key, keyType);
                if (field.Subpattern != null)
                {
                    VerifyDestructuringPattern(field.Subpattern, readOnly, output, visibility, undefinedOrValueType);
                }
                else
                {
                    // VerifyError: key is not an identifier
                    VerifyError(field.Key.Span.Value.Script, 145, field.Key.Span.Value, new DiagnosticArguments {});
                }
            }
        }
    }

    private void VerifyRecordDestructuringPatternForCompileTime
    (
        Ast.RecordDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol type
    )
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
                    throwawayProp = throwawayProp?.EscapeAlias();
                    // VerifyError: unargumented function
                    if (throwawayProp.TypeParameters != null)
                    {
                        VerifyError(key.Span.Value.Script, 146, key.Span.Value, new DiagnosticArguments { ["name"] = key.Value });
                    }
                    fieldType = throwawayProp.StaticType;
                }

                if (field.Subpattern == null)
                {
                    Symbol newDefinition = null;
                    var previousDefinition = output[key.Value];
                    if (previousDefinition != null)
                    {
                        // VerifyError: duplicate definition
                        newDefinition = previousDefinition is VariableSlot ? previousDefinition : null;
                        if (!m_Options.AllowDuplicates)
                        {
                            VerifyError(key.Span.Value.Script, 139, key.Span.Value, new DiagnosticArguments { ["name"] = key.Value });
                        }
                    }
                    else
                    {
                        newDefinition = m_ModelCore.Factory.VariableSlot(key.Value, readOnly, fieldType);
                        newDefinition.Visibility = visibility;
                        output[pattern.Name] = newDefinition;
                    }
                    field.SemanticProperty = newDefinition;
                }
                else
                {
                    VerifyDestructuringPattern(field.Subpattern, readOnly, output, visibility, fieldType);
                }
            }
            // VerifyError: key is not an identifier
            else
            {
                VerifyExp(field.Key);
                VerifyError(field.Key.Span.Value.Script, 145, field.Key.Span.Value, new DiagnosticArguments {});
                if (field.Subpattern != null)
                {
                    VerifyDestructuringPattern(field.Subpattern, readOnly, output, visibility, m_ModelCore.AnyType);
                }
            }
        }
    }
}