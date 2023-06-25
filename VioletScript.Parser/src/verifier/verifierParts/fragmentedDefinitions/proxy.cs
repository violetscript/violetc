namespace VioletScript.Parser.Verifier;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Diagnostic;
using VioletScript.Parser.Semantic.Logic;
using VioletScript.Parser.Semantic.Model;
using VioletScript.Parser.Source;
using Ast = VioletScript.Parser.Ast;

using DiagnosticArguments = Dictionary<string, object>;

public partial class Verifier
{
    private void Fragmented_VerifyProxyDefinition(Ast.ProxyDefinition defn, VerifyPhase phase)
    {
        if (phase == VerifyPhase.Phase1)
        {
            Fragmented_VerifyProxyDefinition1(defn);
        }
        else if (phase == VerifyPhase.Phase2)
        {
            Fragmented_VerifyProxyDefinition2(defn);
        }
        else if (phase == VerifyPhase.Phase3)
        {
            Fragmented_VerifyProxyDefinition3(defn);
        }
        // VerifyPhase.Phase7
        else if (phase == VerifyPhase.Phase7)
        {
            Fragmented_VerifyProxyDefinition7(defn);
        }
    }

    private void Fragmented_VerifyProxyDefinition1(Ast.ProxyDefinition defn)
    {
        var type = m_Frame.TypeFromFrame;
        // do not allow duplicate proxy
        toDo();
    }

    private void Fragmented_VerifyProxyDefinition2(Ast.ProxyDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }
        var type = m_Frame.TypeFromFrame;
        toDo();
    }

    private void Fragmented_VerifyProxyDefinition3(Ast.ProxyDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }
        var type = m_Frame.TypeFromFrame;
        toDo();
    }

    private void Fragmented_VerifyProxyDefinition7(Ast.ProxyDefinition defn)
    {
        var method = defn.SemanticMethodSlot;
        if (method == null)
        {
            return;
        }
        var type = m_Frame.TypeFromFrame;
        toDo();
    }
}