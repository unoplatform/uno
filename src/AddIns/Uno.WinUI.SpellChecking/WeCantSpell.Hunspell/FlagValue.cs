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

public readonly struct FlagValue :
    IEquatable<FlagValue>,
    IEquatable<int>,
    IEquatable<char>,
    IComparable<FlagValue>,
    IComparable<int>,
    IComparable<char>
{
    internal const char ZeroValue = '\0';

    public static FlagValue Zero { get; } = (FlagValue)ZeroValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator char(FlagValue flag) => flag._value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator FlagValue(char value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(FlagValue flag) => flag._value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator FlagValue(int value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(FlagValue a, FlagValue b) => a._value != b._value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(FlagValue a, FlagValue b) => a._value == b._value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(FlagValue a, FlagValue b) => a._value >= b._value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(FlagValue a, FlagValue b) => a._value <= b._value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(FlagValue a, FlagValue b) => a._value > b._value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(FlagValue a, FlagValue b) => a._value < b._value;

    internal static FlagValue CreateAsLong(char high, char low) => new(unchecked((char)((high << 8) | low)));

    internal static bool TryParseAsChar(string text, out FlagValue value)
    {
        if (text is { Length: > 0 })
        {
            value = new FlagValue(text[0]);
            return true;
        }

        value = default;
        return false;
    }

    internal static bool TryParseAsChar(ReadOnlySpan<char> text, out FlagValue value)
    {
        if (text.Length > 0)
        {
            value = new FlagValue(text[0]);
            return true;
        }

        value = default;
        return false;
    }

    internal static bool TryParseAsLong(ReadOnlySpan<char> text, out FlagValue value)
    {
        if (text.IsEmpty)
        {
            value = default;
            return false;
        }

        value = text.Length >= 2
            ? CreateAsLong(text[0], text[1])
            : new FlagValue(text[0]);
        return true;
    }

    internal static bool TryParseAsNumber(ReadOnlySpan<char> text, out FlagValue value)
    {
        if (!text.IsEmpty && IntEx.TryParseInvariant(text, out var integerValue) && integerValue >= char.MinValue && integerValue <= char.MaxValue)
        {
            value = new(unchecked((char)integerValue));
            return true;
        }

        value = default;
        return false;
    }

    internal static FlagValue[] ParseAsChars(string text)
    {
        if (text.Length > 0)
        {
            var values = new FlagValue[text.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = new FlagValue(text[i]);
            }

            return values;
        }

        return [];
    }

    internal static FlagValue[] ParseAsChars(ReadOnlySpan<char> text)
    {
        if (text.Length > 0)
        {
            var values = new FlagValue[text.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = new FlagValue(text[i]);
            }

            return values;
        }

        return [];
    }

    internal static FlagValue[] ParseAsLongs(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return [];
        }

        var flags = new FlagValue[(text.Length + 1) / 2];
        var flagWriteIndex = 0;
        var lastIndex = text.Length - 1;
        for (var i = 0; i < lastIndex; i += 2, flagWriteIndex++)
        {
            flags[flagWriteIndex] = CreateAsLong(text[i], text[i + 1]);
        }

        if (flagWriteIndex < flags.Length)
        {
            flags[flagWriteIndex] = new FlagValue(text[lastIndex]);
        }

        return flags;
    }

    internal static FlagValue[] ParseAsNumbers(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return [];
        }

        var builder = ArrayBuilder<FlagValue>.Pool.Get();

        foreach (var part in text.SplitOnComma(StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseAsNumber(part, out var value))
            {
                builder.Add(value);
            }
        }

        return ArrayBuilder<FlagValue>.Pool.ExtractAndReturn(builder);
    }

    public FlagValue(char value)
    {
        _value = value;
    }

    public FlagValue(int value)
    {
        _value = checked((char)value);
    }

    private readonly char _value;

    public bool HasValue => _value != ZeroValue;

    public bool IsZero => _value == ZeroValue;

    public bool IsWildcard => _value is '*' or '?';

    public bool IsNotWildcard => _value is not '*' or '?';

    internal bool IsPrintable => (int)_value is > 32 and < 127;

    public bool Equals(FlagValue other) => other._value == _value;

    [Obsolete("To be removed")]
    public bool EqualsAny(FlagValue a, FlagValue b) => a._value == _value || b._value == _value;

    [Obsolete("To be removed")]
    public bool EqualsAny(FlagValue a, FlagValue b, FlagValue c) => a._value == _value || b._value == _value || c._value == _value;

    public bool Equals(int other) => other == _value;

    public bool Equals(char other) => other == _value;

    public override bool Equals(object? obj) => obj switch
    {
        FlagValue value => Equals(value),
        int value => Equals(value),
        char value => Equals(value),
        _ => false,
    };

    public override int GetHashCode() => _value.GetHashCode();

    public int CompareTo(FlagValue other) => _value.CompareTo(other._value);

    public int CompareTo(int other) => ((int)_value).CompareTo(other);

    public int CompareTo(char other) => _value.CompareTo(other);

    public override string ToString()
    {
        var result = ((int)_value).ToString(CultureInfo.InvariantCulture);

        if (IsPrintable)
        {
            result += " '" + _value.ToString() + "'";
        }

        return result;
    }
}

