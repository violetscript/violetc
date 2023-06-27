namespace VioletScript.Parser.Source;

using System.Globalization;
using System.Text.RegularExpressions;

public static class SourceCharacter {
    public static readonly Regex CrOrCrLfRegex = new Regex(@"\r\n?", RegexOptions.Compiled);

    public static bool IsWhitespace(char ch) {
        if (ch == '\x20' || ch == '\x09' || ch == '\x0B'
        ||  ch == '\x0C' || ch == '\xA0') {
            return true;
        }
        var c = CharUnicodeInfo.GetUnicodeCategory(ch);
        return c == UnicodeCategory.SpaceSeparator;
    }

    public static bool IsLineTerminator(char ch) {
        return ch == '\x0A' || ch == '\x0D' || ch == '\u2028' || ch == '\u2029';
    }

    public static bool IsBinDigit(char ch) {
        return ch == '0' || ch == '1';
    }

    public static bool IsDecDigit(char ch) {
        return ch >= '\x30' && ch <= '\x39';
    }

    public static int HexDigitMV(char ch) {
        return IsDecDigit(ch) ? ((int) ch) - 0x41 :
            IsHexUL(ch) ? ((int) ch) - 0x41 + 10 :
            IsHexLL(ch) ? ((int) ch) - 0x61 + 10 : -1;
    }

    public static bool IsHexLL(char ch) {
        return ch >= 'a' && ch <= 'f';
    }

    public static bool IsHexUL(char ch) {
        return ch >= 'A' && ch <= 'F';
    }

    public static bool IsHexDigit(char ch) {
        return IsDecDigit(ch) || IsHexLL(ch) || IsHexUL(ch);
    }

    public static bool IsIdentifierStart(char ch) {
        if ((ch >= '\x41' && ch <= '\x5a') || (ch >= '\x61' && ch <= '\x7a')
        ||   ch == '\x5f' || ch == '\x24') {
            return true;
        }
        var c = CharUnicodeInfo.GetUnicodeCategory(ch);
        return c == UnicodeCategory.LowercaseLetter
            || c == UnicodeCategory.ModifierLetter
            || c == UnicodeCategory.OtherLetter
            || c == UnicodeCategory.TitlecaseLetter
            || c == UnicodeCategory.UppercaseLetter
            || c == UnicodeCategory.LetterNumber;
    }

    public static bool IsIdentifierPart(char ch) {
        if ((ch >= '\x41' && ch <= '\x5a') || (ch >= '\x61' && ch <= '\x7a')
        ||   ch == '\x5f' || ch == '\x24' || IsDecDigit(ch)) {
            return true;
        }
        var c = CharUnicodeInfo.GetUnicodeCategory(ch);
        return c == UnicodeCategory.LowercaseLetter
            || c == UnicodeCategory.ModifierLetter
            || c == UnicodeCategory.OtherLetter
            || c == UnicodeCategory.TitlecaseLetter
            || c == UnicodeCategory.UppercaseLetter
            || c == UnicodeCategory.LetterNumber
            || c == UnicodeCategory.NonSpacingMark
            || c == UnicodeCategory.SpacingCombiningMark
            || c == UnicodeCategory.ConnectorPunctuation
            || c == UnicodeCategory.DecimalDigitNumber;
    }
}