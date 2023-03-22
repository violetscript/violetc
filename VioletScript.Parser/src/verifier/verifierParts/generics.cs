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
    private List<Symbol> VerifyTypeParameters(Ast.Generics generics, Properties propsOutput)
    {
        var r = new List<Symbol>();
        foreach (var paramNode in generics.Params)
        {
            var p = m_ModelCore.Factory.TypeParameter(paramNode.Name);
            r.Add(p);
            // T:Cons
            if (paramNode.DefaultIsBound != null)
            {
                //
                doFooBarQuxBaz();
            }
            // assign property to 'propsOutput'
            doFooBarQuxBaz();
        }
        if (generics.Bounds != null)
        {
            foreach (var boundNode in generics.Bounds)
            {
                //
                doFooBarQuxBaz();
            }
        }
        return r;
    }

    // verify arguments to a generic type or function; ensure:
    // - the number of arguments is correct.
    // - the arguments follow the constraints.
    //
    // note that, for generic functions, this method does not automatically wrap
    // them into reference values, for now.
    //
    private Symbol VerifyGenericInstArguments(Span wholeSpan, Symbol genericTypeOrF, List<Ast.TypeExpression> giTeArguments)
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
                if (argument.SuperType != null && !argument.IsSubtypeOf(@param.SuperType))
                {
                    VerifyError(null, 136, argumentExp.Span.Value, new DiagnosticArguments { ["t"] = @param.SuperType });
                }
            }
        }
        if (genericTypeOrF is Type)
        {
            return m_ModelCore.InternInstantiatedType(genericTypeOrF, arguments);
        }
        else
        {
            return m_ModelCore.InternInstantiationOfTypeParamMethodSlot(genericTypeOrF, arguments);
        }
    }
}