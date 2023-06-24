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
    private void Fragmented_VerifyVariableDefinition7(Ast.VariableDefinition defn)
    {
        // if it is a program top-level variable,
        // it has been resolved by a separate method.
        if (defn.SemanticShadowFrame != null)
        {
            return;
        }

        var isClassProp = this.m_Frame is ClassFrame && !defn.Modifiers.HasFlag(Ast.AnnotatableDefinitionModifier.Static);
        if (defn.Decorators != null && isClassProp)
        {
            var decoratorFnType = this.m_ModelCore.Factory.FunctionType(new NameAndTypePair[]{new NameAndTypePair("obj", this.m_Frame.TypeFromFrame), new NameAndTypePair("binding", this.m_ModelCore.BindingType)}, null, null, this.m_ModelCore.UndefinedType);
            foreach (var decorator in defn.Decorators)
            {
                this.LimitExpType(decorator, decoratorFnType);
            }
        }

        foreach (var binding in defn.Bindings)
        {
            this.Fragmented_VerifyVariableBinding7(binding);
        }
    }

    private void Fragmented_VerifyVariableBinding7(Ast.VariableBinding binding)
    {
        Symbol init = null;
        if (binding.Init != null)
        {
            var type = binding.Pattern.SemanticProperty?.StaticType;
            init = type == null ? this.VerifyExpAsValue(binding.Init) : this.LimitExpType(binding.Init, type);
        }
        // if initialiser is a constant value,
        // use it as initial value.
        if (init is ConstantValue && binding.Pattern.SemanticProperty != null && init.StaticType == binding.Pattern.SemanticProperty.StaticType)
        {
            binding.Pattern.SemanticProperty.InitValue = init;
        }
        this.Fragmented_VerifyDestructuringPattern7(binding.Pattern, init?.StaticType);
    }

    private void Fragmented_VerifyDestructuringPattern7(Ast.DestructuringPattern pattern, Symbol initType)
    {
        if (pattern is Ast.NondestructuringPattern nondestructuring)
        {
            this.Fragmented_VerifyNondestructuringPattern7(nondestructuring, initType);
        }
        else if (pattern is Ast.RecordDestructuringPattern recordDestructuring)
        {
            this.Fragmented_VerifyRecordDestructuringPattern7(recordDestructuring, initType);
        }
        else
        {
            this.Fragmented_VerifyArrayDestructuringPattern7((Ast.ArrayDestructuringPattern) pattern, initType);
        }
    }
}