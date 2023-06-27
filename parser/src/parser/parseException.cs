namespace VioletScript.Parser.Parser;

using VioletScript.Parser.Diagnostic;

public class ParseException : Exception {
    public Diagnostic Diagnostic;

    public ParseException(Diagnostic p) {
        this.Diagnostic = p;
    }
}