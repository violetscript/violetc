namespace VioletScript.Parser.Semantic.Logic;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Model;

public static class PropertyResolution {
    public static Symbol Resolve(Symbol @base, string name) {
        var mc = @base.ModelCore;
        var f = mc.Factory;
        if (@base is Frame) {
            Symbol r = @base.Properties[name];
            if (r != null) {
                var parentOrBaseFrame = r.ParentDefinition ?? @base;
                if (parentOrBaseFrame is Type) {
                    r = f.ReferenceValueFromType(parentOrBaseFrame, r, parentOrBaseFrame);
                } else if (parentOrBaseFrame is Namespace) {
                    r = f.ReferenceValueFromNamespace(parentOrBaseFrame, r);
                } else if (r is ReferenceValueFromNamespace || r is ReferenceValueFromType) {
                    // here, 'r' is an import alias to a property from a namespace or type.
                } else {
                    r = f.ReferenceValueFromFrame(parentOrBaseFrame, r);
                }
            }
            Symbol r2 = null;
            if (r == null && @base is ActivationFrame && @base.ActivationThisOrThisAsStaticType != null) {
                r = PropertyResolution.Resolve(@base.ActivationThisOrThisAsStaticType, name);
                if (r != null) return r;
            }
            if (@base is ClassFrame || @base is EnumFrame || @base is InterfaceFrame) {
                r2 = PropertyResolution.Resolve(@base.TypeFromFrame, name);
                if (r2 != null) {
                    if (r != null) return f.AmbiguousReferenceIssue(name);
                    r = r2;
                }
            }
            if (@base is NamespaceFrame) {
                r2 = PropertyResolution.Resolve(@base.NamespaceFromFrame, name);
                if (r2 != null) {
                    if (r != null) return f.AmbiguousReferenceIssue(name);
                    r = r2;
                }
            }
            if (@base is PackageFrame) {
                r2 = @base.PackageFromFrame.Properties[name];
                if (r2 != null) {
                    if (r != null) return f.AmbiguousReferenceIssue(name);
                    r = f.ReferenceValueFromNamespace(@base.PackageFromFrame, r2);
                }
            }
            foreach (var openNS in @base.OpenNamespaces) {
                r2 = openNS.Properties[name];
                if (r2 != null) {
                    if (r != null) return f.AmbiguousReferenceIssue(name);
                    r = f.ReferenceValueFromNamespace(openNS, r2);
                }
            }
            if (r == null) {
                r = @base.ParentFrame != null ? PropertyResolution.Resolve(@base.ParentFrame, name) : null;
            }
            if (r == null) {
                r = mc.GlobalPackage.GetSubpackage(name);
            }
            return r;
        } else if (@base is Type) {
            foreach (var (name2, prop, definedByType) in StaticPropertiesHierarchy.Iterate(@base)) {
                if (name == name2) {
                    return f.ReferenceValueFromType(@base, prop, definedByType);
                }
            }
            return null;
        } else if (@base is Value) {
            foreach (var (name2, prop, definedByType) in InstancePropertiesHierarchy.Iterate(@base.StaticType)) {
                if (name == name2) {
                    return f.ReferenceValue(@base, prop, definedByType);
                }
            }
            return null;
        } else if (@base is Namespace) {
            var r = @base.Properties[name];
            if (r != null) return f.ReferenceValueFromNamespace(@base, r);
            if (@base is Package) {
                r = @base.GetSubpackage(name);
                if (r != null) return r;
            }
            return null;
        } else if (@base is NamespaceSet) {
            foreach (var ns in @base.NamespaceSetItems) {
                var r = ns.Properties[name];
                if (r != null) return f.ReferenceValueFromNamespace(ns, r);
            }
            return null;
        } else {
            return null;
        }
    }
}