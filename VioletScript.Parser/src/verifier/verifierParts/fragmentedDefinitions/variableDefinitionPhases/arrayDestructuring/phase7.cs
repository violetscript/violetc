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
    private void Fragmented_VerifyArrayDestructuringPattern7(Ast.ArrayDestructuringPattern pattern, Symbol init)
    {
        pattern.SemanticProperty.StaticType ??= init?.StaticType ?? this.m_ModelCore.AnyType;
        pattern.SemanticProperty.InitValue ??= pattern.SemanticProperty.StaticType?.DefaultValue;

        foreach (var item in pattern.Items)
        {
            if (item is Ast.ArrayDestructuringSpread spread)
            {
                toDo();
                this.Fragmented_VerifyDestructuringPattern7(spread.Pattern, init);
                continue;
            }
            // hole
            if (item == null)
            {
                toDo();
                continue;
            }
            toDo();
            this.Fragmented_VerifyDestructuringPattern7((Ast.DestructuringPattern) item, init);
        }
    }
}