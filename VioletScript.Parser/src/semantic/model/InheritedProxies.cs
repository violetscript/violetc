namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public static class InheritedProxies {
    public static Symbol Find(Symbol type, Operator op) {
        while (type != null) {
            if (type.Delegate != null && type.Delegate.Proxies.ContainsKey(op)) {
                return type.Delegate.Proxies[op];
            }
            type = type.SuperType;
        }
        return null;
    }

    public static Symbol FindImplicitConversion(Symbol type, Symbol convertsTo) {
        while (type != null) {
            if (type.Delegate != null) {
                var c = type.Delegate.ImplicitConversionProxies.ContainsKey(convertsTo) ? type.Delegate.ImplicitConversionProxies[convertsTo] : null;
                if (c != null) {
                    return c;
                }
            }
            type = type.SuperType;
        }
        return null;
    }

    public static Symbol FindExplicitConversion(Symbol type, Symbol convertsTo) {
        while (type != null) {
            if (type.Delegate != null) {
                var c = type.Delegate.ExplicitConversionProxies.ContainsKey(convertsTo) ? type.Delegate.ExplicitConversionProxies[convertsTo] : null;
                if (c != null) {
                    return c;
                }
            }
            type = type.SuperType;
        }
        return null;
    }
}