// UNO PREAMBLE START

// Note that this license is inherited from the original Hunspell project
// 	as this is derived from that work. Original license:
//
// Version: MPL 1.1/GPL 2.0/LGPL 2.1
//
// The contents of this file are subject to the Mozilla Public License Version
// 1.1 (the "License"); you may not use this file except in compliance with
// the License. You may obtain a copy of the License at
// http://www.mozilla.org/MPL/
//
// Software distributed under the License is distributed on an "AS IS" basis,
// WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
// for the specific language governing rights and limitations under the
// License.
//
// 	The Original Code is Hunspell, based on MySpell.
//
// 	The Initial Developers of the Original Code are
// Kevin Hendricks (MySpell) and NÃ©meth LÃ¡szlÃ³ (Hunspell).
// 	Portions created by the Initial Developers are Copyright (C) 2002-2005
// the Initial Developers. All Rights Reserved.
//
// 	Contributor(s):
// David Einstein 
// Davide Prina
// Giuseppe Modugno 
// Gianluca Turconi
// Simon Brouwer
// Noll JÃ¡nos
// BÃ­rÃ³ ÃrpÃ¡d
// Goldman EleonÃ³ra
// SarlÃ³s TamÃ¡s
// BencsÃ¡th BoldizsÃ¡r
// HalÃ¡csy PÃ©ter
// Dvornik LÃ¡szlÃ³
// Gefferth AndrÃ¡s
// Nagy Viktor
// Varga DÃ¡niel
// Chris Halls
// Rene Engelhard
// Bram Moolenaar
// Dafydd Jones
// Harri PitkÃ¤nen
//
// 	Alternatively, the contents of this file may be used under the terms of
// 	either the GNU General Public License Version 2 or later (the "GPL"), or
// 	the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
// 	in which case the provisions of the GPL or the LGPL are applicable instead
// of those above. If you wish to allow use of your version of this file only
// under the terms of either the GPL or the LGPL, and not to allow others to
// use your version of this file under the terms of the MPL, indicate your
// decision by deleting the provisions above and replace them with the notice
// and other provisions required by the GPL or the LGPL. If you do not delete
// the provisions above, a recipient may use your version of this file under
// the terms of any one of the MPL, the GPL or the LGPL.

// https://github.com/aarondandy/WeCantSpell.Hunspell

#pragma warning disable IDE0055, CA1805, CA1815, CA1310,

// UNO PREAMBLE END

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace WeCantSpell.Hunspell;

internal static partial class StringEx
{

#if false // This isn't used anymore but I want to keep it around as it was tricky to port

    public static bool IsReverseSubset(string s1, ReadOnlySpan<char> s2)
    {
        return s1.Length <= s2.Length && check(s1, s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool check(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
        {
            var s2LastIndex = s2.Length - 1;

            for (var i = 0; i < s1.Length; i++)
            {
                var c = s1[i];
                if (c != '.' && s2[s2LastIndex - i] != c)
                {
                    return false;
                }
            }

            return true;
        }
    }

#endif

    public static bool IsSubset(string s1, string s2)
    {
        if (s1.Length <= s2.Length)
        {
            for (var i = 0; i < s1.Length; i++)
            {
                if (s1[i] != '.' && s1[i] != s2[i])
                {
                    goto fail;
                }
            }

            return true;
        }

    fail:
        return false;
    }

    public static bool IsSubset(string s1, ReadOnlySpan<char> s2)
    {
        if (s1.Length <= s2.Length)
        {
            for (var i = 0; i < s1.Length; i++)
            {
                if (s1[i] != '.' && s1[i] != s2[i])
                {
                    goto fail;
                }
            }

            return true;
        }

    fail:
        return false;
    }

    public static bool IsNumericWord(ReadOnlySpan<char> word)
    {
        var isNum = false;
        foreach (var c in word)
        {
            switch (c)
            {
                case >= '0' and <= '9':
                    isNum = true;
                    break;

                case ',' or '.' or '-' when isNum:
                    isNum = false;
                    break;

                default:
                    return false;
            }
        }

        return isNum;
    }

    public static bool IsNumericWord(string word)
    {
        var isNum = false;
        foreach (var c in word)
        {
            switch (c)
            {
                case >= '0' and <= '9':
                    isNum = true;
                    break;

                case ',' or '.' or '-' when isNum:
                    isNum = false;
                    break;

                default:
                    return false;
            }
        }

        return isNum;
    }

#if HAS_SEARCHVALUES

    public static int CountMatchingFromLeft(ReadOnlySpan<char> text, char character)
    {
        var count = text.IndexOfAnyExcept(character);
        return count < 0 ? text.Length : count;
    }

    public static int CountMatchingFromRight(ReadOnlySpan<char> text, char character)
    {
        return text.Length - text.LastIndexOfAnyExcept(character) - 1;
    }

#else

    public static int CountMatchingFromLeft(string text, char character)
    {
        var count = 0;
        for (; count < text.Length && text[count] == character; count++) ;

        return count;
    }

    public static int CountMatchingFromLeft(ReadOnlySpan<char> text, char character)
    {
        var count = 0;
        for (; count < text.Length && text[count] == character; count++) ;

        return count;
    }

    public static int CountMatchingFromRight(string text, char character)
    {
        var searchIndex = text.Length - 1;
        for (; searchIndex >= 0 && text[searchIndex] == character; searchIndex--) ;

        return text.Length - searchIndex - 1;
    }

    public static int CountMatchingFromRight(ReadOnlySpan<char> text, char character)
    {
        var searchIndex = text.Length - 1;
        for (; searchIndex >= 0 && text[searchIndex] == character; searchIndex--) ;

        return text.Length - searchIndex - 1;
    }

#endif

    public static int CountMatchesFromLeft(this ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        return a.Length > b.Length ? countMatches(b, a) : countMatches(a, b);

        static int countMatches(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
        {
            var count = 0;
            for (; count < a.Length && a[count] == b[count]; count++) ;
            return count;
        }
    }

    public static int CountMatchesFromRight(this ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        return a.Length > b.Length ? countMatches(b, a) : countMatches(a, b);

        static int countMatches(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
        {
            var count = 0;
            for (; count < a.Length && a[a.Length - 1 - count] == b[b.Length - 1 - count]; count++) ;
            return count;
        }
    }

    /// <summary>
    /// This is a character class function used within Hunspell to determine if a character is an ASCII letter or something else.
    /// </summary>
    /// <param name="ch">The character value to check.</param>
    /// <returns><c>true</c> is a given character is an ASCII letter or something else.</returns>
    public static bool MyIsAlpha(char ch) => ch is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or >= (char)128;

    public static string MakeInitCap(string s, TextInfo textInfo)
    {
        if (s.Length > 0)
        {
            var actualFirstLetter = s[0];
            var expectedFirstLetter = textInfo.ToUpper(actualFirstLetter);
            if (expectedFirstLetter != actualFirstLetter)
            {
                s = ConcatString(expectedFirstLetter, s.AsSpan(1));
            }
        }

        return s;
    }

    /// <summary>
    /// Convert to all little.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string MakeAllSmall(string s, TextInfo textInfo) => textInfo.ToLower(s);

    public static string MakeInitSmall(string s, TextInfo textInfo)
    {
        if (s.Length > 0)
        {
            var actualFirstLetter = s[0];
            var expectedFirstLetter = textInfo.ToLower(actualFirstLetter);
            if (expectedFirstLetter != actualFirstLetter)
            {
                s = ConcatString(expectedFirstLetter, s.AsSpan(1));
            }
        }

        return s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string MakeAllCap(string s, TextInfo textInfo) => textInfo.ToUpper(s);

    public static string MakeTitleCase(string s, CultureInfo cultureInfo)
    {
        if (s.Length > 0)
        {
            var builder = new StringBuilderSpan(s.Length);
            builder.Append(cultureInfo.TextInfo.ToUpper(s[0]));
            builder.AppendLower(s.AsSpan(1), cultureInfo);
            s = builder.GetStringAndDispose();
        }

        return s;
    }

    public static CapitalizationType GetCapitalizationType(ReadOnlySpan<char> word, TextInfo textInfo)
    {
        if (word.IsEmpty)
        {
            return CapitalizationType.None;
        }

        var hasFoundMoreCaps = false;
        var firstIsUpper = false;
        var hasLower = false;

        var c = word[0];

        if (char.IsUpper(c))
        {
            firstIsUpper = true;
        }
        else if (charIsNotNeutral(c, textInfo))
        {
            hasLower = true;
        }

        for (var i = 1; i < word.Length; i++)
        {
            c = word[i];

            if (!hasFoundMoreCaps && char.IsUpper(c))
            {
                if (hasLower)
                {
                    goto handleHuh;
                }

                hasFoundMoreCaps = true;
            }
            else if (!hasLower && charIsNotNeutral(c, textInfo))
            {
                if (hasFoundMoreCaps)
                {
                    goto handleHuh;
                }

                hasLower = true;
            }
        }

        if (hasFoundMoreCaps)
        {
            return CapitalizationType.All;
        }

        return firstIsUpper ? CapitalizationType.Init : CapitalizationType.None;

    handleHuh:
        return firstIsUpper ? CapitalizationType.HuhInit : CapitalizationType.Huh;

        static bool charIsNotNeutral(char c, TextInfo textInfo) => c < 128
            ? c is >= 'a' and <= 'z' // For ASCII, only the a-z range needs to be checked
            : (char.IsLower(c) && textInfo.ToUpper(c) != c); // Outside ASCII, use the framework combined with the uppercase thing
    }
}

