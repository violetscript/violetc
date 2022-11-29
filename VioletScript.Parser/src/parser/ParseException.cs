namespace VioletScript.Parser.Parser;

using VioletScript.Parser.Problem;

public class ParseException : Exception {
    public Problem Problem;

    public ParseException(Problem p) {
        this.Problem = p;
    }
}