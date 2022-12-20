namespace VioletScript.Parser.Semantic.Logic;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Model;

/// <summary>
/// Logic for implementing interfaces.
/// It is possible to implement generic methods.
/// </summary>
public static class InterfaceImpl {
    public delegate void HandleMissingRequirement(string name, RequirementKind kind, Symbol signature);
    public delegate void HandleWrongRequirementSignature(string name, Symbol signature);

    public enum RequirementKind {
        RegularMethod,
        Getter,
        Setter,
    }

    public static void VerifyImpl(
        Symbol implementor, Symbol itrfc,
        HandleMissingRequirement handleMissingRequirement,
        HandleWrongRequirementSignature handleWrongRequirementSignature
    )
    {
        foreach (var (name, property, _) in InstancePropertiesHierarchy.Iterate(itrfc)) {
            //
        }
    }
}