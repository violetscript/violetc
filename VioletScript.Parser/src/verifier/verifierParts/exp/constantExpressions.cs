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
    public Symbol VerifyConstantExp
    (
        Ast.Expression exp,
        bool faillible,
        Symbol expectedType = null,
        bool instantiatingGeneric = false
    )
    {
        if (exp.SemanticConstantExpResolved)
        {
            return exp.SemanticSymbol;
        }
        // verify identifier; ensure
        // - it is not undefined.
        // - it is not an ambiguous reference.
        // - it is lexically visible.
        // - if it is a non-argumented generic type or function, throw a VerifyError.
        // - it is a compile-time value.
        if (exp is Ast.Identifier id)
        {
            var r = m_Frame.ResolveProperty(id.Name);
            if (r == null)
            {
                // VerifyError: undefined reference
                if (faillible)
                {
                    VerifyError(null, 128, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else if (r is AmbiguousReferenceIssue)
            {
                // VerifyError: ambiguous reference
                if (faillible)
                {
                    VerifyError(null, 129, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else
            {
                if (!r.PropertyIsVisibleTo(m_Frame))
                {
                    // VerifyError: accessing private property
                    if (faillible)
                    {
                        VerifyError(null, 130, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }
                r = r is Alias ? r.AliasToSymbol : r;
                // VerifyError: unargumented generic type or function
                if (!instantiatingGeneric && r.TypeParameters != null)
                {
                    if (faillible)
                    {
                        VerifyError(null, 132, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }

                // extend variable life
                if (r is ReferenceValueFromFrame && r.Base.FindActivation() != m_Frame.FindActivation())
                {
                    r.Base.FindActivation().AddExtendedLifeVariable(r.Property);
                }

                // use initial constant value if any.
                if ((r is ReferenceValue || r is ReferenceValueFromNamespace || r is ReferenceValueFromType) && r.Property.InitValue is ConstantValue)
                {
                    r = r.Property.InitValue;
                }

                if  (!(r is Type || r is ConstantValue || r is Namespace))
                {
                    if (faillible)
                    {
                        VerifyError(null, 151, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }

                if (id.Type != null)
                {
                    VerifyError(null, 152, exp.Span.Value, new DiagnosticArguments {});
                    VerifyTypeExp(id.Type);
                }

                exp.SemanticSymbol = r;
                exp.SemanticConstantExpResolved = true;
                return r;
            }
        } // Identifier
        // verify member; ensure
        // - it is not undefined.
        // - it is lexically visible.
        // - if it is a non-argumented generic type or function, throw a VerifyError.
        // - it is a compile-time value.
        if (exp is Ast.MemberExpression memb && !memb.Optional)
        {
            var @base = VerifyConstantExp(memb.Base, faillible, null, false);
            if (@base == null)
            {
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            var r = @base.ResolveProperty(memb.Id.Name);
            if (r == null)
            {
                // VerifyError: undefined reference
                if (faillible)
                {
                    VerifyError(null, 128, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                }
                exp.SemanticSymbol = null;
                exp.SemanticConstantExpResolved = true;
                return exp.SemanticSymbol;
            }
            else
            {
                if (!r.PropertyIsVisibleTo(m_Frame))
                {
                    // VerifyError: accessing private property
                    if (faillible)
                    {
                        VerifyError(null, 130, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }
                r = r is Alias ? r.AliasToSymbol : r;
                // VerifyError: unargumented generic type or function
                if (!instantiatingGeneric && r.TypeParameters != null)
                {
                    if (faillible)
                    {
                        VerifyError(null, 132, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }

                // extend variable life
                if (r is ReferenceValueFromFrame && r.Base.FindActivation() != m_Frame.FindActivation())
                {
                    r.Base.FindActivation().AddExtendedLifeVariable(r.Property);
                }

                // use initial constant value if any.
                if ((r is ReferenceValue || r is ReferenceValueFromNamespace || r is ReferenceValueFromType) && r.Property.InitValue is ConstantValue)
                {
                    r = r.Property.InitValue;
                }

                if  (!(r is Type || r is ConstantValue || r is Namespace))
                {
                    if (faillible)
                    {
                        VerifyError(null, 151, memb.Id.Span.Value, new DiagnosticArguments { ["name"] = memb.Id.Name });
                    }
                    exp.SemanticSymbol = null;
                    exp.SemanticConstantExpResolved = true;
                    return exp.SemanticSymbol;
                }

                exp.SemanticSymbol = r;
                exp.SemanticConstantExpResolved = true;
                return r;
            }
        } // MemberExpression
        else
        {
            if (faillible)
            {
                VerifyError(null, 150, exp.Span.Value, new DiagnosticArguments {});
            }
            exp.SemanticSymbol = null;
            exp.SemanticConstantExpResolved = true;
            return exp.SemanticSymbol;
        }
    }
}