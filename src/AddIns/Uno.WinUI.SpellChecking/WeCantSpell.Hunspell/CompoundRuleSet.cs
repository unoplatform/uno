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
public readonly struct CompoundRuleSet : IReadOnlyList<CompoundRule>
{
    public static CompoundRuleSet Empty { get; } = new([]);

    public static CompoundRuleSet Create(IEnumerable<CompoundRule> rules)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(rules);
#else
        ExceptionEx.ThrowIfArgumentNull(rules, nameof(rules));
#endif
        return new([.. rules]);
    }

    internal CompoundRuleSet(CompoundRule[] rules)
    {
        _rules = rules;
    }

    private readonly CompoundRule[]? _rules;

    public int Count => _rules is not null ? _rules.Length : 0;

    public bool IsEmpty => _rules is not { Length: > 0 };

    public bool HasItems => _rules is { Length: > 0 };

    public CompoundRule this[int index]
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
            if (_rules is null)
            {
                ExceptionEx.ThrowInvalidOperation("Not initialized");
            }

            return _rules![index];
        }
    }

    public IEnumerator<CompoundRule> GetEnumerator() => ((IEnumerable<CompoundRule>)(_rules ?? [])).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal bool EntryContainsRuleFlags(in FlagSet flags)
    {
        if (flags.HasItems && _rules is { Length: > 0 })
        {
            foreach(var rule in _rules)
            {
                if (rule.ContainsRuleFlagForEntry(flags))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal bool CompoundCheck(IncrementalWordList words, bool all)
    {
        if (_rules is not { Length: > 0 })
        {
            return false;
        }

        var bt = 0;
        var btinfo = new List<MetacharData>(4) { new() };

        foreach (var compoundRule in _rules)
        {
            var pp = 0; // pattern position
            var wp = 0; // "words" position
            var ok = true;
            var ok2 = true;
            do
            {
                while (pp < compoundRule.Count && wp <= words.WNum)
                {
                    if (pp + 1 < compoundRule.Count && compoundRule.IsWildcard(pp + 1))
                    {
                        var wend = compoundRule[pp + 1] == '?' ? wp : words.WNum;
                        ok2 = true;
                        pp += 2;
                        btinfo[bt].btpp = pp;
                        btinfo[bt].btwp = wp;

                        while (wp <= wend)
                        {
                            if (!words.ContainsFlagAt(wp, compoundRule[pp - 2]))
                            {
                                ok2 = false;
                                break;
                            }

                            wp++;
                        }

                        if (wp <= words.WNum)
                        {
                            ok2 = false;
                        }

                        btinfo[bt].btnum = wp - btinfo[bt].btwp;

                        if (btinfo[bt].btnum > 0)
                        {
                            ++bt;
                            btinfo.Add(new MetacharData());
                        }
                        if (ok2)
                        {
                            break;
                        }
                    }
                    else
                    {
                        ok2 = true;
                        if (!words.ContainsFlagAt(wp, compoundRule[pp]))
                        {
                            ok = false;
                            break;
                        }

                        pp++;
                        wp++;

                        if (compoundRule.Count == pp && wp <= words.WNum)
                        {
                            ok = false;
                        }
                    }
                }

                if (ok && ok2)
                {
                    var r = pp;
                    while (
                        compoundRule.Count > r
                        &&
                        r + 1 < compoundRule.Count
                        &&
                        compoundRule.IsWildcard(r + 1)
                    )
                    {
                        r += 2;
                    }

                    if (compoundRule.Count <= r)
                    {
                        return true;
                    }
                }

                // backtrack
                if (bt != 0)
                {
                    do
                    {
                        ok = true;
                        btinfo[bt - 1].btnum--;
                        pp = btinfo[bt - 1].btpp;
                        wp = btinfo[bt - 1].btwp + btinfo[bt - 1].btnum;
                    }
                    while ((btinfo[bt - 1].btnum < 0) && (--bt != 0));
                }

            }
            while (bt != 0);

            if (
                ok
                &&
                ok2
                &&
                (
                    !all
                    ||
                    compoundRule.Count <= pp
                )
            )
            {
                return true;
            }

            // check zero ending
            while (
                ok
                &&
                ok2
                &&
                pp + 1 < compoundRule.Count
                &&
                compoundRule.IsWildcard(pp + 1)
            )
            {
                pp += 2;
            }

            if (
                ok
                &&
                ok2
                &&
                compoundRule.Count <= pp
            )
            {
                return true;
            }
        }

        return false;
    }

    private sealed class MetacharData
    {
        /// <summary>
        /// Metacharacter (*, ?) position for backtracking.
        /// </summary>
        public int btpp;
        /// <summary>
        /// Word position for metacharacters.
        /// </summary>
        public int btwp;
        /// <summary>
        /// Number of matched characters in metacharacter.
        /// </summary>
        public int btnum;
    }
}

