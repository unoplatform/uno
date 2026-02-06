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

namespace WeCantSpell.Hunspell;

public readonly struct CharacterConditionGroup : IReadOnlyList<CharacterCondition>
{
#if HAS_SEARCHVALUES
    private static readonly System.Buffers.SearchValues<char> ConditionParseStopCharacters = System.Buffers.SearchValues.Create(".[");
#endif

    public static readonly CharacterConditionGroup Empty = new([]);

    public static readonly CharacterConditionGroup AllowAnySingleCharacter = Create(CharacterCondition.AllowAny);

    public static CharacterConditionGroup Create(CharacterCondition condition) => new([condition]);

    public static CharacterConditionGroup Create(IEnumerable<CharacterCondition> conditions)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(conditions);
#else
        ExceptionEx.ThrowIfArgumentNull(conditions, nameof(conditions));
#endif

        return new([.. conditions]);
    }

    public static CharacterConditionGroup Parse(string text)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(text);
#else
        ExceptionEx.ThrowIfArgumentNull(text, nameof(text));
#endif

        return Parse(text.AsSpan());
    }

    public static CharacterConditionGroup Parse(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return Empty;
        }

        ReadOnlySpan<char> span;
        var conditions = ArrayBuilder<CharacterCondition>.Pool.Get();
        int index;

        do
        {
            switch (text[0])
            {
                case '.':
                    conditions.Add(CharacterCondition.AllowAny);
                    text = text.Slice(1);

                    break;

                case '[':
                    span = text.Slice(1);
                    index = span.IndexOf(']');
                    if (index >= 0)
                    {
                        span = span.Slice(0, index);
                        text = text.Slice(index + 2);
                    }
                    else
                    {
                        text = [];
                    }

                    if (span.Length > 0 && span[0] == '^')
                    {
                        conditions.Add(CharacterCondition.CreateCharSet(span.Slice(1), CharacterCondition.ModeKind.RestrictChars));
                    }
                    else
                    {
                        conditions.Add(CharacterCondition.CreateCharSet(span, CharacterCondition.ModeKind.PermitChars));
                    }

                    break;

                default:
#if HAS_SEARCHVALUES
                    index = text.IndexOfAny(ConditionParseStopCharacters);
#else
                    index = text.IndexOfAny('.', '[');
#endif

                    if (index >= 0)
                    {
                        span = text.Slice(0, index);
                        text = text.Slice(index);
                    }
                    else
                    {
                        span = text;
                        text = [];
                    }

                    conditions.Add(CharacterCondition.CreateSequence(span));

                    break;
            }
        }
        while (text.Length > 0);

        return new(ArrayBuilder<CharacterCondition>.Pool.ExtractAndReturn(conditions));
    }

    internal CharacterConditionGroup(CharacterCondition[] items)
    {
        _items = items;
    }

    private readonly CharacterCondition[]? _items;

    public int Count => _items is not null ? _items.Length : 0;

    public bool IsEmpty => _items is not { Length: > 0 };

    public bool HasItems => _items is { Length: > 0 };

    internal CharacterCondition[] RawArray => _items ?? [];

    public CharacterCondition this[int index]
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
            return _items![index];
        }
    }

    public IEnumerator<CharacterCondition> GetEnumerator() => ((IEnumerable<CharacterCondition>)RawArray).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool MatchesAnySingleCharacter => _items is { Length: 1 } && _items[0].MatchesAnySingleCharacter;

    public string GetEncoded() => string.Concat(Array.ConvertAll(RawArray, static c => c.GetEncoded()));

    public override string ToString() => GetEncoded();

    /// <summary>
    /// Determines if the start of the given <paramref name="text"/> matches the conditions.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True when the start of the <paramref name="text"/> is matched by the conditions.</returns>
    public bool IsStartingMatch(ReadOnlySpan<char> text)
    {
        if (_items is not null)
        {
            foreach (var condition in _items)
            {
                var matchLength = condition.FullyMatchesFromStart(text);
                if (matchLength > 0)
                {
                    text = text.Slice(matchLength);
                }
                else
                {
                    goto exit;
                }

            }

            return true;
        }

    exit:
        return false;
    }

    /// <summary>
    /// Determines if the end of the given <paramref name="text"/> matches the conditions.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True when the end of the <paramref name="text"/> is matched by the conditions.</returns>
    public bool IsEndingMatch(ReadOnlySpan<char> text)
    {
        if (_items is not null)
        {
            for (var conditionIndex = _items.Length - 1; conditionIndex >= 0; conditionIndex--)
            {
                var matchLength = _items[conditionIndex].FullyMatchesFromEnd(text);
                if (matchLength > 0)
                {
                    text = text.Slice(0, text.Length - matchLength);
                }
                else
                {
                    goto exit;
                }
            }

            return true;
        }

    exit:
        return false;
    }

    public bool IsOnlyPossibleMatch(ReadOnlySpan<char> text)
    {
        if (_items is not null)
        {
            foreach (var condition in _items)
            {
                if (!condition.IsOnlyPossibleMatch(text, out var matchLength))
                {
                    return false;
                }

                text = text.Slice(matchLength);
            }
        }

        return text.IsEmpty;
    }
}

