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
    private void VerifyFunctionCall(List<Ast.Expression> arguments, Span span, Symbol type)
    {
        if (arguments.Count() < type.FunctionCountOfRequiredParameters)
        {
            VerifyError(null, 202, span, new DiagnosticArguments {["atLeast"] = type.FunctionCountOfRequiredParameters});
        }
        var restType = type.FunctionRestParameter?.Type;
        var parameters = RequiredOrOptOrRestParam.FromType(type);
        if (arguments.Count() > parameters.Count() && restType == null)
        {
            VerifyError(null, 203, span, new DiagnosticArguments {["atMost"] = parameters.Count()});
        }
        if (restType != null)
        {
            parameters.RemoveAt(parameters.Count() - 1);
        }
        Symbol restTypeElementType = null;
        for (int i = 0; i < arguments.Count(); ++i)
        {
            if (i < parameters.Count())
            {
                LimitExpType(arguments[i], parameters[i].NameAndType.Type);
            }
            else if (restType != null)
            {
                restTypeElementType ??= restType.IsArgumentationOf(m_ModelCore.ArrayType)
                    ? restType.ArgumentTypes[0] : m_ModelCore.AnyType;
                LimitExpType(arguments[i], restTypeElementType);
            }
            else
            {
                LimitExpType(arguments[i], m_ModelCore.AnyType);
            }
        }
    }
}