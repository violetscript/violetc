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
    public Symbol VerifyTypeExp(Ast.TypeExpression exp, bool isBase = false)
    {
        if (exp.SemanticResolved)
        {
            return exp.SemanticSymbol;
        }
        // verify identifier; ensure
        // - it is not undefined.
        // - it is not an ambiguous reference.
        // - it is lexically visible.
        // - it is a type unless it is the base of a member.
        // - if it is a non-argumented generic type, throw a VerifyError.
        if (exp is Ast.IdentifierTypeExpression id)
        {
            var r = m_Frame.ResolveProperty(id.Name);
            if (r == null)
            {
                // VerifyError: undefined reference
                VerifyError(null, 128, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                exp.SemanticSymbol = null;
                exp.SemanticResolved = true;
                return exp.SemanticSymbol;
            }
            else if (r is AmbiguousReferenceIssue)
            {
                // VerifyError: ambiguous reference
                VerifyError(null, 129, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                exp.SemanticSymbol = null;
                exp.SemanticResolved = true;
                return exp.SemanticSymbol;
            }
            else
            {
                if (!r.PropertyIsVisibleTo(m_Frame))
                {
                    // VerifyError: accessing private property
                    VerifyError(null, 130, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                }
                r = r is Alias ? r.AliasToSymbol : r;
                if (!isBase && !(r is Type))
                {
                    // VerifyError: not a type constant
                    VerifyError(null, 131, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                    exp.SemanticSymbol = null;
                    exp.SemanticResolved = true;
                    return exp.SemanticSymbol;
                }
                // VerifyError: unargumented generic type
                if (!isBase && r.TypeParameters != null)
                {
                    VerifyError(null, 132, exp.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
                    exp.SemanticSymbol = null;
                    exp.SemanticResolved = true;
                    return exp.SemanticSymbol;
                }

                // extend variable life
                if (r is ReferenceValueFromFrame && r.Base.FindActivation() != m_Frame.FindActivation())
                {
                    r.Base.FindActivation().AddExtendedLifeVariable(r.Property);
                }

                exp.SemanticSymbol = r;
                exp.SemanticResolved = true;
                return r;
            }
        } // IdentifierTypeExpression
        else if (exp is Ast.AnyTypeExpression)
        {
            exp.SemanticSymbol = m_ModelCore.AnyType;
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        }
        else if (exp is Ast.VoidTypeExpression || exp is Ast.UndefinedTypeExpression)
        {
            exp.SemanticSymbol = m_ModelCore.UndefinedType;
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        }
        else if (exp is Ast.NullTypeExpression)
        {
            exp.SemanticSymbol = m_ModelCore.NullType;
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        }
        else if (exp is Ast.FunctionTypeExpression fte)
        {
            List<NameAndTypePair> @params = null;
            List<NameAndTypePair> optParams = null;
            NameAndTypePair? restParam = null;
            Symbol returnType = null;
            Symbol p = null;
            bool structurallyInvalid = false;
            if (fte.Params != null)
            {
                @params = new List<NameAndTypePair>();
                foreach (var paramId in fte.Params)
                {
                    p = VerifyTypeExp(paramId.Type);
                    structurallyInvalid = structurallyInvalid || p == null;
                    @params.Add(new NameAndTypePair(paramId.Name, p ?? m_ModelCore.AnyType));
                }
            }
            if (fte.OptParams != null)
            {
                optParams = new List<NameAndTypePair>();
                foreach (var paramId in fte.OptParams)
                {
                    p = VerifyTypeExp(paramId.Type);
                    structurallyInvalid = structurallyInvalid || p == null;
                    optParams.Add(new NameAndTypePair(paramId.Name, p ?? m_ModelCore.AnyType));
                }
            }
            if (fte.RestParam != null)
            {
                p = VerifyTypeExp(fte.RestParam.Type);
                structurallyInvalid = structurallyInvalid || p == null;
                restParam = new NameAndTypePair(fte.RestParam.Name, p ?? m_ModelCore.AnyType);
            }
            returnType = fte.ReturnType != null ? VerifyTypeExp(fte.ReturnType) : m_ModelCore.AnyType;
            if (returnType == null)
            {
                structurallyInvalid = true;
                returnType = m_ModelCore.AnyType;
            }
            exp.SemanticSymbol = structurallyInvalid ? null : m_ModelCore.InternFunctionType(@params?.ToArray(), optParams?.ToArray(), restParam, returnType);
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        } // FunctionTypeExpression
        else if (exp is Ast.ArrayTypeExpression arrayTe)
        {
            var itemType = VerifyTypeExp(arrayTe.ItemType);
            exp.SemanticSymbol = itemType == null ? null : m_ModelCore.InternInstantiatedType(m_ModelCore.ArrayType, new Symbol[]{itemType});
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        }
        else if (exp is Ast.TupleTypeExpression tupleTe)
        {
            var structurallyInvalid = false;
            var items = tupleTe.ItemTypes.Select(te =>
            {
                var r = VerifyTypeExp(te);
                structurallyInvalid = structurallyInvalid || r == null;
                return r ?? m_ModelCore.AnyType;
            }).ToArray();
            exp.SemanticSymbol = structurallyInvalid ? null : m_ModelCore.InternTupleType(items);
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        }
        else if (exp is Ast.RecordTypeExpression recordTe)
        {
            var structurallyInvalid = false;
            var fields = recordTe.Fields.Select(field =>
            {
                var fieldType = VerifyTypeExp(field.Type);
                structurallyInvalid = structurallyInvalid || fieldType == null;
                return new NameAndTypePair(field.Name, fieldType ?? m_ModelCore.AnyType);
            }).ToArray();
            exp.SemanticSymbol = structurallyInvalid ? null : m_ModelCore.InternRecordType(fields);
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        }
        else if (exp is Ast.ParensTypeExpression parensTe)
        {
            exp.SemanticSymbol = VerifyTypeExp(parensTe.Base);
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        }
        else if (exp is Ast.UnionTypeExpression unionTe)
        {
            var structurallyInvalid = false;
            var items = unionTe.Types.Select(te =>
            {
                var r = VerifyTypeExp(te);
                structurallyInvalid = structurallyInvalid || r == null;
                return r ?? m_ModelCore.AnyType;
            }).ToArray();
            exp.SemanticSymbol = structurallyInvalid ? null : m_ModelCore.InternUnionType(items);
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        }
        // verify member; ensure
        // - it is not undefined.
        // - it is lexically visible.
        // - it is a type unless it is the base of a member.
        // - if it is a non-argumented generic type, throw a VerifyError.
        else if (exp is Ast.MemberTypeExpression memberTe)
        {
            var @base = VerifyTypeExp(memberTe.Base, true);
            if (@base == null)
            {
                exp.SemanticSymbol = null;
                exp.SemanticResolved = true;
                return exp.SemanticSymbol;
            }
            var r = @base.ResolveProperty(memberTe.Id.Name);
            if (r == null)
            {
                // VerifyError: undefined reference
                VerifyError(memberTe.Id.Span.Value.Script, 128, memberTe.Id.Span.Value, new DiagnosticArguments { ["name"] = memberTe.Id.Name });
                exp.SemanticSymbol = null;
                exp.SemanticResolved = true;
                return exp.SemanticSymbol;
            }
            else
            {
                if (!r.PropertyIsVisibleTo(m_Frame))
                {
                    // VerifyError: accessing private property
                    VerifyError(exp.Span.Value.Script, 130, memberTe.Id.Span.Value, new DiagnosticArguments { ["name"] = memberTe.Id.Name });
                }
                r = r is Alias ? r.AliasToSymbol : r;
                if (!isBase && !(r is Type))
                {
                    // VerifyError: not a type constant
                    VerifyError(memberTe.Id.Span.Value.Script, 131, memberTe.Id.Span.Value, new DiagnosticArguments { ["name"] = memberTe.Id.Name });
                    exp.SemanticSymbol = null;
                    exp.SemanticResolved = true;
                    return exp.SemanticSymbol;
                }
                // VerifyError: unargumented generic type
                if (!isBase && r.TypeParameters != null)
                {
                    VerifyError(memberTe.Id.Span.Value.Script, 132, memberTe.Id.Span.Value, new DiagnosticArguments { ["name"] = memberTe.Id.Name });
                    exp.SemanticSymbol = null;
                    exp.SemanticResolved = true;
                    return exp.SemanticSymbol;
                }
                exp.SemanticSymbol = r;
                exp.SemanticResolved = true;
                return exp.SemanticSymbol;
            }
        } // MemberTypeExpression
        // verify a generic instantiation; ensure:
        // - the base is a generic type.
        // - the number of arguments is correct.
        // - the arguments follow the constraints.
        else if (exp is Ast.GenericInstantiationTypeExpression giTe)
        {
            var @base = VerifyTypeExp(giTe.Base, true);
            if (@base == null)
            {
                exp.SemanticSymbol = null;
                exp.SemanticResolved = true;
                return exp.SemanticSymbol;
            }
            if (!(@base is Type) || @base.TypeParameters == null)
            {
                // VerifyError: base is not a generic type
                if (@base is Type)
                {
                    VerifyError(exp.Span.Value.Script, 133, giTe.Base.Span.Value, new DiagnosticArguments { ["t"] = @base });
                }
                // VerifyError: base is not a type constant
                else
                {
                    VerifyError(exp.Span.Value.Script, 134, giTe.Base.Span.Value, new DiagnosticArguments {});
                }
                exp.SemanticSymbol = null;
                exp.SemanticResolved = true;
                return exp.SemanticSymbol;
            }
            exp.SemanticSymbol = VerifyGenericInstArguments(exp.Span.Value, @base, giTe.ArgumentsList);
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        } // GenericInstantiationTypeExpression
        else if (exp is Ast.NullableTypeExpression nullableTe)
        {
            exp.SemanticSymbol = VerifyTypeExp(nullableTe.Base)?.ToNullableType();
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        }
        else if (exp is Ast.NonNullableTypeExpression nonNullableTe)
        {
            exp.SemanticSymbol = VerifyTypeExp(nonNullableTe.Base)?.ToNonNullableType();
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        }
        else if (exp is Ast.TypedTypeExpression typedTe)
        {
            VerifyError(exp.Span.Value.Script, 137, exp.Span.Value, new DiagnosticArguments {});
            VerifyTypeExp(typedTe.Base);
            exp.SemanticSymbol = VerifyTypeExp(typedTe.Type);
            exp.SemanticResolved = true;
            return exp.SemanticSymbol;
        }
        throw new Exception("Uncovered type expression");
    }
}