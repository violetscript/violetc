namespace VioletScript.Parser.Source;

using System.Collections.Generic;

public static class ReservedWords {
    private static Dictionary<int, List<string>> m_ByLength = new Dictionary<int, List<string>> {
        [2] = new List<string> {"as", "do", "if", "in", "is",},
        [3] = new List<string> {"for", "new", "try", "use", "var",},
        [4] = new List<string> {"case", "else", "null", "this", "true", "void", "with",},
        [5] = new List<string> {"await", "break", "catch", "class", "const", "false", "super", "throw", "where", "while", "yield",},
        [6] = new List<string> {"delete", "import", "public", "return", "switch", "throws", "typeof",},
        [7] = new List<string> {"default", "extends", "finally", "package", "private",},
        [8] = new List<string> {"continue", "function", "internal",},
        [9] = new List<string> {"interface", "protected",},
        [10] = new List<string> {"implements",},
    };

    public static bool IsReservedWord(string id) {
        if (!m_ByLength.ContainsKey(id.Count())) {
            return false;
        }
        var list = m_ByLength[id.Count()];
        return list.Contains(id);
    }
}