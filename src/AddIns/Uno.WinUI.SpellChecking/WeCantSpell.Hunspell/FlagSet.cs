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
using System.Globalization;
using System.Linq;

namespace WeCantSpell.Hunspell;

public readonly struct FlagSet : IReadOnlyList<FlagValue>, IEquatable<FlagSet>
{
    public static readonly FlagSet Empty = new(string.Empty);

    public static bool operator ==(FlagSet left, FlagSet right) => left.Equals(right);

    public static bool operator !=(FlagSet left, FlagSet right) => !left.Equals(right);

    public static FlagSet Create(FlagValue value) => value.IsZero ? Empty : new(value);

    public static FlagSet Create(char value) => value == FlagValue.ZeroValue ? Empty : new(value);

    public static FlagSet Create(FlagValue value0, FlagValue value1)
    {
        if (value0.IsZero)
        {
            return Create(value1);
        }

        if (value1.IsZero)
        {
            return Create(value0);
        }

        return CreateUsingMutableBuffer([value0, value1]);
    }

    public static FlagSet Create(FlagValue value0, FlagValue value1, FlagValue value2)
    {
        return CreateUsingMutableBuffer([value0, value1, value2]);
    }

    public static FlagSet Create(FlagValue value0, FlagValue value1, FlagValue value2, FlagValue value3)
    {
        return CreateUsingMutableBuffer([value0, value1, value2, value3]);
    }

    public static FlagSet Create(IEnumerable<FlagValue> values)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(values);
#else
        ExceptionEx.ThrowIfArgumentNull(values, nameof(values));
#endif

        var builder = new StringBuilderSpan(values.GetNonEnumeratedCountOrDefault());

        foreach (var value in values)
        {
            builder.AppendIfNotNull(value);
        }

        builder.Sort();
        builder.RemoveAdjacentDuplicates();
        return new(builder.GetStringAndDispose());
    }

    public static FlagSet Create(ReadOnlySpan<FlagValue> values)
    {
        var builder = new StringBuilderSpan(values.Length);

        foreach (var value in values)
        {
            builder.AppendIfNotNull(value);
        }

        builder.Sort();
        builder.RemoveAdjacentDuplicates();
        return new(builder.GetStringAndDispose());
    }

    internal static FlagSet CreateFromPreparedValues(string values)
    {
#if DEBUG
        if (!ValidateFlagSetData(values)) ExceptionEx.ThrowArgumentOutOfRange(nameof(values));
#endif

        return new(values);
    }

    internal static FlagSet ParseAsChars(string text)
    {
        var span = text.AsSpan();
        return ValidateFlagSetData(span)
            ? CreateFromPreparedValues(text)
            : CreateWithMutation(span);
    }

    internal static FlagSet ParseAsChars(ReadOnlySpan<char> text)
    {
        return ValidateFlagSetData(text)
            ? CreateFromPreparedValues(text.ToString())
            : CreateWithMutation(text);
    }

    private static bool ValidateFlagSetData(ReadOnlySpan<char> values)
    {
        if (values.Length > 0)
        {
            if (values[0] == '\0')
            {
                return false;
            }

            for (var i = 1; i < values.Length; i++)
            {
                var c = values[i];
                if (c == '\0' || values[i - 1] >= c)
                {
                    return false;
                }
            }
        }

        return true;
    }

    static FlagSet CreateWithMutation(ReadOnlySpan<char> text)
    {
        if (text.Length <= 4)
        {
            var buffer = (stackalloc char[4]).Slice(0, text.Length);
            text.CopyTo(buffer);
            return CreateUsingMutableBuffer(buffer);
        }

        return CreateFromBuilderChars(text);
    }

    private static FlagSet CreateFromBuilderChars(ReadOnlySpan<char> text)
    {
        var builder = new StringBuilderSpan(text);
        builder.RemoveAll(FlagValue.ZeroValue);
        builder.Sort();
        builder.RemoveAdjacentDuplicates();
        return new(builder.GetStringAndDispose());
    }

    internal static FlagSet ParseAsLongs(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return Empty;
        }

        var lastIndex = text.Length - 1;
        var builder = new StringBuilderSpan((text.Length + 1) / 2);

        for (var i = 0; i < lastIndex; i += 2)
        {
            builder.AppendIfNotNull(FlagValue.CreateAsLong(text[i], text[i + 1]));
        }

        if ((lastIndex & 1) == 0)
        {
            builder.AppendIfNotNull(new FlagValue(text[lastIndex]));
        }

        builder.Sort();
        builder.RemoveAdjacentDuplicates();
        return new(builder.GetStringAndDispose());
    }

    internal static FlagSet ParseAsNumbers(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return Empty;
        }

        var builder = new StringBuilderSpan(text.Length);

        foreach (var part in text.SplitOnComma(StringSplitOptions.RemoveEmptyEntries))
        {
            if (FlagValue.TryParseAsNumber(part, out var value))
            {
                builder.AppendIfNotNull(value);
            }
        }

        builder.Sort();
        builder.RemoveAdjacentDuplicates();
        return new(builder.GetStringAndDispose());
    }

    private static FlagSet CreateUsingMutableBuffer(Span<char> values)
    {
        MemoryEx.RemoveAll(ref values, FlagValue.ZeroValue);

        values.Sort();

        MemoryEx.RemoveAdjacentDuplicates(ref values);

        return new(values.ToString());
    }

#if !HAS_SEARCHVALUES

    private static bool SortedIntersectionTest(ReadOnlySpan<char> aSet, ReadOnlySpan<char> bSet)
    {

#if DEBUG
        ExceptionEx.ThrowIfArgumentEmpty(aSet, nameof(aSet));
        ExceptionEx.ThrowIfArgumentEmpty(bSet, nameof(bSet));
#endif

        return (aSet[aSet.Length - 1] >= bSet[0] && bSet[bSet.Length - 1] >= aSet[0]) // Check for disjoint sets
            &&
            (
                (aSet.Length <= 4 && bSet.Length <= 4)
                ? sortedInterectionTestLinear(aSet, bSet)
                : sortedInterectionTestBinary(aSet, bSet)
            );

        static bool sortedInterectionTestLinear(ReadOnlySpan<char> aSet, ReadOnlySpan<char> bSet)
        {
            var aIndex = 0;
            var bIndex = 0;

            do
            {
                switch (aSet[aIndex].CompareTo(bSet[bIndex]))
                {
                    case 0:
                        return true;

                    case < 0:
                        aIndex++;

                        if (aIndex >= aSet.Length)
                        {
                            goto disjointOrEmptyCantMatch;
                        }

                        break;

                    default:
                        bIndex++;

                        if (bIndex >= bSet.Length)
                        {
                            goto disjointOrEmptyCantMatch;
                        }

                        break;
                }
            }
            while (true);

        disjointOrEmptyCantMatch:

            return false;
        }

        static bool sortedInterectionTestBinary(ReadOnlySpan<char> aSet, ReadOnlySpan<char> bSet)
        {
            do
            {
                if (aSet.Length > bSet.Length)
                {
                    MemoryEx.Swap(ref aSet, ref bSet);
                }

                var flagValuesIndex = bSet.BinarySearch(aSet[0]);
                if (flagValuesIndex >= 0)
                {
                    return true;
                }

                if (aSet.Length <= 1)
                {
                    break;
                }

                flagValuesIndex = ~flagValuesIndex;

                if (bSet.Length <= flagValuesIndex)
                {
                    break;
                }

                aSet = aSet.Slice(1);
                bSet = bSet.Slice(flagValuesIndex);
            }
            while (true);

            return false;
        }
    }

#endif

    private FlagSet(FlagValue value) : this((char)value)
    {
    }

    private FlagSet(char value) : this(value.ToString())
    {
    }

#if HAS_SEARCHVALUES

    private FlagSet(string values)
    {
        _values = values;
        _searchValues = SearchValues.Create(values);
    }

    private readonly string? _values;
    private readonly SearchValues<char>? _searchValues;

#else

    private FlagSet(string values)
    {
        _values = values;
    }

    private readonly string? _values;

#endif

    public int Count => _values is not null ? _values.Length : 0;

    public bool IsEmpty => _values is not { Length: > 0 };

    public bool HasItems => _values is { Length: > 0 };

    public FlagValue this[int index]
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

            return (FlagValue)_values![index];
        }
    }

    public IEnumerator<FlagValue> GetEnumerator() => GetInternalText().Select(static v => (FlagValue)v).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        if (_values is { Length: > 0 })
        {
            if (((FlagValue)_values[0]).IsPrintable && (_values.Length == 1 || ((FlagValue)_values[_values.Length - 1]).IsPrintable))
            {
                return _values;
            }
            else
            {
                return string.Join(",", _values.Select(static v => ((int)v).ToString(CultureInfo.InvariantCulture.NumberFormat)));
            }
        }

        return string.Empty;
    }

    public override int GetHashCode() => (int)StringEx.GetStableOrdinalHashCode(GetInternalText());

    public bool Equals(FlagSet other) => GetInternalText().Equals(other.GetInternalText(), StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is FlagSet set && Equals(set);


#if HAS_SEARCHVALUES

    public bool Contains(FlagValue value) => value != FlagValue.Zero && _searchValues!.Contains(value);

    public bool Contains(char value) => value != FlagValue.ZeroValue && _searchValues!.Contains(value);

    public bool DoesNotContain(FlagValue value) => value == FlagValue.Zero || !_searchValues!.Contains(value);

    public bool DoesNotContain(char value) => value == FlagValue.ZeroValue || !_searchValues!.Contains(value);

    public bool ContainsAny(FlagValue a, FlagValue b) =>
        ((ReadOnlySpan<char>)[a, b]).ContainsAny(_searchValues!);

    public bool ContainsAny(FlagValue a, FlagValue b, FlagValue c) =>
        ((ReadOnlySpan<char>)[a, b, c]).ContainsAny(_searchValues!);

    public bool ContainsAny(FlagValue a, FlagValue b, FlagValue c, FlagValue d) =>
        ((ReadOnlySpan<char>)[a, b, c, d]).ContainsAny(_searchValues!);

    public bool ContainsAny(FlagValue a, FlagValue b, FlagValue c, FlagValue d, FlagValue e) =>
        ((ReadOnlySpan<char>)[a, b, c, d, e]).ContainsAny(_searchValues!);

    public bool ContainsAny(FlagSet other)
    {
        return _values is { Length: > 0 }
            &&
            other._values is { Length: > 0 }
            &&
            (
                _values.Length < other._values.Length
                ? _values.ContainsAny(other._searchValues!)
                : other._values.ContainsAny(_searchValues!)
            );
    }

    public bool DoesNotContainAny(FlagSet other) => !ContainsAny(other);

#else

    public bool Contains(FlagValue value) => Contains((char)value);

    public bool Contains(char value)
    {
        return
            value != FlagValue.ZeroValue
            &&
            _values is not null
            &&
            _values.Length switch
            {
                0 => false,
                1 => _values[0] == value,
                2 => _values[0] == value || _values[1] == value,
                3 => _values[0] == value || _values[1] == value || _values[2] == value,
                _ => MemoryEx.SortedLargeSearchSpaceContains(_values, value)
            };
    }

    public bool DoesNotContain(FlagValue value) => DoesNotContain((char)value);

    public bool DoesNotContain(char value)
    {
        return
            value == FlagValue.ZeroValue
            ||
            _values is null
            ||
            _values.Length switch
            {
                0 => true,
                1 => _values[0] != value,
                2 => _values[0] != value && _values[1] != value,
                3 => _values[0] != value && _values[1] != value && _values[2] != value,
                _ => !MemoryEx.SortedLargeSearchSpaceContains(_values, value)
            };
    }

    public bool ContainsAny(FlagValue a, FlagValue b)
    {
        if (a.IsZero)
        {
            return Contains(b);
        }

        if (b.IsZero)
        {
            return Contains(a);
        }

        if (_values is not { Length: > 0 })
        {
            return false;
        }

        if (_values.Length == 1)
        {
            return _values[0] == a || _values[0] == b;
        }

        if (a > b)
        {
            MemoryEx.Swap(ref a, ref b);
        }

        return ContainsAnyPrepared([a, b]);
    }

    public bool ContainsAny(FlagValue a, FlagValue b, FlagValue c)
    {
        if (a.IsZero)
        {
            return ContainsAny(b, c);
        }

        if (b.IsZero)
        {
            return ContainsAny(a, c);
        }

        if (c.IsZero)
        {
            return ContainsAny(a, b);
        }

        if (IsEmpty)
        {
            return false;
        }

        Span<char> other = [a, b, c];
        other.Sort();
        return ContainsAnyPrepared(other);
    }

    public bool ContainsAny(FlagValue a, FlagValue b, FlagValue c, FlagValue d)
    {
        if (a.IsZero)
        {
            return ContainsAny(b, c, d);
        }

        if (b.IsZero)
        {
            return ContainsAny(a, c, d);
        }

        if (c.IsZero)
        {
            return ContainsAny(a, b, d);
        }

        if (d.IsZero)
        {
            return ContainsAny(a, b, c);
        }

        if (IsEmpty)
        {
            return false;
        }

        Span<char> other = [a, b, c, d];
        other.Sort();
        return ContainsAnyPrepared(other);
    }

    public bool ContainsAny(FlagSet other)
    {
        return HasItems
            &&
            other.HasItems
            &&
            (
                other._values!.Length == 1
                ? Contains(other._values[0])
                : (
                    _values!.Length == 1
                        ? other.Contains(_values[0])
                        : SortedIntersectionTest(other._values, _values)
                )
            );
    }

    public bool DoesNotContainAny(FlagSet other)
    {
        return IsEmpty
            ||
            other.IsEmpty
            ||
            (
                other._values!.Length == 1
                ? DoesNotContain(other._values[0])
                : (
                    _values!.Length == 1
                        ? other.DoesNotContain(_values[0])
                        : !SortedIntersectionTest(other._values, _values)
                )
            );
    }

    private bool ContainsAnyPrepared(ReadOnlySpan<char> other)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentEmpty(other, nameof(other));
        ExceptionEx.ThrowIfArgumentEqual(other.Length, 1, nameof(other));
        if (other.Contains(FlagValue.ZeroValue)) ExceptionEx.ThrowArgumentOutOfRange(nameof(other));
        for (var i = 1; i < other.Length; i++)
        {
            if (other[i - 1] > other[i]) ExceptionEx.ThrowArgumentOutOfRange(nameof(other));
        }
#endif

        return _values is not null
            &&
            (
                _values!.Length == 1
                ? MemoryEx.SortedLargeSearchSpaceContains(other, _values[0])
                : SortedIntersectionTest(_values, other)
            );
    }

#endif

    public FlagSet Union(FlagSet other)
    {
        if (_values is not { Length: > 0 })
        {
            return other;
        }

        if (other._values is not { Length: > 0 })
        {
            return this;
        }

        if (other._values.Length == 1)
        {
            return Union(other[0]);
        }

        if (_values.Length == 1)
        {
            return other.Union(this[0]);
        }

        var builder = new StringBuilderSpan(other.Count + Count);
        builder.Append(_values);
        builder.Append(other._values);
        builder.Sort();
        builder.RemoveAdjacentDuplicates();
        return new(builder.GetStringAndDispose());
    }

    public FlagSet Union(FlagValue value)
    {
        if (value.IsZero)
        {
            return this;
        }

        if (IsEmpty)
        {
            return new(value);
        }

        var oldValues = _values.AsSpan();

        var valueIndex = oldValues.BinarySearch((char)value);
        if (valueIndex >= 0)
        {
            return this;
        }

        valueIndex = ~valueIndex; // locate the best insertion point

        var builder = new StringBuilderSpan(oldValues.Length + 1);

        if (valueIndex >= oldValues.Length)
        {
            builder.Append(oldValues);
            builder.Append(value);
        }
        else if (valueIndex == 0)
        {
            builder.Append(value);
            builder.Append(oldValues);
        }
        else
        {
            builder.Append(oldValues.Slice(0, valueIndex));
            builder.Append(value);
            builder.Append(oldValues.Slice(valueIndex));
        }

        return new(builder.GetStringAndDispose());
    }

    internal string GetInternalText() => _values ?? string.Empty;
}

