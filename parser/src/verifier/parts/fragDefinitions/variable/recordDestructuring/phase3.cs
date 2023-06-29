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
    private void Fragmented_VerifyRecordDestructuringPattern3(Ast.RecordDestructuringPattern pattern)
    {
        foreach (var field in pattern.Fields)
        {
            if (field.Subpattern != null)
            {
                this.Fragmented_VerifyDestructuringPattern3(field.Subpattern);
            }
            else if (field.Key is Ast.StringLiteral)
            {
                this.Fragmented_VerifyRecordDestructuringPattern3Field(field);
            }
        }
    }

    private void Fragmented_VerifyRecordDestructuringPattern3Field(Ast.RecordDestructuringPatternField field)
    {
        var key = ((Ast.StringLiteral) field.Key).Value;
        var subtype = this.m_Frame.TypeFromFrame;
        if (subtype != null && subtype.IsInterfaceType)
        {
            if (InterfaceInheritanceInstancePropertiesHierarchy.HasProperty(subtype, key))
            {
                this.VerifyError(field.Span.Value.Script, 246, field.Span.Value, new DiagnosticArguments {["name"] = key});
            }
            return;
        }
        var superType = subtype?.SuperType;
        if (superType == null || field.SemanticProperty == null)
        {
            return;
        }
        if (SingleInheritanceInstancePropertiesHierarchy.HasProperty(superType, key))
        {
            this.VerifyError(field.Span.Value.Script, 246, field.Span.Value, new DiagnosticArguments {["name"] = key});
        }
    }
}