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
    // verify an array destructuring pattern for assignment expressions.
    private void VerifyAssignmentArrayDestructuringPattern(Ast.ArrayDestructuringPattern pattern, Symbol type)
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
        if (type is TupleType)
        {
            VerifyAssignmentArrayDestructuringPatternForTuple(pattern, type);
        }
        else if (type.IsInstantiationOf(m_ModelCore.ArrayType))
        {
            VerifyAssignmentArrayDestructuringPatternForArray(pattern, type);
        }
        else
        {
            var proxy = type.Delegate.Proxies.ContainsKey(Operator.ProxyToGetIndex) ? type.Delegate.Proxies[Operator.ProxyToGetIndex] : null;
            if (proxy != null && this.m_ModelCore.IsNumericType(proxy.StaticType.FunctionRequiredParameters[0].Type))
            {
                VerifyAssignmentArrayDestructuringPatternForProxy(pattern, proxy);
            }
            else
            {
                if (type != m_ModelCore.AnyType)
                {
                    VerifyError(null, 141, pattern.Span.Value, new DiagnosticArguments { ["t"] = type });
                }
                VerifyAssignmentArrayDestructuringPatternForAny(pattern);
            }
        }
    }

    private void VerifyAssignmentArrayDestructuringPatternForTuple
    (
        Ast.ArrayDestructuringPattern pattern,
        Symbol tupleType
    )
    {
        if (pattern.Items.Count() > tupleType.TupleElementTypes.Count())
        {
            VerifyError(null, 142, pattern.Span.Value, new DiagnosticArguments { ["limit"] = tupleType.TupleElementTypes.Count() });
        }
        for (int i = 0; i < pattern.Items.Count(); ++i)
        {
            var item = pattern.Items[i];
            var tupleItemType = i < tupleType.TupleElementTypes.Count() ? tupleType.TupleElementTypes[i] : null;
            if (item == null)
            {
                // ellision
            }
            else if (item is Ast.ArrayDestructuringSpread spread)
            {
                VerifyError(null, 143, spread.Span.Value, new DiagnosticArguments {});
                VerifyAssignmentDestructuringPattern(spread.Pattern, m_ModelCore.AnyType);
            }
            else
            {
                VerifyAssignmentDestructuringPattern((Ast.DestructuringPattern) item, tupleItemType ?? m_ModelCore.AnyType);
            }
        }
    }

    private void VerifyAssignmentArrayDestructuringPatternForArray
    (
        Ast.ArrayDestructuringPattern pattern,
        Symbol arrayType
    )
    {
        var arrayElementType = arrayType.ArgumentTypes[0];
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
                    VerifyError(null, 144, spread.Span.Value, new DiagnosticArguments {});
                }
                VerifyAssignmentDestructuringPattern(spread.Pattern, arrayType);
            }
            else
            {
                VerifyAssignmentDestructuringPattern((Ast.DestructuringPattern) item, arrayElementType);
            }
        }
    }

    private void VerifyAssignmentArrayDestructuringPatternForProxy
    (
        Ast.ArrayDestructuringPattern pattern,
        Symbol proxy
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
                if (i != pattern.Items.Count() - 1)
                {
                    VerifyError(null, 144, spread.Span.Value, new DiagnosticArguments {});
                }
                // for a proxy destructuring, the rest will be a [T] type.
                VerifyAssignmentDestructuringPattern(spread.Pattern, this.m_ModelCore.Factory.TypeWithArguments(this.m_ModelCore.ArrayType, new Symbol[]{arrayElementType}));
            }
            else
            {
                VerifyAssignmentDestructuringPattern((Ast.DestructuringPattern) item, arrayElementType);
            }
        }
    }

    private void VerifyAssignmentArrayDestructuringPatternForAny(Ast.ArrayDestructuringPattern pattern)
    {
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
                    VerifyError(null, 144, spread.Span.Value, new DiagnosticArguments {});
                }
                VerifyAssignmentDestructuringPattern(spread.Pattern, m_ModelCore.AnyType);
            }
            else
            {
                VerifyAssignmentDestructuringPattern((Ast.DestructuringPattern) item, m_ModelCore.AnyType);
            }
        }
    }
}