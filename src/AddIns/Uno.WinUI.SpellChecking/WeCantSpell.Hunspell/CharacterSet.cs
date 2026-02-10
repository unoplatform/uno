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
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace WeCantSpell.Hunspell;

public readonly struct CharacterSet : IReadOnlyList<char>, IEquatable<CharacterSet>, IEquatable<string>
{
    public static readonly CharacterSet Empty = new(string.Empty);

    public static bool operator ==(CharacterSet left, CharacterSet right) => left.Equals(right);

    public static bool operator !=(CharacterSet left, CharacterSet right) => !left.Equals(right);

    public static CharacterSet Create(char value) => new(value.ToString());

    public static CharacterSet Create(IEnumerable<char> values)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(values);
#else
        ExceptionEx.ThrowIfArgumentNull(values, nameof(values));
#endif

        var builder = new StringBuilderSpan(values.GetNonEnumeratedCountOrDefault());
        builder.Append(values);
        builder.Sort();
        builder.RemoveAdjacentDuplicates();
        return new(builder.GetStringAndDispose());
    }

    public static CharacterSet Create(string values)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(values);
#else
        ExceptionEx.ThrowIfArgumentNull(values, nameof(values));
#endif

        var valuesSpan = values.AsSpan();
        return valuesSpan.CheckSortedWithoutDuplicates()
            ? new(values)
            : Create(valuesSpan);
    }

    public static CharacterSet Create(ReadOnlySpan<char> values)
    {
        if (values.IsEmpty)
        {
            return Empty;
        }

        var builder = new StringBuilderSpan(values);
        builder.Sort();
        builder.RemoveAdjacentDuplicates();
        return new(builder.GetStringAndDispose());
    }

#if HAS_SEARCHVALUES

    private CharacterSet(string values)
    {
        _values = values;
        _searchValues = SearchValues.Create(values);
    }

    private readonly string? _values;
    private readonly SearchValues<char>? _searchValues;

#else

    private CharacterSet(string values)
    {
        _values = values;
    }

    private readonly string? _values;

#endif

    public int Count => _values is not null ? _values.Length : 0;

    public bool IsEmpty => _values is not { Length: > 0 };

    public bool HasItems => _values is { Length: > 0 };

    public char this[int index]
    {
        get
        {
#if HAS_THROWOOR
            ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
#else
            ExceptionEx.ThrowIfArgumentLessThan(index, 0, nameof(index));
            ExceptionEx.ThrowIfArgumentGreaterThanOrEqual(index, Count, nameof(index));
#endif

            if (_values is null)
            {
                ExceptionEx.ThrowInvalidOperation("Not initialized");
            }

            return _values![index];
        }
    }

    public IEnumerator<char> GetEnumerator() => ToString().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#if HAS_SEARCHVALUES

    public bool Contains(char value) => _searchValues!.Contains(value);

    public int FindIndexOfMatch(ReadOnlySpan<char> text) => text.IndexOfAny(_searchValues!);

#else

    public bool Contains(char value) => MemoryEx.SortedLargeSearchSpaceContains(_values, value);

    public int FindIndexOfMatch(ReadOnlySpan<char> text)
    {
        if (_values is not null)
        {
            switch (_values.Length)
            {
                case 1:
                    return text.IndexOf(_values[0]);
                case <= 5: // There are special cases in IndexOfAny for sizes 5 or less
                    return text.IndexOfAny(_values);
                default:
                    var values = _values.AsSpan();
                    for (var searchLocation = 0; searchLocation < text.Length; searchLocation++)
                    {
                        if (MemoryEx.SortedLargeSearchSpaceContains(values, text[searchLocation]))
                        {
                            return searchLocation;
                        }
                    }

                    break;
            }
        }

        return -1;
    }

#endif

    public int FindIndexOfMatch(ReadOnlySpan<char> text, int startIndex)
    {
        var result = FindIndexOfMatch(text.Slice(startIndex));
        return result < 0 ? result : result + startIndex;
    }

    public string RemoveChars(string text)
    {
        if (text is not { Length: > 0 } || IsEmpty)
        {
            return text;
        }

        var textSpan = text.AsSpan();
        var index = FindIndexOfMatch(textSpan);
        if (index < 0)
        {
            return text;
        }

        do
        {
            if (index == textSpan.Length - 1)
            {
                return textSpan.Slice(0, index).ToString();
            }

            if (index != 0)
            {
                break;
            }

            if (textSpan.Length <= 1)
            {
                return string.Empty;
            }

            textSpan = textSpan.Slice(1);

            index = FindIndexOfMatch(textSpan);
            if (index < 0)
            {
                return textSpan.ToString();
            }
        }
        while (true);

        return ToStringWithRemoval(textSpan, index);
    }

    public ReadOnlySpan<char> RemoveChars(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty || IsEmpty)
        {
            return text;
        }

        int index;
        do
        {
            index = FindIndexOfMatch(text);
            if (index < 0)
            {
                return text;
            }

            if (index == text.Length - 1)
            {
                return text.Slice(0, index);
            }

            if (index != 0)
            {
                break;
            }

            if (text.Length <= 1)
            {
                return [];
            }

            text = text.Slice(1);
        }
        while (true);

        return ToStringWithRemoval(text, index);
    }

    private string ToStringWithRemoval(ReadOnlySpan<char> text, int matchIndex)
    {
        var builder = new StringBuilderSpan(text.Length - 1);

        do
        {
            if (matchIndex > 0)
            {
                builder.Append(text.Slice(0, matchIndex));
            }

            text = text.Slice(matchIndex + 1);
            matchIndex = FindIndexOfMatch(text);
        }
        while (matchIndex >= 0);

        builder.Append(text);

        return builder.GetStringAndDispose();
    }

    public override string ToString() => _values ?? string.Empty;

    public bool Equals(CharacterSet other) => ToString().Equals(other.ToString(), StringComparison.Ordinal);

    public bool Equals(string? other) => other is not null && ToString().Equals(other, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj switch
    {
        CharacterSet set => Equals(set),
        string value => Equals(value),
        _ => false
    };

    public override int GetHashCode() => unchecked((int)StringEx.GetStableOrdinalHashCode(ToString()));
}

