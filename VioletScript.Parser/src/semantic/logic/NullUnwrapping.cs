namespace VioletScript.Parser.Semantic.Logic;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Model;

public static class NullUnwrapping {
    public static Symbol Unwrap(Symbol value) {
        var type = value.StaticType;
        if (!(type is UnionType)) return value;
        var mc = type.ModelCore;
        var r = new List<Symbol>(type.UnionMemberTypes);
        var i = r.IndexOf(mc.NullType);
        if (i != -1) r.RemoveAt(i);
        i = r.IndexOf(mc.UndefinedType);
        if (i != -1) r.RemoveAt(i);
        return mc.Factory.NullUnwrappedValue(mc.Factory.UnionType(r.ToArray()));
    }
}