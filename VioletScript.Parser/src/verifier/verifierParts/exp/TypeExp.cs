namespace VioletScript.Parser.Verifier;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Problem;
using VioletScript.Parser.Semantic.Logic;
using VioletScript.Parser.Semantic.Model;
using VioletScript.Parser.Source;
using Ast = VioletScript.Parser.Ast;

using ProblemVars = Dictionary<string, object>;

public partial class Verifier {
    public Symbol VerifyTypeExp(Ast.TypeExpression exp, bool isBase = false) {
        if (exp is Ast.IdentifierTypeExpression id) {
            var r = m_Frame.ResolveProperty(id.Name);
            if (r == null) {
                VerifyError(exp.Span.Value.Script, 128, exp.Span.Value, new ProblemVars { ["name"] = id.Name });
                return m_ModelCore.Factory.Value(m_ModelCore.AnyType);
            } else if (r is AmbiguousReferenceIssue) {
                ...
                return m_ModelCore.Factory.Value(m_ModelCore.AnyType);
            } else {
                if (!r.PropertyIsVisibleTo(m_Frame)) {
                    ...
                }
                r = r is Alias ? r.AliasToSymbol : r;
                if (!isBase && !(r is Type)) {
                    ...
                    return m_ModelCore.Factory.Value(m_ModelCore.AnyType);
                }
                // generic type
                if (!isBase && r.TypeParameters != null) {
                    ...
                    return m_ModelCore.Factory.Value(m_ModelCore.AnyType);
                }
                return r;
            }
        }
        //
        throw new Exception("Uncovered type expression");
    }
}