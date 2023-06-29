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
    private Symbol[] VerifyTypeParameters(Ast.Generics generics, Properties propsOutput)
    {
        var r = FragmentedA_VerifyTypeParameters(generics, propsOutput);
        // resolve constraints
        FragmentedB_VerifyTypeParameters(r, generics, propsOutput);
        return r;
    }

    // declare the type parameters. this won't resolve its constraints.
    private Symbol[] FragmentedA_VerifyTypeParameters(Ast.Generics generics, Properties propsOutput, Symbol parentTypeOrMethod = null)
    {
        if (parentTypeOrMethod != null && parentTypeOrMethod.TypeParameters != null)
        {
            int i = 0;
            var reusedTypeParams = parentTypeOrMethod.TypeParameters;
            foreach (var paramNode in generics.Params)
            {
                if (i >= reusedTypeParams.Count())
                {
                    break;
                }
                var p = reusedTypeParams[i];
                if (paramNode.Id.Name != p.Name)
                {
                    throw new Exception("Defining " + parentTypeOrMethod.ToString() + " with wrong generic parameter name: expected " + p.Name + "; got " + paramNode.Id.Name);
                }
                paramNode.SemanticSymbol = p;
                propsOutput[p.Name] = p;
                i += 1;
            }
            return parentTypeOrMethod.TypeParameters;
        }
        var r = new List<Symbol>();
        foreach (var paramNode in generics.Params)
        {
            var p = m_ModelCore.Factory.TypeParameter(paramNode.Id.Name);
            r.Add(p);
            paramNode.SemanticSymbol = p;

            // assign property to 'propsOutput'
            if (propsOutput.Has(p.Name))
            {
                VerifyError(null, 139, paramNode.Id.Span.Value, new DiagnosticArguments { ["name"] = p.Name });
            }
            else
            {
                propsOutput[p.Name] = p;
            }
        }
        return r.ToArray();
    }

    // resolves the constraints of a generic declaration.
    private void FragmentedB_VerifyTypeParameters(Symbol[] typeParameters, Ast.Generics generics, Properties propsOutput)
    {
        var r = typeParameters;
        foreach (var paramNode in generics.Params)
        {
            // T: Constraint
            if (paramNode.DefaultIsBound != null)
            {
                Symbol defaultBound = VerifyTypeExp(paramNode.DefaultIsBound);
                if (defaultBound != null && defaultBound.IsClassType)
                {
                    paramNode.SemanticSymbol.SuperType = defaultBound;
                }
                else if (defaultBound != null && defaultBound.IsInterfaceType)
                {
                    paramNode.SemanticSymbol.AddImplementedInterface(defaultBound);
                }
                else if (defaultBound != null)
                {
                    VerifyError(null, 224, paramNode.DefaultIsBound.Span.Value, new DiagnosticArguments { ["t"] = defaultBound });
                }
            }
        }
        if (generics.Bounds != null)
        {
            foreach (var boundNode in generics.Bounds)
            {
                var isBoundNode = (Ast.GenericTypeParameterIsBound) boundNode;
                Symbol typeParameter = VerifyGenericBoundLex(isBoundNode.Id);
                if (typeParameter == null)
                {
                    VerifyTypeExp(isBoundNode.Type);
                    continue;
                }
                if (!r.Contains(typeParameter))
                {
                    var shadowed = typeParameter;
                    typeParameter = shadowed.CloneTypeParameter();
                    typeParameter.ShadowsTypeParameter ??= shadowed;
                    m_Frame.Properties[typeParameter.Name] = typeParameter;
                }
                isBoundNode.SemanticTypeParameter = typeParameter;

                Symbol bound = VerifyTypeExp(isBoundNode.Type);
                if (bound != null && bound.IsClassType)
                {
                    typeParameter.SuperType = bound;
                }
                else if (bound != null && bound.IsInterfaceType)
                {
                    typeParameter.AddImplementedInterface(bound);
                }
                else if (bound != null)
                {
                    VerifyError(null, 224, isBoundNode.Type.Span.Value, new DiagnosticArguments { ["t"] = bound });
                }
            }
        }
    }

    // verifies lexical reference resolving to a type parameter.
    private Symbol VerifyGenericBoundLex(Ast.Identifier id)
    {
        var r = m_Frame.ResolveProperty(id.Name);
        if (r == null)
        {
            // VerifyError: undefined reference
            VerifyError(null, 128, id.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
            return null;
        }
        else if (r is AmbiguousReferenceIssue)
        {
            // VerifyError: ambiguous reference
            VerifyError(null, 129, id.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
            return null;
        }
        else
        {
            if (!r.PropertyIsVisibleTo(m_Frame))
            {
                // VerifyError: accessing private property
                VerifyError(null, 130, id.Span.Value, new DiagnosticArguments { ["name"] = id.Name });
            }
            r = r?.EscapeAlias();
            if (!(r is TypeParameter))
            {
                // VerifyError: not a type parameter
                VerifyError(null, 225, id.Span.Value, new DiagnosticArguments {});
                return null;
            }

            return r;
        }
    }

    // verify arguments to a generic type or function; ensure:
    // - the number of arguments is correct.
    // - the arguments follow the constraints.
    //
    // note that, for generic functions, this method does not automatically wrap
    // them into reference values, for now.
    //
    // note that type expressions with arguments (G.<T>) defer validating
    // the constraints to the `VerifyPrograms` method.
    //
    private Symbol VerifyGenericTypeArguments(Span wholeSpan, Symbol genericTypeOrF, List<Ast.TypeExpression> giTeArguments, Ast.TypeExpressionWithArguments surroundingTypeExp = null)
    {
        Symbol[] typeParameters = genericTypeOrF.TypeParameters;
        // VerifyError: wrong number of arguments
        if (giTeArguments.Count() != typeParameters.Count())
        {
            VerifyError(null, 135, wholeSpan, new DiagnosticArguments { ["expectedN"] = typeParameters.Count(), ["gotN"] = giTeArguments.Count() });
            return null;
        }
        var structurallyInvalid = false;
        var arguments = giTeArguments.Select(te =>
        {
            var r = VerifyTypeExp(te);
            structurallyInvalid = structurallyInvalid || r == null;
            return r ?? m_ModelCore.AnyType;
        }).ToArray();

        if (structurallyInvalid)
        {
            return null;
        }

        if (surroundingTypeExp != null)
        {
            // assert
            if (m_TypeExpsWithArguments == null)
            {
                throw new Exception("Program verification must be done through verifyPrograms()");
            }
            m_TypeExpsWithArguments.Add(surroundingTypeExp);
        }
        else
        {
            for (int i = 0; i < arguments.Count(); ++i)
            {
                var argument = arguments[i];
                var argumentExp = giTeArguments[i];
                foreach (var @param in typeParameters)
                {
                    foreach (var constraintItrfc in @param.ImplementsInterfaces)
                    {
                        // VerifyError: missing interface constraint
                        if (!argument.IsSubtypeOf(constraintItrfc))
                        {
                            VerifyError(null, 136, argumentExp.Span.Value, new DiagnosticArguments { ["t"] = constraintItrfc });
                        }
                    }
                    // VerifyError: missing class constraint
                    if (@param.SuperType != null && !argument.IsSubtypeOf(@param.SuperType))
                    {
                        VerifyError(null, 136, argumentExp.Span.Value, new DiagnosticArguments { ["t"] = @param.SuperType });
                    }
                }
            }
        }
        if (genericTypeOrF is Type)
        {
            return m_ModelCore.InternTypeWithArguments(genericTypeOrF, arguments);
        }
        else if (genericTypeOrF is Alias)
        {
            return genericTypeOrF.AliasToSymbol.ReplaceTypes(genericTypeOrF.TypeParameters, arguments);
        }
        else
        {
            return m_ModelCore.InternMethodSlotWithTypeArgs(genericTypeOrF, arguments);
        }
    }

    private void VerifyAllTypeExpsWithArgs()
    {
        foreach (var gi in m_TypeExpsWithArguments)
        {
            var typeParameters = gi.Base.SemanticSymbol.TypeParameters;
            var arguments = gi.ArgumentsList.Select(te => te.SemanticSymbol).ToArray();

            for (int i = 0; i < arguments.Count(); ++i)
            {
                var argument = arguments[i];
                var argumentExp = gi.ArgumentsList[i];
                foreach (var @param in typeParameters)
                {
                    foreach (var constraintItrfc in @param.ImplementsInterfaces)
                    {
                        // VerifyError: missing interface constraint
                        if (!argument.IsSubtypeOf(constraintItrfc))
                        {
                            VerifyError(null, 136, argumentExp.Span.Value, new DiagnosticArguments { ["t"] = constraintItrfc });
                        }
                    }
                    // VerifyError: missing class constraint
                    if (@param.SuperType != null && !argument.IsSubtypeOf(@param.SuperType))
                    {
                        VerifyError(null, 136, argumentExp.Span.Value, new DiagnosticArguments { ["t"] = @param.SuperType });
                    }
                }
            }
        }
        m_TypeExpsWithArguments.Clear();
        m_TypeExpsWithArguments = null;
    }
}