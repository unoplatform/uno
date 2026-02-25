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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WeCantSpell.Hunspell;

internal static partial class StringEx
{
#if HAS_SEARCHVALUES
    private static readonly System.Buffers.SearchValues<char> TabOrSpace = System.Buffers.SearchValues.Create("\t ");
#endif

#if NO_STATIC_STRINGCHAR_METHODS
    public static bool StartsWith(this string @this, char character) => @this.Length != 0 && @this[0] == character;

    public static bool EndsWith(this string @this, char character) => @this.Length > 0 && @this[@this.Length - 1] == character;
#endif

    public static bool StartsWithOrdinal(this ReadOnlySpan<char> @this, string value) => @this.StartsWith(value.AsSpan());

    public static bool StartsWith(this ReadOnlySpan<char> @this, char value) => @this.Length > 0 && @this[0] == value;

    public static bool EndsWith(this ReadOnlySpan<char> @this, char value) => @this.Length > 0 && @this[@this.Length - 1] == value;

    public static bool EndsWith(this ReadOnlyMemory<char> @this, char value) => @this.Length > 0 && @this.Span[@this.Length - 1] == value;

    public static bool EqualsOrdinal(this ReadOnlySpan<char> @this, char value) => @this.Length == 1 && @this[0] == value;

    public static bool EqualsOrdinal(this string @this, char value) => @this.Length == 1 && @this[0] == value;

    public static bool IsTabOrSpace(this char c) => c is '\t' or ' ';

#if HAS_SEARCHVALUES
    public static int IndexOfTabOrSpace(this ReadOnlySpan<char> span) => span.IndexOfAny(TabOrSpace);
#else
    public static int IndexOfTabOrSpace(this ReadOnlySpan<char> span) => span.IndexOfAny('\t', ' ');
#endif

#if NO_STRING_CONTAINS

    public static bool Contains(this string @this, char value) => @this.IndexOf(value) >= 0;

    public static bool Contains(this ReadOnlySpan<char> @this, char value) => @this.IndexOf(value) >= 0;

    public static bool Contains(this Span<char> @this, char value) => @this.IndexOf(value) >= 0;

#endif

#if NO_SPAN_CONTAINSANY

    public static bool ContainsAny(this ReadOnlySpan<char> @this, char value0, char value1) => @this.IndexOfAny(value0, value1) >= 0;

#endif

    public static bool Contains(this List<string> list, ReadOnlySpan<char> value)
    {
        foreach (var item in list)
        {
            if (item is not null && value.SequenceEqual(item))
            {
                return true;
            }
        }

        return false;
    }

    public static ReadOnlySpan<char> AsSpanRemoveFromEnd(this string @this, int toRemove) => @this.AsSpan(0, @this.Length - toRemove);

    public static string ReplaceIntoString(this ReadOnlySpan<char> @this, int index, int removeCount, string replacement)
    {
        if (index == 0)
        {
            return removeCount >= @this.Length
                ? replacement
                : ConcatString(replacement, @this.Slice(removeCount));
        }

        return ConcatString(@this.Slice(0, index), replacement, @this.Slice(index + removeCount));
    }

    public static string ReplaceIntoString(this ReadOnlySpan<char> text, char oldChar, char newChar)
    {
        if (text.Length > 0)
        {
            var replacementStartIndex = text.IndexOf(oldChar);
            if (replacementStartIndex < 0)
            {
                return text.ToString();
            }

            if (replacementStartIndex == text.Length - 1)
            {
                return ConcatString(text.Slice(0, text.Length - 1), newChar);
            }

            return buildReplaced(replacementStartIndex, text, oldChar, newChar);
        }

        return string.Empty;

        static string buildReplaced(int startIndex, ReadOnlySpan<char> text, char oldChar, char newChar)
        {
            var builder = new StringBuilderSpan(text);
            builder[startIndex] = newChar;
            builder.Replace(oldChar, newChar, startIndex + 1);
            return builder.GetStringAndDispose();
        }
    }

    public static string ReplaceIntoString(this ReadOnlySpan<char> text, string oldText, string newText)
    {
        if (text.IsEmpty)
        {
            return string.Empty;
        }

        var replacementStartIndex = text.IndexOf(oldText.AsSpan());
        if (replacementStartIndex < 0)
        {
            return text.ToString();
        }

        return buildReplaced(replacementStartIndex, text, oldText, newText);

        static string buildReplaced(int startIndex, ReadOnlySpan<char> text, string oldText, string newText)
        {
            var builder = new StringBuilderSpan(text);
            builder.Replace(oldText, newText, startIndex);
            return builder.GetStringAndDispose();
        }
    }

    public static char GetCharOrTerminator(this string @this, int index)
    {
        return index < @this.Length ? @this[index] : '\0';
    }

    public static char GetCharOrTerminator(this Span<char> @this, int index)
    {
        return index < @this.Length ? @this[index] : '\0';
    }

#if NO_STRING_SPAN
    public static string ConcatString(char c, ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return c.ToString();
        }

        var builder = new StringBuilderSpan(span.Length + 1);
        builder.Append(c);
        builder.Append(span);
        return builder.GetStringAndDispose();
    }
#else
    public static string ConcatString(char c, ReadOnlySpan<char> span) => string.Concat([c], span);
#endif

#if NO_STRING_SPAN
    public static string ConcatString(ReadOnlySpan<char> str0, string str1, char char2, ReadOnlySpan<char> str3)
    {
        var builder = new StringBuilderSpan(str0.Length + str1.Length + str3.Length + 1);
        builder.Append(str0);
        builder.Append(str1);
        builder.Append(char2);
        builder.Append(str3);
        return builder.GetStringAndDispose();
    }
#else
    public static string ConcatString(ReadOnlySpan<char> str0, string str1, char char2, ReadOnlySpan<char> str3) => string.Concat(str0, str1, [char2], str3);
#endif

#if NO_STRING_SPAN
    public static string ConcatString(string str0, ReadOnlySpan<char> str1, string str2, ReadOnlySpan<char> str3)
    {
        var builder = new StringBuilderSpan(str0.Length + str1.Length + str2.Length + str3.Length);
        builder.Append(str0);
        builder.Append(str1);
        builder.Append(str2);
        builder.Append(str3);
        return builder.GetStringAndDispose();
    }
#else
    public static string ConcatString(string str0, ReadOnlySpan<char> str1, string str2, ReadOnlySpan<char> str3) => string.Concat(str0, str1, str2, str3);
#endif

#if NO_STRING_SPAN
    public static string ConcatString(string str0, string str1, string str2, ReadOnlySpan<char> str3)
    {
        var builder = new StringBuilderSpan(str0.Length + str1.Length + str2.Length + str3.Length);
        builder.Append(str0);
        builder.Append(str1);
        builder.Append(str2);
        builder.Append(str3);
        return builder.GetStringAndDispose();
    }
#else
    public static string ConcatString(string str0, string str1, string str2, ReadOnlySpan<char> str3) => string.Concat(str0, str1, str2, str3);
#endif

#if NO_STRING_SPAN
    public static string ConcatString(ReadOnlySpan<char> str0, string str1, ReadOnlySpan<char> str2)
    {
        var builder = new StringBuilderSpan(str0.Length + str1.Length + str2.Length);
        builder.Append(str0);
        builder.Append(str1);
        builder.Append(str2);
        return builder.GetStringAndDispose();
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConcatString(ReadOnlySpan<char> str0, string str1, ReadOnlySpan<char> str2) => string.Concat(str0, str1, str2);
#endif

#if NO_STRING_SPAN
    public static string ConcatString(ReadOnlySpan<char> str0, char char1, ReadOnlySpan<char> str2)
    {
        var builder = new StringBuilderSpan(str0.Length + str2.Length + 1);
        builder.Append(str0);
        builder.Append(char1);
        builder.Append(str2);
        return builder.GetStringAndDispose();
    }
#else
    public static string ConcatString(ReadOnlySpan<char> str0, char char1, ReadOnlySpan<char> str2) => string.Concat(str0, [char1], str2);
#endif

#if NO_STRING_SPAN
    public static string ConcatString(this ReadOnlySpan<char> @this, char value)
    {
        if (@this.IsEmpty)
        {
            return value.ToString();
        }

        var builder = new StringBuilderSpan(@this.Length + 1);
        builder.Append(@this);
        builder.Append(value);
        return builder.GetStringAndDispose();
    }
#else
    public static string ConcatString(this ReadOnlySpan<char> @this, char value) => string.Concat(@this, [value]);
#endif

#if NO_STRING_SPAN
    public static string ConcatString(this string @this, char value)
    {
        if (@this.Length == 0)
        {
            return value.ToString();
        }

        var builder = new StringBuilderSpan(@this.Length + 1);
        builder.Append(@this);
        builder.Append(value);
        return builder.GetStringAndDispose();
    }
#else
    public static string ConcatString(this string @this, char value) => string.Concat(@this, [value]);
#endif

#if NO_STRING_SPAN
    public static string ConcatString(this ReadOnlySpan<char> @this, ReadOnlySpan<char> value)
    {
        if (@this.IsEmpty)
        {
            return value.ToString();
        }

        if (value.IsEmpty)
        {
            return @this.ToString();
        }

        var builder = new StringBuilderSpan(@this.Length + value.Length);
        builder.Append(@this);
        builder.Append(value);
        return builder.GetStringAndDispose();
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConcatString(this ReadOnlySpan<char> @this, ReadOnlySpan<char> value) => string.Concat(@this, value);
#endif

    public static string ConcatString(this ReadOnlySpan<char> @this, string value) => @this.IsEmpty ? value : ConcatString(@this, value.AsSpan());

    public static string ConcatString(this string @this, ReadOnlySpan<char> value) => value.IsEmpty ? @this : ConcatString(@this.AsSpan(), value);

    public static ReadOnlySpan<char> ConcatSpan(this ReadOnlySpan<char> @this, string value) => value.Length == 0 ? @this : ConcatString(@this, value).AsSpan();

    public static ReadOnlySpan<char> ConcatSpan(this string @this, ReadOnlySpan<char> value) => @this.Length == 0 ? value : ConcatString(@this, value).AsSpan();

    public static string ToStringWithoutChars(this ReadOnlySpan<char> text, char value)
    {
        if (text.IsEmpty)
        {
            return string.Empty;
        }

        var index = text.IndexOf(value);
        if (index < 0)
        {
            return text.ToString();
        }

        if (index == text.Length - 1)
        {
            return text.Slice(0, index).ToString();
        }

        return build(text, index, value);

        static string build(ReadOnlySpan<char> text, int index, char value)
        {
            var builder = new StringBuilderSpan(text.Length - 1);

            do
            {
                builder.Append(text.Slice(0, index));
                text = text.Slice(index + 1);
            }
            while ((index = text.IndexOf(value)) >= 0);

            builder.Append(text);

            return builder.GetStringAndDispose();
        }
    }

    public static string ToStringReversed(this ReadOnlySpan<char> @this)
    {
        if (@this.Length == 0)
        {
            return string.Empty;
        }

        if (@this.Length == 1)
        {
            return @this.ToString();
        }

        var builder = new StringBuilderSpan(@this.Length);
        builder.AppendReversed(@this);
        return builder.GetStringAndDispose();
    }

#if NO_STRING_SPAN

    public static string GetReversed(this string @this)
    {
        if (@this is not { Length: > 1 })
        {
            return @this;
        }

        var builder = new StringBuilderSpan(@this.Length);
        builder.AppendReversed(@this);
        return builder.GetStringAndDispose();
    }

#else

    public static string GetReversed(this string @this)
    {
        if (@this is not { Length: > 1 })
        {
            return @this;
        }

        return string.Create(@this.Length, @this, static (span, @this) =>
        {
            @this.CopyToReversed(span);
        });
    }

#endif

    public static SpanSeparatorSplitEnumerator<char> SplitOnComma(this ReadOnlySpan<char> @this, StringSplitOptions options = StringSplitOptions.None) => new(@this, options, static span => span.IndexOf(','));

    public static SpanSeparatorSplitEnumerator<char> SplitOnTabOrSpace(this ReadOnlySpan<char> @this) => new(@this, StringSplitOptions.RemoveEmptyEntries, static span => span.IndexOfTabOrSpace());

#if NO_STATIC_STRINGCHAR_METHODS

    public static string Join(char seperator, string[] items)
    {
        if (items.Length == 0)
        {
            return string.Empty;
        }

        if (items.Length == 1)
        {
            return items[0];
        }

        return joinUsingBuilder(seperator, items);

        static string joinUsingBuilder(char seperator, string[] items)
        {
            var requiredCapacity = items.Length - 1;
            int i;
            for (i = 0; i < items.Length; i++)
            {
                requiredCapacity += items[i].Length;
            }

            var builder = new StringBuilderSpan(requiredCapacity);
            builder.Append(items[0]);

            for (i = 1; i < items.Length; i++)
            {
                builder.Append(seperator);
                builder.Append(items[i]);
            }

            return builder.GetStringAndDispose();
        }
    }

#endif

}

