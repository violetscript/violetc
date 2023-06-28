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
    private void Fragmented_VerifyArrayDestructuringPattern7(Ast.ArrayDestructuringPattern pattern, Symbol initType)
    {
        pattern.SemanticProperty.StaticType ??= initType ?? this.m_ModelCore.AnyType;
        pattern.SemanticProperty.InitValue ??= pattern.SemanticProperty.StaticType?.DefaultValue;

        var type = pattern.SemanticProperty.StaticType;

        if (type is TupleType)
        {
            this.Fragmented_VerifyArrayDestructuringPattern7ForTuple(pattern, type);
        }
        else if (type.IsInstantiationOf(this.m_ModelCore.ArrayType))
        {
            this.Fragmented_VerifyArrayDestructuringPattern7ForArray(pattern, type);
        }
        else
        {
            var proxy = type.Delegate.Proxies.ContainsKey(Operator.ProxyToGetIndex) ? type.Delegate.Proxies[Operator.ProxyToGetIndex] : null;
            if (proxy != null && this.m_ModelCore.IsNumericType(proxy.StaticType.FunctionRequiredParameters[0].Type))
            {
                this.Fragmented_VerifyArrayDestructuringPattern7ForProxy(pattern, proxy);
            }
            else
            {
                if (type != m_ModelCore.AnyType)
                {
                    this.VerifyError(pattern.Span.Value.Script, 141, pattern.Span.Value, new DiagnosticArguments { ["t"] = type });
                }
                this.Fragmented_VerifyArrayDestructuringPattern7ForAny(pattern);
            }
        }
    }

    private void Fragmented_VerifyArrayDestructuringPattern7ForTuple(Ast.ArrayDestructuringPattern pattern, Symbol tupleType)
    {
        if (pattern.Items.Count() > tupleType.TupleElementTypes.Count())
        {
            this.VerifyError(pattern.Span.Value.Script, 142, pattern.Span.Value, new DiagnosticArguments { ["limit"] = tupleType.TupleElementTypes.Count() });
        }
        for (int i = 0; i < pattern.Items.Count(); ++i)
        {
            var item = pattern.Items[i];
            if (item is Ast.ArrayDestructuringSpread spread)
            {
                this.VerifyError(spread.Span.Value.Script, 143, spread.Span.Value, new DiagnosticArguments {});
                this.Fragmented_VerifyDestructuringPattern7(spread.Pattern, this.m_ModelCore.AnyType);
                continue;
            }
            // hole
            if (item == null)
            {
                continue;
            }
            var tupleItemType = i < tupleType.TupleElementTypes.Count() ? tupleType.TupleElementTypes[i] : null;
            this.Fragmented_VerifyDestructuringPattern7((Ast.DestructuringPattern) item, tupleItemType ?? this.m_ModelCore.AnyType);
        }
    }

    private void Fragmented_VerifyArrayDestructuringPattern7ForArray(Ast.ArrayDestructuringPattern pattern, Symbol arrayType)
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
                    this.VerifyError(spread.Span.Value.Script, 144, spread.Span.Value, new DiagnosticArguments {});
                }
                this.Fragmented_VerifyDestructuringPattern7(spread.Pattern, arrayType);
            }
            else
            {
                this.Fragmented_VerifyDestructuringPattern7((Ast.DestructuringPattern) item, arrayElementType);
            }
        }
    }

    private void Fragmented_VerifyArrayDestructuringPattern7ForProxy(Ast.ArrayDestructuringPattern pattern, Symbol proxy)
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
                    this.VerifyError(spread.Span.Value.Script, 144, spread.Span.Value, new DiagnosticArguments {});
                }
                // for a proxy destructuring, the rest will be a [T] type.
                this.Fragmented_VerifyDestructuringPattern7(spread.Pattern, this.m_ModelCore.Factory.TypeWithArguments(this.m_ModelCore.ArrayType, new Symbol[]{arrayElementType}));
            }
            else
            {
                this.Fragmented_VerifyDestructuringPattern7((Ast.DestructuringPattern) item, arrayElementType);
            }
        }
    }

    private void Fragmented_VerifyArrayDestructuringPattern7ForAny(Ast.ArrayDestructuringPattern pattern)
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
                if (i != pattern.Items.Count() -1)
                {
                    this.VerifyError(spread.Span.Value.Script, 144, spread.Span.Value, new DiagnosticArguments {});
                }
                this.Fragmented_VerifyDestructuringPattern7(spread.Pattern, anyType);
            }
            else
            {
                this.Fragmented_VerifyDestructuringPattern7((Ast.DestructuringPattern) item, anyType);
            }
        }
    }
}