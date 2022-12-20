namespace VioletScript.Parser.Semantic.Logic;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Model;

/// <summary>
/// Logic for implementing interfaces.
/// It is possible to implement generic methods.
/// </summary>
public static class InterfaceImpl {
    public delegate void HandleMissingMethod(string name);
    public delegate void HandleMissingGetter(string name);
    public delegate void HandleMissingSetter(string name);
    public delegate void HandleRequirementMustBeMethod(string name);
    public delegate void HandleRequirementMustBeVirtualProperty(string name);
    public delegate void HandleWrongMethodSignature(string name, Symbol expectedSignature);
    public delegate void HandleWrongGetterSignature(string name, Symbol expectedSignature);
    public delegate void HandleWrongSetterSignature(string name, Symbol expectedSignature);
    public delegate void HandleRequirementGenericsDontMatch(string name);

    public static void VerifyImpl(
        Symbol implementor, Symbol itrfc,
        HandleMissingMethod handleMissingMethod,
        HandleMissingGetter handleMissingGetter,
        HandleMissingSetter handleMissingSetter,
        HandleRequirementMustBeMethod handleRequirementMustBeMethod,
        HandleRequirementMustBeVirtualProperty handleRequirementMustBeVirtualProperty,
        HandleWrongMethodSignature handleWrongMethodSignature,
        HandleWrongGetterSignature handleWrongGetterSignature,
        HandleWrongSetterSignature handleWrongSetterSignature,
        HandleRequirementGenericsDontMatch handleRequirementGenericsDontMatch
    )
    {
        foreach (var (name, reqProp, _) in InstancePropertiesHierarchy.Iterate(itrfc)) {
            var implProp = implementor.Delegate.Properties[name];
            if (implProp == null) {
                if (reqProp is VirtualSlot) {
                    if (reqProp.Getter != null && (reqProp.Getter.MethodFlags & MethodSlotFlags.OptionalInterfaceMethod) == 0) {
                        handleMissingGetter(name);
                    }
                    if (reqProp.Setter != null && (reqProp.Setter.MethodFlags & MethodSlotFlags.OptionalInterfaceMethod) == 0) {
                        handleMissingSetter(name);
                    }
                } else if ((reqProp.MethodFlags & MethodSlotFlags.OptionalInterfaceMethod) == 0) {
                    handleMissingMethod(name);
                }
            } else if (reqProp is VirtualSlot) {
                if (!(implProp is VirtualSlot)) {
                    handleRequirementMustBeVirtualProperty(name);
                    return;
                }

                // getter
                if (implProp.Getter == null) {
                    if (reqProp.Getter != null && (reqProp.Getter.MethodFlags & MethodSlotFlags.OptionalInterfaceMethod) == 0) {
                        handleMissingGetter(name);
                    }
                } else if (reqProp.Getter != null && !reqProp.Getter.StaticType.TypeStructurallyEquals(implProp.Getter.StaticType)) {
                    handleWrongGetterSignature(name, reqProp.Getter.StaticType);
                }

                // setter
                if (implProp.Setter == null) {
                    if (reqProp.Setter != null && (reqProp.Setter.MethodFlags & MethodSlotFlags.OptionalInterfaceMethod) == 0) {
                        handleMissingSetter(name);
                    }
                } else if (reqProp.Setter != null && !reqProp.Setter.StaticType.TypeStructurallyEquals(implProp.Setter.StaticType)) {
                    handleWrongSetterSignature(name, reqProp.Setter.StaticType);
                }
            } else {
                // regular method
                if (!(implProp is MethodSlot)) {
                    handleRequirementMustBeMethod(name);
                    return;
                }
                // don't forget generics
                ...
            }
        }
    }
}