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
using System.Runtime.CompilerServices;

namespace WeCantSpell.Hunspell;

internal static class MemoryEx
{
    public static int IndexOf(this ReadOnlySpan<char> @this, char value, int startIndex)
    {
        var result = @this.Slice(startIndex).IndexOf(value);
        return result >= 0 ? result + startIndex : result;
    }

    public static int IndexOf(this ReadOnlySpan<char> @this, ReadOnlySpan<char> value, int startIndex)
    {
        var result = @this.Slice(startIndex).IndexOf(value);
        return result >= 0 ? result + startIndex : result;
    }

    public static bool SortedLargeSearchSpaceContains(ReadOnlySpan<char> sorted, char value)
    {
        return
            (value <= sorted[sorted.Length - 1])
            &&
            (
                sorted.Length < 8
                ? checkIterative(sorted, value)
                : (value >= sorted[0] && sorted.BinarySearch(value) >= 0)
            );

        static bool checkIterative(ReadOnlySpan<char> searchSpace, char target)
        {
            foreach (var value in searchSpace)
            {
                if (value == target)
                {
                    return true;
                }

                if (value > target)
                {
                    break;
                }
            }

            return false;
        }
    }

    public static ReadOnlySpan<char> Limit(this ReadOnlySpan<char> @this, int maxLength) =>
        @this.Length > maxLength ? @this.Slice(0, maxLength) : @this;

    public static void Swap<T>(ref T value0, ref T value1)
    {
        (value1, value0) = (value0, value1);
    }

    public static void Swap(this Span<char> span, int index0, int index1)
    {
        (span[index1], span[index0]) = (span[index0], span[index1]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap(ref ReadOnlySpan<char> a, ref ReadOnlySpan<char> b)
    {
        var tmp = a;
        a = b;
        b = tmp;
    }

    public static void CopyToReversed(this ReadOnlySpan<char> source, Span<char> target)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentLessThan(target.Length, source.Length, nameof(target));
#endif

        for (var index = 0; index < source.Length; index++)
        {
            target[target.Length - index - 1] = source[index];
        }
    }

#if NO_SPAN_REPLACE

    public static void Replace(this Span<char> span, char oldValue, char newValue)
    {
        do
        {
            if (span.IsEmpty)
            {
                return;
            }

            var index = span.IndexOf(oldValue);
            if (index < 0)
            {
                return;
            }

            span[index] = newValue;
            span = span.Slice(index + 1);
        }
        while (true);
    }

#endif

#if NO_SPAN_SORT

    public static void Sort<T>(this Span<T> span) where T : IComparable<T>
    {
        // This should be called on small collections and I'm lazy, so it's bubblesort.

        while (span.Length >= 2)
        {
            var hasSwapped = false;

            for (var i = span.Length - 2; i >= 0; i--)
            {
                ref var value0 = ref span[i];
                ref var value1 = ref span[i + 1];
                if (value0.CompareTo(value1) > 0)
                {
                    Swap(ref value0, ref value1);
                    hasSwapped = true;
                }
            }

            if (!hasSwapped)
            {
                break;
            }

            span = span.Slice(1);
        }
    }

#endif

#if NO_SPAN_COMPARISON_SORT

    public static void Sort<T>(this Span<T> span, Comparison<T> comparer)
    {
        // This should be called on small collections and I'm lazy, so it's bubblesort.

        while (span.Length >= 2)
        {
            var hasSwapped = false;

            for (var i = span.Length - 2; i >= 0; i--)
            {
                ref var value0 = ref span[i];
                ref var value1 = ref span[i + 1];
                if (comparer(value0, value1) > 0)
                {
                    Swap(ref value0, ref value1);
                    hasSwapped = true;
                }
            }

            if (!hasSwapped)
            {
                break;
            }

            span = span.Slice(1);
        }
    }

#endif

    public static bool CheckSortedWithoutDuplicates(this ReadOnlySpan<char> span)
    {
        if (span.Length > 1)
        {
            for (var i = 1; i < span.Length; i++)
            {
                if (span[i - 1] >= span[i])
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static void RemoveAll<T>(ref Span<T> span, T value) where T : notnull, IEquatable<T>
    {
        var readIndex = span.IndexOf(value);
        if (readIndex < 0)
        {
            return;
        }

        var writeIndex = readIndex;

        for (; readIndex < span.Length; readIndex++)
        {
            if (!value.Equals(span[readIndex]))
            {
                if (readIndex != writeIndex)
                {
                    span[writeIndex] = span[readIndex];
                }

                writeIndex++;
            }
        }

        if (writeIndex < span.Length)
        {
            span = span.Slice(0, writeIndex);
        }
    }

    public static void RemoveAdjacentDuplicates<T>(ref Span<T> span) where T : notnull, IEquatable<T>
    {
        if (span.Length < 2)
        {
            return;
        }

        var writeIndex = 1;
        var readIndex = 1;

        for (; readIndex < span.Length; readIndex++)
        {
            if (!span[readIndex].Equals(span[writeIndex - 1]))
            {
                if (readIndex != writeIndex)
                {
                    span[writeIndex] = span[readIndex];
                }

                writeIndex++;
            }
        }

        if (writeIndex < span.Length)
        {
            span = span.Slice(0, writeIndex);
        }
    }
}

