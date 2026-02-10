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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace WeCantSpell.Hunspell;

internal static class CollectionsEx
{
    internal const int CollectionPreallocationLimit = 16384;

#if NO_NONENUMERATED_COUNT

    public static int GetNonEnumeratedCountOrDefault<T>(this IEnumerable<T> enumerable) => enumerable switch
    {
        ICollection<T> c => c.Count,
        ICollection c => c.Count,
        IReadOnlyCollection<T> c => c.Count,
        _ => 0
    };

#else

    public static int GetNonEnumeratedCountOrDefault<T>(this IEnumerable<T> enumerable) =>
        enumerable.TryGetNonEnumeratedCount(out var count) ? count : 0;

#endif

    public static void ReplaceLast<T>(this List<T> list, T item)
    {
        var index = list.Count - 1;
        if (index >= 0)
        {
            list[index] = item;
        }
        else
        {
            list.Add(item);
        }
    }

    public static int RemoveDuplicates<T>(this List<T> list, IEqualityComparer<T> comparer)
    {
        if (list.Count < 2)
        {
            return 0;
        }

        if (list.Count == 2)
        {
            if (comparer.Equals(list[0], list[1]))
            {
                list.RemoveAt(1);
                return 1;
            }

            return 0;
        }

        return generalCase();

        int generalCase()
        {

#if NO_HASHSET_CAPACITY
            var set = new HashSet<T>(comparer);
#else
            var set = new HashSet<T>(list.Count, comparer);
#endif

            set.Add(list[0]);

            var writeIndex = 1;
            for (var readIndex = 1; readIndex < list.Count; readIndex++)
            {
                var value = list[readIndex];
                if (set.Add(value))
                {
                    if (readIndex != writeIndex)
                    {
                        list[writeIndex] = value;
                    }

                    writeIndex++;
                }
            }

            var duplicateCount = list.Count - writeIndex;
            if (duplicateCount > 0)
            {
                list.RemoveRange(writeIndex, duplicateCount);
            }

            return duplicateCount;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any<T>(this List<T> list) => list.Count != 0;

#if NO_SPAN_CONTAINS

    public static bool Contains<T>(this T[] values, T value) where T : IEquatable<T> => Array.IndexOf(values, value) >= 0;

#else

    public static bool Contains<T>(this T[] values, T value) where T : IEquatable<T> => values.AsSpan().Contains(value);

#endif

#if NO_DICTIONARY_GETVALUE

    public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) =>
        dictionary.TryGetValue(key, out var result) ? result : default;

#endif

#if NO_KVP_DECONSTRUCT

    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }

#endif

}

