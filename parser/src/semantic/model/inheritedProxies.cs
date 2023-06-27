namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public static class InheritedProxies {
    public static Symbol Find(Symbol type, Operator op) {
        while (type != null) {
            if (type.Delegate != null && type.Delegate.Proxies.ContainsKey(op)) {
                var proxy = type.Delegate.Proxies[op];
                if (proxy.StaticType != null) {
                    return proxy;
                }
            }
            type = type.SuperType;
        }
        return null;
    }

    public static Symbol FindImplicitConversion(Symbol type, Symbol convertsTo) {
        if (convertsTo.Delegate != null) {
            var c = convertsTo.Delegate.ImplicitConversionProxies.ContainsKey(type) ? convertsTo.Delegate.ImplicitConversionProxies[type] : null;
            if (c != null && c.StaticType != null) {
                return c;
            }
        }
        return null;
    }

    public static Symbol FindExplicitConversion(Symbol type, Symbol convertsTo) {
        if (convertsTo.Delegate != null) {
            var c = convertsTo.Delegate.ExplicitConversionProxies.ContainsKey(type) ? convertsTo.Delegate.ExplicitConversionProxies[type] : null;
            if (c != null && c.StaticType != null) {
                return c;
            }
        }
        return null;
    }
}