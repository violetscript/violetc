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
    private void VerifyTypeDefinition(Ast.TypeDefinition defn)
    {
        defn.SemanticRightFrame = m_ModelCore.Factory.Frame();
        Symbol[] typeParameters = null;
        if (defn.Generics != null)
        {
            typeParameters = VerifyTypeParameters(defn.Generics, defn.SemanticRightFrame.Properties);
        }
        EnterFrame(defn.SemanticRightFrame);
        var right = VerifyTypeExp(defn.Type);
        ExitFrame();

        var alias = m_ModelCore.Factory.Alias(defn.Id.Name, right);
        alias.TypeParameters = typeParameters;
        defn.SemanticAlias = alias;

        if (m_Frame.Properties.Has(alias.Name))
        {
            VerifyError(null, 139, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = alias.Name });
        }
        else
        {
            m_Frame.Properties[alias.Name] = alias;
        }
    } // type definition
}