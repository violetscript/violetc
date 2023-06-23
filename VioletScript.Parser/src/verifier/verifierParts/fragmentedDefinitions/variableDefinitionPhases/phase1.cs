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
    private void Fragmented_VerifyVariableDefinition1(Ast.VariableDefinition defn)
    {
        var parentDefinition = m_Frame.TypeFromFrame ?? m_Frame.NamespaceFromFrame ?? m_Frame.PackageFromFrame;

        // if there is no parent item, this is a program top-level
        // variable.
        if (parentDefinition == null)
        {
            this.VerifyVariableDefinition(defn);
            return;
        }

        // determine target properties. this depends
        // on the 'static' modifier.
        toDo();

        foreach (var binding in defn.Bindings)
        {
            this.Fragmented_VerifyVariableBinding1(binding, defn.ReadOnly, output, Visibility.Public);
        }
    }

    private void Fragmented_VerifyVariableBinding1(
        Ast.VariableBinding binding,
        bool readOnly,
        Properties output,
        Visibility visibility
    )
    {
    }
}