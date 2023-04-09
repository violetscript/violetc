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
    private void Fragmented_VerifyEnumDefinition(Ast.EnumDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            Fragmented_VerifyEnumDefinition1(defn);
        }
        else if (defn.SemanticFrame != null)
        {
            if (phase == VerifyPhase.Phase7)
            {
                VerifyTypeDefinitionDecorators(defn);
            }
            EnterFrame(defn.SemanticFrame);
            foreach (var drtv in defn.Block.Statements)
            {
                if (!drtv.IsEnumVariantDefinition)
                {
                    Fragmented_VerifyStatement(drtv, phase);
                }
            }
            ExitFrame();
        }
    }

    private void Fragmented_VerifyEnumDefinition1(Ast.EnumDefinition defn)
    {
        Properties outputProps =
            m_Frame.NamespaceFromFrame != null ? m_Frame.NamespaceFromFrame.Properties
            : m_Frame.PackageFromFrame != null ? m_Frame.PackageFromFrame.Properties
            : m_Frame.Properties;
        Symbol type = null;

        var wrapsType = defn.NumericType != null ? VerifyTypeExp(defn.NumericType) : m_ModelCore.NumberType;
        wrapsType ??= m_ModelCore.NumberType;

        var previousDuplicate = outputProps[defn.Id.Name];
        if (previousDuplicate != null)
        {
            if (m_Options.AllowDuplicates && previousDuplicate is EnumType)
            {
                type = previousDuplicate;
            }
            else
            {
                VerifyError(null, 139, defn.Id.Span.Value, new DiagnosticArguments { ["name"] = defn.Id.Name });
            }
        }
        else
        {
            type = m_ModelCore.Factory.EnumType(defn.Id.Name, defn.IsFlags, wrapsType);
            type.ParentDefinition = m_Frame.NamespaceFromFrame ?? m_Frame.PackageFromFrame;
            type.EnumInitializeMethods();
            outputProps[defn.Id.Name] = type;
        }

        defn.SemanticType = type;

        if (type != null)
        {
            defn.SemanticFrame = m_ModelCore.Factory.EnumFrame(type);
            EnterFrame(defn.SemanticFrame);

            Fragmented_VerifyEnumDefinition1Variants(defn);

            foreach (var drtv in defn.Block.Statements)
            {
                if (!drtv.IsEnumVariantDefinition)
                {
                    Fragmented_VerifyStatement(drtv, VerifyPhase.Phase1);
                }
            }

            ExitFrame();
        }
    }

    private void Fragmented_VerifyEnumDefinition1Variants(Ast.EnumDefinition defn)
    {
        Symbol type = defn.SemanticType;
        var numType = type.NumericType;
        bool isFlags = type.IsFlagsEnum;
        object counter = type.IsFlagsEnum ? EnumConstHelpers.One(type) : EnumConstHelpers.Zero(type);
        foreach (var drtv in defn.Block.Statements.Where(d => d.IsEnumVariantDefinition))
        {
            var binding = (Ast.EnumVariantDefinition) drtv;
            var screamingSnakeCaseName = binding.Id.Name;
            var previousDuplicate = type.Properties[screamingSnakeCaseName];
            if (previousDuplicate != null)
            {
                VerifyError(null, 139, binding.Id.Span.Value, new DiagnosticArguments { ["name"] = screamingSnakeCaseName });
                continue;
            }
            object variantNumber = null;
            string variantString = "";
            if (binding.Init is Ast.ArrayInitializer arrayInitStN
                && arrayInitStN.Items.Count() == 2
                && arrayInitStN.Items[0] is Ast.StringLiteral strLit1
                && !(arrayInitStN.Items[1] is Ast.Spread))
            {
                var numSymbol = LimitConstantExpType(arrayInitStN.Items[1], numType);
                variantNumber = numSymbol?.NumericConstantValueAsObject ?? counter;
                variantString = strLit1.Value;
            }
            else if (binding.Init is Ast.ArrayInitializer arrayInitNtS
                && arrayInitNtS.Items.Count() == 2
                && !(arrayInitNtS.Items[0] is Ast.Spread)
                && arrayInitNtS.Items[1] is Ast.StringLiteral strLit2)
            {
                var numSymbol = LimitConstantExpType(arrayInitNtS.Items[0], numType);
                variantNumber = numSymbol?.NumericConstantValueAsObject ?? counter;
                variantString = strLit2.Value;
            }
            else if (binding.Init is Ast.StringLiteral strLiteral)
            {
                variantNumber = counter;
                variantString = strLiteral.Value;
            }
            else if (binding.Init != null)
            {
                var numSymbol = LimitConstantExpType(binding.Init, numType);
                variantNumber = numSymbol?.NumericConstantValueAsObject ?? counter;
                variantString = ScreamingSnakeCaseToCamelCase(screamingSnakeCaseName);
            }
            else
            {
                variantNumber = counter;
                variantString = ScreamingSnakeCaseToCamelCase(screamingSnakeCaseName);
            }

            // if it is a flags enum, ensure number is one or power of 2.
            if (isFlags && !(EnumConstHelpers.IsOne(variantNumber) || EnumConstHelpers.IsPowerOf2(variantNumber)))
            {
                VerifyError(null, 227, binding.Id.Span.Value, new DiagnosticArguments {});
            }

            // check for duplicate variant number/string
            if (type.EnumHasVariantByNumber(variantNumber))
            {
                VerifyError(null, 228, binding.Id.Span.Value, new DiagnosticArguments {});
            }
            if (type.EnumHasVariantByString(variantString))
            {
                VerifyError(null, 229, binding.Id.Span.Value, new DiagnosticArguments {});
            }

            type.EnumSetVariant(variantString, variantNumber);
            var variantVar = m_ModelCore.Factory.VariableSlot(screamingSnakeCaseName, true, type);
            variantVar.InitValue = m_ModelCore.Factory.EnumConstantValue(variantNumber, type);
            variantVar.ParentDefinition = type;
            variantVar.Visibility = Visibility.Public;
            type.Properties[screamingSnakeCaseName] = variantVar;

            binding.SemanticString = variantString;
            binding.SemanticValue = variantNumber;

            counter = variantNumber;
            counter = isFlags ? EnumConstHelpers.MultiplyPer2(type, counter) : EnumConstHelpers.Increment(type, counter);
        }
    }

    private static string ScreamingSnakeCaseToCamelCase(string name)
    {
        var str = string.Join("", name.Split("_").Select(s =>
        {
            if (s.Count() == 0)
            {
                return "";
            }
            if (s.Count() == 1)
            {
                return s.Substring(0, 1).ToUpper();
            }
            return s.Substring(0, 1).ToUpper() + s.Substring(1).ToLower();
        }).ToArray());
        if (str.Count() == 0)
        {
            return "";
        }
        if (str.Count() == 1)
        {
            return str.ToLower();
        }
        return str.Substring(0, 1).ToLower() + str.Substring(1);
    }
}