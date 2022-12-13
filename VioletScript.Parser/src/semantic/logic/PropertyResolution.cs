namespace VioletScript.Parser.Semantic.Logic;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Model;

public static class PropertyResolution {
    public static Symbol Resolve(Symbol @base, string name) {
        var mc = @base.ModelCore;
        var f = mc.Factory;
        if (@base is Frame) {
            //
        } else if (@base is Type) {
            foreach (var (name2, prop, definedByType) in StaticPropertiesHierarchy.Iterate(@base)) {
                if (name == name2) {
                    return f.ReferenceValueFromType(@base, prop, definedByType);
                }
            }
        } else if (@base is Value) {
            foreach (var (name2, prop, definedByType) in InstancePropertiesHierarchy.Iterate(@base.StaticType)) {
                if (name == name2) {
                    return f.ReferenceValue(@base, prop, definedByType);
                }
            }
        } else if (@base is Namespace) {
            var r = @base.Properties[name];
            if (r != null) return f.ReferenceValueFromNamespace(@base, r);
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