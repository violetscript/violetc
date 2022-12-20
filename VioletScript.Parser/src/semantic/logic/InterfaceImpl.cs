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
                if (reqProp.TypeParameters != null) {
                    // implement type-parameterized method
                    if (implProp.TypeParameters != null || !TypeParameterSequenceEquals(reqProp.TypeParameters, implProp.TypeParameters)) {
                        handleRequirementGenericsDontMatch(name);
                        return;
                    }
                    var reqSubProp = reqProp.StaticType.ReplaceTypes(reqProp.TypeParameters, implProp.TypeParameters);
                    if (!reqSubProp.StaticType.TypeStructurallyEquals(implProp.StaticType)) {
                        handleWrongMethodSignature(name, reqSubProp.StaticType);
                    }
                } else if (implProp.TypeParameters != null) {
                    handleRequirementGenericsDontMatch(name);
                } else {
                    if (!reqProp.StaticType.TypeStructurallyEquals(implProp.StaticType)) {
                        handleWrongMethodSignature(name, reqProp.StaticType);
                    }
                }
            }
        }
    }

    private static bool TypeParameterSequenceEquals(Symbol[] a, Symbol[] b) {
        var c = a.Count();
        if (b.Count() != c) {
            return false;
        }
        for (int i = 0; i != c; ++i) {
            var pa = a[i];
            var pb = b[i];
            var itrfcsA = pa.ImplementsInterfaces;
            var itrfcsB = pb.ImplementsInterfaces;
            var itrfcsCount = itrfcsA.Count();
            if (itrfcsCount != itrfcsB.Count()) {
                return false;
            }
            for (int j = 0; j != itrfcsCount; ++j) {
                if (!itrfcsA[j].TypeStructurallyEquals(itrfcsB[j])) {
                    return false;
                }
            }
            if (pa.SuperType != null) {
                if (pb.SuperType == null || !pa.SuperType.TypeStructurallyEquals(pb.SuperType)) {
                    return false;
                }
            } else if (pb.SuperType != null) {
                return false;
            }
        }
        return true;
    }
}