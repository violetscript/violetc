namespace VioletScript.Util;

public static class StringSubstr
{
    public static char CharAt(string s, int index)
    {
        return index < s.Count() ? s[index] : '\x00';
    }

    public static string Substr(string s, int index, int length)
    {
        int j = index + length;
        j = Math.Min(s.Count(), j);
        return s.Substring(index, j - index);
    }
}