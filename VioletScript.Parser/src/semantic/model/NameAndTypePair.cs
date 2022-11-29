namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;

public record struct NameAndTypePair(string Name, Symbol Type) {
    public NameAndTypePair ReplaceTypes(Symbol[] typeParameters, Symbol[] argumentsList) {
        return TypeReplacement.ReplaceInNameAndTypePair(this, typeParameters, argumentsList);
    }
}