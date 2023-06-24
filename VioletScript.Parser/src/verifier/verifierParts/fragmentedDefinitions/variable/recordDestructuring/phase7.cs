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
    private void Fragmented_VerifyRecordDestructuringPattern7(Ast.RecordDestructuringPattern pattern, Symbol initType)
    {
        pattern.SemanticProperty.StaticType ??= initType ?? this.m_ModelCore.AnyType;
        pattern.SemanticProperty.InitValue ??= pattern.SemanticProperty.StaticType?.DefaultValue;

        var type = pattern.SemanticProperty.StaticType;

        if (type == m_ModelCore.AnyType)
        {
            Fragmented_VerifyRecordDestructuringPattern7ForAny(pattern);
        }
        else if (type.IsInstantiationOf(m_ModelCore.MapType))
        {
            Fragmented_VerifyRecordDestructuringPattern7ForMap(pattern, type);
        }
        else
        {
            Fragmented_VerifyRecordDestructuringPattern7ForCompileTime(pattern, type);
        }
    }

    private void Fragmented_VerifyRecordDestructuringPattern7ForAny(Ast.RecordDestructuringPattern pattern)
    {
        foreach (var field in pattern.Fields)
        {
            if (field.Key is Ast.StringLiteral key)
            {
                if (field.Subpattern == null)
                {
                    field.SemanticProperty.StaticType ??= this.m_ModelCore.AnyType;
                }
                else
                {
                    this.Fragmented_VerifyDestructuringPattern7(field.Subpattern, this.m_ModelCore.AnyType);
                }
            }
            else
            {
                this.VerifyExp(field.Key);
                if (field.Subpattern != null)
                {
                    this.Fragmented_VerifyDestructuringPattern7(field.Subpattern, m_ModelCore.AnyType);
                }
            }
        }
    }

    private void Fragmented_VerifyRecordDestructuringPattern7ForMap(Ast.RecordDestructuringPattern pattern, Symbol mapType)
    {
        Symbol keyType = mapType.ArgumentTypes[0];
        Symbol valueType = mapType.ArgumentTypes[1];
        // accessing keys should produce `v: undefined | V`
        Symbol undefinedOrValueType = m_ModelCore.Factory.UnionType(new Symbol[]{m_ModelCore.UndefinedType, valueType});

        foreach (var field in pattern.Fields)
        {
            if (field.Key is Ast.StringLiteral key)
            {
                if (field.Subpattern == null)
                {
                    field.SemanticProperty.StaticType ??= undefinedOrValueType;
                }
                else
                {
                    this.Fragmented_VerifyDestructuringPattern7(field.Subpattern, undefinedOrValueType);
                }
            }
            else
            {
                this.LimitExpType(field.Key, keyType);
                if (field.Subpattern != null)
                {
                    this.Fragmented_VerifyDestructuringPattern7(field.Subpattern, undefinedOrValueType);
                }
            }
        }
    }

    private void Fragmented_VerifyRecordDestructuringPattern7ForCompileTime(Ast.RecordDestructuringPattern pattern, Symbol type)
    {
        foreach (var field in pattern.Fields)
        {
            if (field.Key is Ast.StringLiteral key)
            {
                var fieldType = this.VerifyRecordDestructuringCompileTimeFieldType(field, key, type);

                if (field.Subpattern == null)
                {
                    field.SemanticProperty.StaticType ??= fieldType;
                }
                else
                {
                    this.Fragmented_VerifyDestructuringPattern7(field.Subpattern, fieldType);
                }
            }
            // ERROR: key is not an identifier
            else
            {
                this.VerifyError(field.Key.Span.Value.Script, 145, field.Key.Span.Value, new DiagnosticArguments {});
                this.VerifyExp(field.Key);

                if (field.Subpattern != null)
                {
                    this.Fragmented_VerifyDestructuringPattern7(field.Subpattern, this.m_ModelCore.AnyType);
                }
            }
        }
    }
}