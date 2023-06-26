namespace VioletScript.Parser.Semantic.Logic;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Model;

/// <summary>
/// Logic for overriding methods.
/// It is not allowed to override a parameterized method, as a <c>CannotOverrideGenericMethodIssue</c>
/// is produced.
/// </summary>
public static class MethodOverride {
    /// <summary>
    /// Performs method overriding. Returns <c>null</c> if successful, or otherwise an issue symbol.
    /// </summary>
    public static Symbol OverrideSingle(Symbol subtype, Symbol subtypeMethod) {
        var mc = subtypeMethod.ModelCore;
        var f = mc.Factory;
        var name = subtypeMethod.Name;

        var superType = subtype.SuperType;
        if (superType == null) {
            return f.MustOverrideAMethodIssue(name);
        }
        var superTypeMethod = superType.ModelCore.Factory.Value(superType).ResolveProperty(name);
        if (superTypeMethod == null || !(superTypeMethod is ReferenceValue)) {
            return f.MustOverrideAMethodIssue(name);
        }
        superTypeMethod = superTypeMethod.Property;

        if (subtypeMethod.BelongsToVirtualProperty != null) {
            bool isGetter = subtypeMethod == subtypeMethod.BelongsToVirtualProperty.Getter;
            if (isGetter) {
                // overriding a getter
                if (!(superTypeMethod is VirtualSlot) || superTypeMethod.Getter != null) {
                    return f.MustOverrideAMethodIssue(name);
                }
                superTypeMethod = superTypeMethod.Getter;
            } else {
                // overriding a setter
                if (!(superTypeMethod is VirtualSlot) || superTypeMethod.Setter != null) {
                    return f.MustOverrideAMethodIssue(name);
                }
                superTypeMethod = superTypeMethod.Setter;
            }
        } else {
            // overriding a regular method
            if (!(superTypeMethod is MethodSlot)) {
                return f.MustOverrideAMethodIssue(name);
            }
        }

        if (superTypeMethod.TypeParameters != null) {
            return f.CannotOverrideGenericMethodIssue(name);
        }

        if (!IsOverrideSignatureCompatible(superTypeMethod.StaticType, subtypeMethod.StaticType)) {
            return f.IncompatibleOverrideSignatureIssue(name, superTypeMethod.StaticType);
        }
        if (superTypeMethod.MethodFlags.HasFlag(MethodSlotFlags.Final)) {
            return f.CannotOverrideFinalMethodIssue(name);
        }
        superTypeMethod.AddMethodOverrider(subtypeMethod);
        return null;
    }

    private static bool IsOverrideSignatureCompatible(Symbol superTypeSignature, Symbol subtypeSignature) {
        if (superTypeSignature.FunctionHasRequiredParameters) {
            if (!subtypeSignature.FunctionHasRequiredParameters || superTypeSignature.FunctionCountOfRequiredParameters != subtypeSignature.FunctionCountOfRequiredParameters) {
                return false;
            }
            int l = superTypeSignature.FunctionCountOfRequiredParameters;
            NameAndTypePair[] params1 = superTypeSignature.FunctionRequiredParameters;
            NameAndTypePair[] params2 = subtypeSignature.FunctionRequiredParameters;
            for (int i = 0; i < l; ++i) {
                if (params1[i].Type != params2[i].Type) {
                    return false;
                }
            }
        } else if (subtypeSignature.FunctionHasRequiredParameters) {
            return false;
        }

        if (superTypeSignature.FunctionHasOptParameters) {
            if (!subtypeSignature.FunctionHasOptParameters || superTypeSignature.FunctionCountOfOptParameters > subtypeSignature.FunctionCountOfOptParameters) {
                return false;
            }
            int l = superTypeSignature.FunctionCountOfOptParameters;
            NameAndTypePair[] params1 = superTypeSignature.FunctionOptParameters;
            NameAndTypePair[] params2 = subtypeSignature.FunctionOptParameters;
            for (int i = 0; i < l; ++i) {
                if (params1[i].Type != params2[i].Type) {
                    return false;
                }
            }
        } else if (subtypeSignature.FunctionHasOptParameters && superTypeSignature.FunctionRestParameter != null) {
            return false;
        }

        if (superTypeSignature.FunctionRestParameter != null) {
            if (subtypeSignature.FunctionRestParameter == null || superTypeSignature.FunctionRestParameter.Value.Type != subtypeSignature.FunctionRestParameter.Value.Type) {
                return false;
            }
        } else if (subtypeSignature.FunctionRestParameter != null) {
            return false;
        }

        return superTypeSignature.FunctionReturnType == subtypeSignature.FunctionReturnType
            || subtypeSignature.FunctionReturnType.IsSubtypeOf(superTypeSignature.FunctionReturnType)
            || subtypeSignature.FunctionReturnType is AnyType;
    }
}