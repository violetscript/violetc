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
    // verifies object initializer.
    // if it is given the type '*' or no type,
    // then it returns an Object.
    private Symbol VerifyObjectInitialiser(Ast.ObjectInitializer exp, Symbol expectedType)
    {
        Symbol type = null;
        if (exp.Type != null)
        {
            type = VerifyTypeExp(exp.Type) ?? m_ModelCore.AnyType;
        }
        if (type == null)
        {
            type = expectedType;
        }
        type ??= expectedType;
        type = type?.ToNonNullableType();

        // make sure 'type' can be initialised
        if (type is UnionType)
        {
            type = type.UnionMemberTypes.Where(t => t.TypeCanUseObjectInitializer).FirstOrDefault();
        }

        if (type == null)
        {
            // VerifyError: no infer type
            VerifyError(null, 186, exp.Span.Value, new DiagnosticArguments {});
            type = m_ModelCore.AnyType;
        }
        else if (!type.TypeCanUseObjectInitializer)
        {
            // VerifyError: cannot initialise type
            VerifyError(null, 187, exp.Span.Value, new DiagnosticArguments {["t"] = type});
            type = m_ModelCore.AnyType;
        }

        // try different initialization methods:
        // - * (any type)
        // - Map
        // - Flags
        // - Record
        // - Class
        if (type == m_ModelCore.AnyType)
        {
            Any_VerifyObjectInitialiser(exp);
        }
        else if (type.IsInstantiationOf(m_ModelCore.MapType))
        {
            Map_VerifyObjectInitialiser(exp, type);
        }
        else if (type.IsFlagsEnum)
        {
            Flags_VerifyObjectInitialiser(exp, type);
        }
        else if (type is RecordType)
        {
            Record_VerifyObjectInitialiser(exp, type);
        }
        else
        {
            User_VerifyObjectInitialiser(exp, type);
        }

        exp.SemanticSymbol = m_ModelCore.Factory.Value(type);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // object initializer

    private void Any_VerifyObjectInitialiser(Ast.ObjectInitializer exp)
    {
        foreach (var fieldOrSpread in exp.Fields)
        {
            if (fieldOrSpread is Ast.Spread spread)
            {
                LimitExpType(spread.Expression, m_ModelCore.AnyType);
                continue;
            }
            var field = (Ast.ObjectField) fieldOrSpread;
            LimitExpType(field.Key, m_ModelCore.AnyType);
            if (field.Value == null)
            {
                VerifyObjectShorthandField(field, m_ModelCore.AnyType);
            }
            else
            {
                LimitExpType(field.Value, m_ModelCore.AnyType);
            }
        }
    } // object initializer (any)

    private void Map_VerifyObjectInitialiser(Ast.ObjectInitializer exp, Symbol type)
    {
        var keyType = type.ArgumentTypes[0];
        var valueType = type.ArgumentTypes[1];

        foreach (var fieldOrSpread in exp.Fields)
        {
            if (fieldOrSpread is Ast.Spread spread)
            {
                LimitExpType(spread.Expression, type);
                continue;
            }
            var field = (Ast.ObjectField) fieldOrSpread;
            LimitExpType(field.Key, keyType);
            if (field.Value == null)
            {
                VerifyObjectShorthandField(field, valueType);
            }
            else
            {
                LimitExpType(field.Value, valueType);
            }
        }
    } // object initializer (Map)

    private void Flags_VerifyObjectInitialiser(Ast.ObjectInitializer exp, Symbol type)
    {
        foreach (var fieldOrSpread in exp.Fields)
        {
            if (fieldOrSpread is Ast.Spread spread)
            {
                LimitExpType(spread.Expression, type);
                continue;
            }
            var field = (Ast.ObjectField) fieldOrSpread;
            LimitExpType(field.Key, type);
            if (field.Value == null)
            {
                VerifyObjectShorthandField(field, m_ModelCore.BooleanType);
            }
            else
            {
                LimitExpType(field.Value, m_ModelCore.BooleanType);
            }
        }
    } // object initializer (flags)

    private void Record_VerifyObjectInitialiser(Ast.ObjectInitializer exp, Symbol type)
    {
        var initializedFields = new Dictionary<string, bool>();

        foreach (var fieldOrSpread in exp.Fields)
        {
            if (fieldOrSpread is Ast.Spread spread)
            {
                LimitExpType(spread.Expression, type);
                continue;
            }
            var field = (Ast.ObjectField) fieldOrSpread;
            if (!(field.Key is Ast.StringLiteral))
            {
                VerifyError(null, 188, field.Key.Span.Value, new DiagnosticArguments {});
                if (field.Value != null)
                {
                    VerifyExp(field.Value);
                }
                continue;
            }

            var fieldName = ((Ast.StringLiteral) field.Key).Value;
            var matchingField = type.RecordTypeGetField(fieldName);

            if (!matchingField.HasValue)
            {
                // VerifyError: undefined property
                VerifyError(null, 198, field.Key.Span.Value, new DiagnosticArguments {["t"] = type, ["name"] = fieldName});
                if (field.Value != null)
                {
                    VerifyExp(field.Value);
                }
                continue;
            }

            initializedFields[fieldName] = true;

            if (field.Value == null)
            {
                VerifyObjectShorthandField(field, matchingField.Value.Type);
            }
            else
            {
                LimitExpType(field.Value, matchingField.Value.Type);
            }
        }

        // ensure that all required fields are specified.
        // a field is optional when it possibly contains undefined.
        foreach (var fieldDefinition in type.RecordTypeFields)
        {
            if (!fieldDefinition.Type.IncludesUndefined
            && !initializedFields.ContainsKey(fieldDefinition.Name))
            {
                VerifyError(null, 189, exp.Span.Value, new DiagnosticArguments {["name"] = fieldDefinition.Name});
            }
        }
    } // object initializer (record)

    private void User_VerifyObjectInitialiser(Ast.ObjectInitializer exp, Symbol type)
    {
        var anonymousValue = m_ModelCore.Factory.Value(type);
        foreach (var fieldOrSpread in exp.Fields)
        {
            if (fieldOrSpread is Ast.Spread spread)
            {
                LimitExpType(spread.Expression, type);
                continue;
            }
            var field = (Ast.ObjectField) fieldOrSpread;
            if (!(field.Key is Ast.StringLiteral))
            {
                VerifyError(null, 188, field.Key.Span.Value, new DiagnosticArguments {});
                if (field.Value != null)
                {
                    VerifyExp(field.Value);
                }
                continue;
            }

            var fieldName = ((Ast.StringLiteral) field.Key).Value;
            var matchingProp = UserObjectInitializer_VerifyKey(anonymousValue, type, fieldName, field.Key.Span.Value);
            if (matchingProp == null)
            {
                if (field.Value != null)
                {
                    VerifyExp(field.Value);
                }
                continue;
            }

            if (field.Value == null)
            {
                VerifyObjectShorthandField(field, matchingProp.StaticType);
            }
            else
            {
                LimitExpType(field.Value, matchingProp.StaticType);
            }
        }
    } // object initializer (user)

    private Symbol UserObjectInitializer_VerifyKey(Symbol anonymousValue, Symbol type, string fieldName, Span fieldSpan)
    {
        var r = anonymousValue.ResolveProperty(fieldName);
        if (r == null)
        {
            // VerifyError: undefined property
            VerifyError(null, 198, fieldSpan, new DiagnosticArguments {["t"] = type, ["name"] = fieldName});
            return null;
        }
        if (!r.PropertyIsVisibleTo(m_Frame))
        {
            // VerifyError: accessing private property
            VerifyError(null, 130, fieldSpan, new DiagnosticArguments { ["name"] = fieldName });
            return null;
        }
        r = r is Alias ? r.AliasToSymbol : r;
        // VerifyError: unargumented generic type or function
        if (r.IsGenericTypeOrMethod)
        {
            VerifyError(null, 132, fieldSpan, new DiagnosticArguments { ["name"] = fieldName });
            return null;
        }
        if (r is Type)
        {
            r = m_ModelCore.Factory.TypeAsValue(r);
        }
        else if (r is Namespace)
        {
            r = m_ModelCore.Factory.NamespaceAsValue(r);
        }
        if (!(r is Value))
        {
            VerifyError(null, 180, fieldSpan, new DiagnosticArguments {});
            return null;
        }
        if (!(r is ReferenceValue && r.Property is VariableSlot))
        {
            VerifyError(null, 195, fieldSpan, new DiagnosticArguments {});
            return null;
        }
        if (r is Value && r.StaticType == null)
        {
            VerifyError(null, 199, fieldSpan, new DiagnosticArguments {});
            return null;
        }
        return r;
    } // UserObjectInitializer_VerifyKey

    private void VerifyObjectShorthandField(Ast.ObjectField field, Symbol expectedType)
    {
        var name = ((Ast.StringLiteral) field.Key).Value;
        var r = m_Frame.ResolveProperty(name);
        if (r == null)
        {
            // VerifyError: undefined reference
            VerifyError(null, 128, field.Span.Value, new DiagnosticArguments { ["name"] = name });
            return;
        }
        else if (r is AmbiguousReferenceIssue)
        {
            // VerifyError: ambiguous reference
            VerifyError(null, 129, field.Span.Value, new DiagnosticArguments { ["name"] = name });
            return;
        }
        else
        {
            if (!r.PropertyIsVisibleTo(m_Frame))
            {
                // VerifyError: accessing private property
                VerifyError(null, 130, field.Span.Value, new DiagnosticArguments { ["name"] = name });
                return;
            }
            r = r is Alias ? r.AliasToSymbol : r;

            // VerifyError: unargumented generic type or function
            if (r.IsGenericTypeOrMethod)
            {
                VerifyError(null, 132, field.Span.Value, new DiagnosticArguments { ["name"] = name });
                return;
            }

            if (r is Type)
            {
                r = m_ModelCore.Factory.TypeAsValue(r);
            }
            else if (r is Namespace)
            {
                r = m_ModelCore.Factory.NamespaceAsValue(r);
            }
            if (!(r is Value))
            {
                VerifyError(null, 180, field.Span.Value, new DiagnosticArguments {});
                return;
            }

            if (r is Value && r.StaticType == null)
            {
                VerifyError(null, 199, field.Span.Value, new DiagnosticArguments {});
                return;
            }

            // extend variable life
            if (r is ReferenceValueFromFrame && r.Base.FindActivation() != m_Frame.FindActivation())
            {
                r.Base.FindActivation().AddExtendedLifeVariable(r.Property);
            }

            var conversion = TypeConversions.ConvertImplicit(r, expectedType);
            if (conversion == null)
            {
                VerifyError(null, 168, field.Span.Value, new DiagnosticArguments {["expected"] = expectedType, ["got"] = r.StaticType});
            }
            field.SemanticShorthand = conversion;
        }
    } // VerifyObjectShorthandField
}