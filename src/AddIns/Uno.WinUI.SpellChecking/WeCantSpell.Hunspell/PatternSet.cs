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
using System.Diagnostics;

namespace WeCantSpell.Hunspell;

[DebuggerDisplay("Count = {Count}")]
public readonly struct PatternSet : IReadOnlyList<PatternEntry>
{
    public static PatternSet Empty { get; } = new([]);

    public static PatternSet Create(IEnumerable<PatternEntry> entries)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(entries);
#else
        ExceptionEx.ThrowIfArgumentNull(entries, nameof(entries));
#endif

        return new([.. entries]);
    }

    internal PatternSet(PatternEntry[] patterns)
    {
        _patterns = patterns;
    }

    private readonly PatternEntry[]? _patterns;

    public int Count => _patterns is not null ? _patterns.Length : 0;

    public bool IsEmpty => _patterns is not { Length: > 0 };

    public bool HasItems => _patterns is { Length: > 0 };

    public PatternEntry this[int index]
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

            if (_patterns is null)
            {
                ExceptionEx.ThrowInvalidOperation("Not initialized");
            }

            return _patterns![index];
        }
    }

    public IEnumerator<PatternEntry> GetEnumerator() => ((IEnumerable<PatternEntry>)(_patterns ?? [])).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Forbid compoundings when there are special patterns at word bound.
    /// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
    internal bool Check(ReadOnlySpan<char> word, int pos, WordEntry r1, WordEntry r2, bool affixed)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        if (_patterns is { Length: > 0 })
        {
            var wordAfterPos = word.Slice(pos);

            foreach (var patternEntry in _patterns)
            {
                if (
                    (
                        patternEntry.Condition.IsZero
                        ||
                        r1.ContainsFlag(patternEntry.Condition)
                    )
                    &&
                    (
                        patternEntry.Condition2.IsZero
                        ||
                        r2.ContainsFlag(patternEntry.Condition2)
                    )
                    &&
                    // zero length pattern => only TESTAFF
                    // zero pattern (0/flag) => unmodified stem (zero affixes allowed)
                    (
                        string.IsNullOrEmpty(patternEntry.Pattern)
                        ||
                        PatternWordCheck(word, pos, patternEntry.Pattern.StartsWith('0') ? r1.Word : patternEntry.Pattern)
                    )
                    &&
                    StringEx.IsSubset(patternEntry.Pattern2, wordAfterPos)
                )
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool PatternWordCheck(ReadOnlySpan<char> word, int pos, string other) =>
        other.Length <= pos
        && word.Slice(pos - other.Length).StartsWith(other.AsSpan());
}

