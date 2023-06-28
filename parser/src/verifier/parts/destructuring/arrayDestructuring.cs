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
    private void VerifyArrayDestructuringPattern
    (
        Ast.ArrayDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol inferredType = null,
        bool canShadow = false
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
        pattern.SemanticProperty.InitValue ??= type.DefaultValue;

        if (type is TupleType)
        {
            VerifyArrayDestructuringPatternForTuple(pattern, readOnly, output, visibility, type, canShadow);
        }
        else if (type.IsInstantiationOf(m_ModelCore.ArrayType))
        {
            VerifyArrayDestructuringPatternForArray(pattern, readOnly, output, visibility, type, canShadow);
        }
        else
        {
            var proxy = type.Delegate != null && type.Delegate.Proxies.ContainsKey(Operator.ProxyToGetIndex) ? type.Delegate.Proxies[Operator.ProxyToGetIndex] : null;
            if (proxy != null && this.m_ModelCore.IsNumericType(proxy.StaticType.FunctionRequiredParameters[0].Type))
            {
                VerifyArrayDestructuringPatternForProxy(pattern, readOnly, output, visibility, proxy, canShadow);
            }
            else
            {
                if (type != m_ModelCore.AnyType)
                {
                    VerifyError(pattern.Span.Value.Script, 141, pattern.Span.Value, new DiagnosticArguments { ["t"] = type });
                }
                VerifyArrayDestructuringPatternForAny(pattern, readOnly, output, visibility, canShadow);
            }
        }
    }

    private void VerifyArrayDestructuringPatternForTuple
    (
        Ast.ArrayDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol tupleType,
        bool canShadow = false
    )
    {
        if (pattern.Items.Count() > tupleType.TupleElementTypes.Count())
        {
            VerifyError(pattern.Span.Value.Script, 142, pattern.Span.Value, new DiagnosticArguments { ["limit"] = tupleType.TupleElementTypes.Count() });
        }
        for (int i = 0; i < pattern.Items.Count(); ++i)
        {
            var item = pattern.Items[i];
            if (item == null)
            {
                // ellision
            }
            else if (item is Ast.ArrayDestructuringSpread spread)
            {
                VerifyError(spread.Span.Value.Script, 143, spread.Span.Value, new DiagnosticArguments {});
                VerifyDestructuringPattern(spread.Pattern, readOnly, output, visibility, m_ModelCore.AnyType);
            }
            else
            {
                var tupleItemType = i < tupleType.TupleElementTypes.Count() ? tupleType.TupleElementTypes[i] : null;
                VerifyDestructuringPattern((Ast.DestructuringPattern) item, readOnly, output, visibility, tupleItemType ?? m_ModelCore.AnyType);
            }
        }
    }

    private void VerifyArrayDestructuringPatternForArray
    (
        Ast.ArrayDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol arrayType,
        bool canShadow = false
    )
    {
        var arrayElementType = this.m_ModelCore.Factory.UnionType(new Symbol[]{this.m_ModelCore.UndefinedType, arrayType.ArgumentTypes[0]});
        for (int i = 0; i < pattern.Items.Count(); ++i)
        {
            var item = pattern.Items[i];
            if (item == null)
            {
                // ellision
            }
            else if (item is Ast.ArrayDestructuringSpread spread)
            {
                // a rest element must be the last element
                if (i != pattern.Items.Count() -1)
                {
                    VerifyError(spread.Span.Value.Script, 144, spread.Span.Value, new DiagnosticArguments {});
                }
                VerifyDestructuringPattern(spread.Pattern, readOnly, output, visibility, arrayType);
            }
            else
            {
                VerifyDestructuringPattern((Ast.DestructuringPattern) item, readOnly, output, visibility, arrayElementType);
            }
        }
    }

    private void VerifyArrayDestructuringPatternForProxy
    (
        Ast.ArrayDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol proxy,
        bool canShadow = false
    )
    {
        var arrayElementType = proxy.StaticType.FunctionReturnType;
        for (int i = 0; i < pattern.Items.Count(); ++i)
        {
            var item = pattern.Items[i];
            if (item == null)
            {
                // ellision
            }
            else if (item is Ast.ArrayDestructuringSpread spread)
            {
                // a rest element must be the last element
                if (i != pattern.Items.Count() -1)
                {
                    VerifyError(spread.Span.Value.Script, 144, spread.Span.Value, new DiagnosticArguments {});
                }
                // for a proxy destructuring, the rest will be a [T] type.
                VerifyDestructuringPattern(spread.Pattern, readOnly, output, visibility, this.m_ModelCore.Factory.TypeWithArguments(this.m_ModelCore.ArrayType, new Symbol[]{arrayElementType}));
            }
            else
            {
                VerifyDestructuringPattern((Ast.DestructuringPattern) item, readOnly, output, visibility, arrayElementType);
            }
        }
    }

    private void VerifyArrayDestructuringPatternForAny
    (
        Ast.ArrayDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        bool canShadow = false
    )
    {
        var anyType = m_ModelCore.AnyType;
        for (int i = 0; i < pattern.Items.Count(); ++i)
        {
            var item = pattern.Items[i];
            if (item == null)
            {
                // ellision
            }
            else if (item is Ast.ArrayDestructuringSpread spread)
            {
                // a rest element must be the last element
                if (i != pattern.Items.Count() - 1)
                {
                    VerifyError(spread.Span.Value.Script, 144, spread.Span.Value, new DiagnosticArguments {});
                }
                VerifyDestructuringPattern(spread.Pattern, readOnly, output, visibility, anyType, canShadow);
            }
            else
            {
                VerifyDestructuringPattern((Ast.DestructuringPattern) item, readOnly, output, visibility, anyType, canShadow);
            }
        }
    }
}