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
    private void Fragmented_VerifyNondestructuringPattern3(Ast.NondestructuringPattern pattern)
    {
        var subtype = this.m_Frame.TypeFromFrame;
        if (subtype != null && subtype.IsInterfaceType)
        {
            if (InterfaceInheritanceInstancePropertiesHierarchy.HasProperty(subtype, pattern.Name))
            {
                this.VerifyError(pattern.Span.Value.Script, 246, pattern.Span.Value, new DiagnosticArguments {["name"] = pattern.Name});
            }
            return;
        }
        var superType = subtype?.SuperType;
        if (superType == null || pattern.SemanticProperty == null)
        {
            return;
        }
        if (SingleInheritanceInstancePropertiesHierarchy.HasProperty(superType, pattern.Name))
        {
            this.VerifyError(pattern.Span.Value.Script, 246, pattern.Span.Value, new DiagnosticArguments {["name"] = pattern.Name});
        }
    }
}