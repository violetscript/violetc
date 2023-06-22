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
    // verifies array initializer.
    private Symbol VerifyArrayInitialiser(Ast.ArrayInitializer exp, Symbol expectedType)
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
            type = type.UnionMemberTypes.Where(t => t.TypeCanUseArrayInitializer).FirstOrDefault();
        }

        if (type == null)
        {
            // VerifyError: no infer type
            VerifyError(null, 186, exp.Span.Value, new DiagnosticArguments {});
            type = m_ModelCore.AnyType;
        }
        else if (!type.TypeCanUseArrayInitializer)
        {
            // VerifyError: cannot initialise type
            VerifyError(null, 190, exp.Span.Value, new DiagnosticArguments {["t"] = type});
            type = m_ModelCore.AnyType;
        }

        if (type == m_ModelCore.AnyType)
        {
            Any_VerifyArrayInitialiser(exp);
        }
        else if (type.IsInstantiationOf(m_ModelCore.ArrayType))
        {
            Array_VerifyArrayInitialiser(exp, type);
        }
        else if (type.IsInstantiationOf(m_ModelCore.SetType))
        {
            Set_VerifyArrayInitialiser(exp, type);
        }
        else if (type.IsFlagsEnum)
        {
            Flags_VerifyArrayInitialiser(exp, type);
        }
        else
        {
            Tuple_VerifyArrayInitialiser(exp, type);
        }

        exp.SemanticSymbol = m_ModelCore.Factory.Value(type);
        exp.SemanticExpResolved = true;
        return exp.SemanticSymbol;
    } // array initializer

    private void Any_VerifyArrayInitialiser(Ast.ArrayInitializer exp)
    {
        foreach (var itemOrHole in exp.Items)
        {
            if (itemOrHole == null)
            {
                continue;
            }
            if (itemOrHole is Ast.Spread spread)
            {
                LimitExpType(spread.Expression, m_ModelCore.AnyType);
                continue;
            }
            LimitExpType(itemOrHole, m_ModelCore.AnyType);
        }
    } // array initializer (any)

    private void Array_VerifyArrayInitialiser(Ast.ArrayInitializer exp, Symbol type)
    {
        var elementType = type.ArgumentTypes[0];
        Symbol spreadUnionType = null;
 
        foreach (var itemOrHole in exp.Items)
        {
            if (itemOrHole == null)
            {
                continue;
            }
            if (itemOrHole is Ast.Spread spread)
            {
                spreadUnionType ??= m_ModelCore.Factory.UnionType(new Symbol[]
                {
                    type,
                    m_ModelCore.Factory.TypeWithArguments(m_ModelCore.IteratorType, new Symbol[]
                    {
                        elementType,
                    }),
                });
                LimitExpType(spread.Expression, spreadUnionType);
                continue;
            }
            LimitExpType(itemOrHole, elementType);
        }
    } // array initializer (Array)

    private void Set_VerifyArrayInitialiser(Ast.ArrayInitializer exp, Symbol type)
    {
        var elementType = type.ArgumentTypes[0];
        Symbol spreadUnionType = null;
 
        foreach (var itemOrHole in exp.Items)
        {
            if (itemOrHole == null)
            {
                continue;
            }
            if (itemOrHole is Ast.Spread spread)
            {
                spreadUnionType ??= m_ModelCore.Factory.TypeWithArguments(m_ModelCore.IteratorType, new Symbol[]
                {
                    elementType,
                });
                LimitExpType(spread.Expression, spreadUnionType);
                continue;
            }
            LimitExpType(itemOrHole, elementType);
        }
    } // array initializer (Set)

    private void Flags_VerifyArrayInitialiser(Ast.ArrayInitializer exp, Symbol type)
    {
        foreach (var itemOrHole in exp.Items)
        {
            if (itemOrHole == null)
            {
                continue;
            }
            if (itemOrHole is Ast.Spread spread)
            {
                LimitExpType(spread.Expression, type);
                continue;
            }
            LimitExpType(itemOrHole, type);
        }
    } // array initializer (flags)

    private void Tuple_VerifyArrayInitialiser(Ast.ArrayInitializer exp, Symbol type)
    {
        var tupleElTypes = type.TupleElementTypes;
        if (exp.Items.Count() != type.CountOfTupleElements)
        {
            VerifyError(null, 191, exp.Span.Value, new DiagnosticArguments { ["expected"] = type.CountOfTupleElements, ["got"] = exp.Items.Count() });
        }
        for (int i = 0; i < exp.Items.Count(); ++i)
        {
            var itemOrHole = exp.Items[i];
            if (itemOrHole == null)
            {
                continue;
            }
            if (itemOrHole is Ast.Spread spread)
            {
                VerifyError(null, 192, spread.Span.Value, new DiagnosticArguments {});
                LimitExpType(spread.Expression, m_ModelCore.AnyType);
                continue;
            }
            LimitExpType(itemOrHole, i < type.CountOfTupleElements ? tupleElTypes[i] : m_ModelCore.AnyType);
        }
    } // array initializer (tuple)
}