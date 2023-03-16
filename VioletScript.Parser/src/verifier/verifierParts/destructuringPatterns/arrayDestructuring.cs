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
        Symbol inferredType = null
    )
    {
        Symbol type = null;
        if (pattern.Type != null)
        {
            type = VerifyTypeExp(pattern.Type);
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

        if (type is TupleType)
        {
            VerifyArrayDestructuringPatternForTuple(pattern, readOnly, output, visibility, type);
        }
        else if (type.IsInstantiationOf(m_ModelCore.ArrayType))
        {
            VerifyArrayDestructuringPatternForArray(pattern, readOnly, output, visibility, type);
        }
        else
        {
            if (type != m_ModelCore.AnyType)
            {
                VerifyError(pattern.Span.Value.Script, 141, pattern.Span.Value, new DiagnosticArguments { ["t"] = type });
            }
            VerifyArrayDestructuringPatternForAny(pattern, readOnly, output, visibility);
        }
    }

    private void VerifyArrayDestructuringPatternForTuple
    (
        Ast.ArrayDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility,
        Symbol tupleType
    )
    {
        if (pattern.Items.Count() > tupleType.TupleElementTypes.Count())
        {
            VerifyError(pattern.Span.Value.Script, 142, pattern.Span.Value, new DiagnosticArguments { ["limit"] = tupleType.TupleElementTypes.Count() });
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
                VerifyError(spread.Span.Value.Script, 143, spread.Span.Value, new DiagnosticArguments {});
                VerifyDestructuringPattern(spread.Pattern, readOnly, output, visibility, m_ModelCore.AnyType);
            }
            else
            {
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

    private void VerifyArrayDestructuringPatternForAny
    (
        Ast.ArrayDestructuringPattern pattern,
        bool readOnly,
        Properties output,
        Visibility visibility
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
                VerifyDestructuringPattern(spread.Pattern, readOnly, output, visibility, anyType);
            }
            else
            {
                VerifyDestructuringPattern((Ast.DestructuringPattern) item, readOnly, output, visibility, anyType);
            }
        }
    }
}