namespace VioletScript.Parser.Semantic.Model;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Logic;

public sealed class Properties : IEnumerable<(string Name, Symbol Symbol)> {
    private OrderedDictionary m_Dict = new OrderedDictionary();

    public Properties() {
    }

    public Symbol this[String name] {
        get => Get(name);
        set => Set(name, value);
    }

    public Symbol Get(String name) {
        return m_Dict.Contains(name) ? ((Symbol) m_Dict[name]) : null;
    }

    public bool Has(String name) {
        return m_Dict.Contains(name);
    }

    public void Set(String name, Symbol symbol) {
        m_Dict[name] = symbol;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return new Enumerator(this);
    }

    public IEnumerator<(string Name, Symbol Symbol)> GetEnumerator() {
        return new Enumerator(this);
    }

    internal class Enumerator : IEnumerator<(string Name, Symbol Symbol)> {
        private IDictionaryEnumerator m_E;

        public Enumerator(Properties p) {
            m_E = p.m_Dict.GetEnumerator();
        }

        public void Dispose() {
        }

        object IEnumerator.Current {
            get => Current;
        }

        public (string Name, Symbol Symbol) Current {
            get {
                var de = (DictionaryEntry) m_E.Current;
                return ((string) de.Key, (Symbol) de.Value);
            }
        }

        public bool MoveNext() {
            return m_E.MoveNext();
        }

        public void Reset() {
            m_E.Reset();
        }
    }
}

/// <summary>
/// Provides iterator for iterating instance properties
/// in subtype descending order.
/// </summary>
public static class InstancePropertiesHierarchy {
    public static IEnumerable<(string Name, Symbol Symbol, Symbol DefinedByType)> Iterate(Symbol type) {
        var types = new List<Symbol>{};
        while (type != null) {
            if (!types.Contains(type)) {
                types.Add(type);
            }
            foreach (var type2 in type.SuperTypes) {
                if (!types.Contains(type2)) {
                    types.Add(type2);
                }
            }
            type = type.SuperType;
        }
        foreach (var type3 in types) {
            if (type3.Delegate == null) {
                continue;
            }
            foreach (var (name4, type4) in type3.Delegate.Properties) {
                yield return (name4, type4, type3);
            }
        }
    }
}

public static class SingleInheritanceInstancePropertiesHierarchy {
    public static IEnumerable<(string Name, Symbol Symbol, Symbol DefinedByType)> Iterate(Symbol type) {
        var types = new List<Symbol>{};
        while (type != null) {
            types.Add(type);
            type = type.SuperType;
        }
        foreach (var type2 in types) {
            if (type2.Delegate == null) {
                continue;
            }
            foreach (var (name3, type3) in type2.Delegate.Properties) {
                yield return (name3, type3, type2);
            }
        }
    }

    public static bool HasProperty(Symbol type, string name)
    {
        return SingleInheritanceInstancePropertiesHierarchy.Iterate(type).Any(prop => prop.Name == name);
    }
}

/// <summary>
/// Iteration for inherited interfaces.
/// </summary>
public static class InterfaceInheritanceInstancePropertiesHierarchy {
    public static IEnumerable<(string Name, Symbol Symbol, Symbol DefinedByType)> Iterate(Symbol type) {
        if (!type.IsInterfaceType) {
            yield break;
        }
        foreach (var type2 in type.SuperTypes) {
            if (type2.Delegate == null) {
                continue;
            }
            foreach (var (name3, type3) in type2.Delegate.Properties) {
                yield return (name3, type3, type2);
            }
        }
    }

    public static bool HasProperty(Symbol type, string name)
    {
        return InterfaceInheritanceInstancePropertiesHierarchy.Iterate(type).Any(prop => prop.Name == name);
    }
}

/// <summary>
/// Provides iterator for static properties from a class.
/// </summary>
public static class StaticPropertiesHierarchy {
    public static IEnumerable<(string Name, Symbol Symbol, Symbol DefinedByType)> Iterate(Symbol type) {
        if (type.Properties == null) {
            yield break;
        }
        while (type != null) {
            foreach (var (name, symbol) in type.Properties) {
                yield return (name, symbol, type);
            }
            type = type.SuperType;
        }
    }
}