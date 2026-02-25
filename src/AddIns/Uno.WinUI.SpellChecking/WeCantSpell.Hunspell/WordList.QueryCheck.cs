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
using System.Globalization;
using System.Threading;

namespace WeCantSpell.Hunspell;

public partial class WordList
{
    private struct QueryCheck
    {
        public QueryCheck(WordList wordList, QueryOptions? options, CancellationToken cancellationToken)
        {
            _query = new(wordList, options, cancellationToken);
        }

        internal QueryCheck(in Query source)
        {
            _query = new(source);
        }

        private Query _query;

        public readonly WordList WordList => _query.WordList;

        public readonly AffixConfig Affix => _query.Affix;

        public readonly TextInfo TextInfo => _query.TextInfo;

        public readonly QueryOptions Options => _query.Options;

        public readonly int MaxSharps => Options.MaxSharps;

        public bool Check(string word) => CheckDetails(word).Correct;

        public bool Check(ReadOnlySpan<char> word) => CheckDetails(word).Correct;

        private readonly bool CheckNested(ReadOnlySpan<char> word) => new QueryCheck(_query).Check(word);

        public SpellCheckResult CheckDetails(string word)
        {
            if (word is not { Length: > 0 } || word.Length >= Options.MaxWordLen || WordList.IsEmpty)
            {
                return SpellCheckResult.DefaultWrong;
            }

            if (word.StartsWith(Query.DefaultXmlTokenCheckPrefix, StringComparison.Ordinal))
            {
                // Hunspell supports XML input of the simplified API (see manual)
                return SpellCheckResult.DefaultCorrect;
            }

            if (StringEx.IsNumericWord(word))
            {
                // allow numbers with dots, dashes and commas (but forbid double separators: "..", "--" etc.)
                return SpellCheckResult.DefaultCorrect;
            }

            // something very broken if spell ends up calling itself with the same word
            if (_query._spellCandidateStack.Contains(word))
            {
                return SpellCheckResult.DefaultWrong;
            }

            // input conversion
            if (!Affix.InputConversions.TryConvert(word, out var scw))
            {
                scw = word;
            }

            scw = _query.CleanWord2(scw, out var capType, out var abbv);

            if (scw.Length == 0)
            {
                return SpellCheckResult.DefaultWrong;
            }

            CandidateStack.Push(ref _query._spellCandidateStack, word);

            var result = CheckDetailsInternal(scw, capType, abbv != 0);

            CandidateStack.Pop(ref _query._spellCandidateStack);

            return result;
        }

        public SpellCheckResult CheckDetails(ReadOnlySpan<char> word)
        {
            if (word.IsEmpty || word.Length >= Options.MaxWordLen || WordList.IsEmpty)
            {
                return SpellCheckResult.DefaultWrong;
            }

            if (word.StartsWith(Query.DefaultXmlTokenCheckPrefix.AsSpan()))
            {
                // Hunspell supports XML input of the simplified API (see manual)
                return SpellCheckResult.DefaultCorrect;
            }

            if (StringEx.IsNumericWord(word))
            {
                // allow numbers with dots, dashes and commas (but forbid double separators: "..", "--" etc.)
                return SpellCheckResult.DefaultCorrect;
            }

            // something very broken if spell ends up calling itself with the same word
            if (_query._spellCandidateStack.Contains(word))
            {
                return SpellCheckResult.DefaultWrong;
            }

            // input conversion
            CapitalizationType capType;
            int abbv;
            if (Affix.InputConversions.TryConvert(word, out var scw))
            {
                scw = _query.CleanWord2(scw, out capType, out abbv);
            }
            else
            {
                scw = _query.CleanWord2(word, out capType, out abbv);
            }

            if (scw.Length == 0)
            {
                return SpellCheckResult.DefaultWrong;
            }

            // NOTE: because a string isn't formed until this point, scw is pushed instead. It isn't the same, but might be good enough.
            CandidateStack.Push(ref _query._spellCandidateStack, scw);

            var result = CheckDetailsInternal(scw, capType, abbv != 0);

            CandidateStack.Pop(ref _query._spellCandidateStack);

            return result;
        }

        private SpellCheckResult CheckDetailsInternal(string scw, CapitalizationType capType, bool abbv)
        {
            string? root;
            WordEntry? rv;
            var resultType = SpellCheckResultType.None;

            switch (capType)
            {
                case CapitalizationType.HuhInit:
                    resultType |= SpellCheckResultType.OrigCap;
                    goto case CapitalizationType.Huh;

                case CapitalizationType.Huh:
                case CapitalizationType.None:
                    rv = _query.CheckWord(scw, ref resultType, out root);

                    if (rv is null)
                    {
                        if (abbv)
                        {
                            rv = _query.CheckWord(scw + ".", ref resultType, out root);
                        }
                        else
                        {
                            goto handleRvNull;
                        }
                    }

                    break;

                case CapitalizationType.All:
                    rv = CheckDetailsAllCap(abbv, ref scw, ref resultType, out root);
                    if (rv is null)
                    {
                        goto case CapitalizationType.Init;
                    }

                    break;

                case CapitalizationType.Init:
                    rv = CheckDetailsInitCap(abbv, capType, ref scw, ref resultType, out root);
                    break;

                default:
                    root = null;
                    rv = null;
                    break;
            }

            if (rv is not null)
            {
                if (rv.ContainsFlag(Affix.Warn))
                {
                    resultType |= SpellCheckResultType.Warn;

                    if (Affix.ForbidWarn)
                    {
                        return SpellCheckResult.Fail(root, resultType);
                    }
                }

                return SpellCheckResult.Success(root, resultType);
            }

        handleRvNull:

            // recursive breaking at break points
            return (Affix.BreakPoints.HasItems && resultType.IsMissingFlag(SpellCheckResultType.Forbidden))
                ? CheckDetailsInternalBreakPoints(scw, root, resultType)
                : SpellCheckResult.Fail(root, resultType);
        }

        private readonly SpellCheckResult CheckDetailsInternalBreakPoints(string scw, string? root, SpellCheckResultType resultType)
        {
            // calculate break points for recursion limit
            if (Affix.BreakPoints.FindRecursionLimit(scw) >= 10)
            {
                return SpellCheckResult.Fail(root, resultType);
            }

            // check boundary patterns (^begin and end$)
            foreach (var breakEntry in Affix.BreakPoints.RawArray)
            {
                if (CheckDetailsInternalBreakPoints_BoundaryCheck(scw, breakEntry))
                {
                    return SpellCheckResult.CompoundSuccess(root, resultType);
                }
            }

            if (scw.Length > 2)
            {
                List<(string breakEntry, int found)>? reSearch = null;
                // other patterns
                foreach (var breakEntry in Affix.BreakPoints.RawArray)
                {
                    var found = scw.IndexOf(breakEntry, 1, scw.Length - 2, StringComparison.Ordinal);
                    if (found >= 0)
                    {
                        (reSearch ??= []).Add((breakEntry, found));

                        // try to break at the second occurance
                        // to recognize dictionary words with wordbreak
                        if (scw.Length - found > 2)
                        {
                            found = Math.Max(scw.IndexOf(breakEntry, found + 1, scw.Length - 2 - found, StringComparison.Ordinal), found);
                        }

                        if (CheckDetailsInternalBreakPoints_OtherPatternCheck(scw, breakEntry, found))
                        {
                            return SpellCheckResult.CompoundSuccess(root, resultType);
                        }
                    }
                }

                if (reSearch is not null)
                {
                    // other patterns (break at first break point)
                    foreach (var (breakEntry, found) in reSearch)
                    {
                        if (CheckDetailsInternalBreakPoints_OtherPatternCheck(scw, breakEntry, found))
                        {
                            return SpellCheckResult.CompoundSuccess(root, resultType);
                        }
                    }
                }
            }

            return SpellCheckResult.Fail(root, resultType);
        }

        private readonly bool CheckDetailsInternalBreakPoints_BoundaryCheck(string scw, string breakEntry)
        {
            if (breakEntry.Length > 1 && breakEntry.Length <= scw.Length)
            {
                var pLastIndex = breakEntry.Length - 1;
                if (
                    breakEntry[0] == '^'
                    && scw.AsSpan(0, pLastIndex).SequenceEqual(breakEntry.AsSpan(1))
                    && CheckNested(scw.AsSpan(pLastIndex))
                )
                {
                    return true;
                }

                if (
                    breakEntry[pLastIndex] == '$'
                    && scw.AsSpan(scw.Length - pLastIndex).SequenceEqual(breakEntry.AsSpan(0, pLastIndex))
                    && CheckNested(scw.AsSpan(0, scw.Length - pLastIndex))
                )
                {
                    return true;
                }
            }

            return false;
        }

        private readonly bool CheckDetailsInternalBreakPoints_OtherPatternCheck(string scw, string breakEntry, int found)
        {
            if (CheckNested(scw.AsSpan(found + breakEntry.Length)))
            {
                // examine 2 sides of the break point
                if (CheckNested(scw.AsSpan(0, found)))
                {
                    return true;
                }

                // LANG_hu: spec. dash rule
                if (Affix.IsHungarian && "-".Equals(breakEntry, StringComparison.Ordinal))
                {
                    if (CheckNested(scw.AsSpan(0, found + 1)))
                    {
                        return true; // check the first part with dash
                    }
                }
            }

            return false;
        }

        private WordEntry? CheckDetailsAllCap(bool abbv, ref string scw, ref SpellCheckResultType resultType, out string? root)
        {
            resultType |= SpellCheckResultType.OrigCap;
            var rv = _query.CheckWord(scw, ref resultType, out root);
            if (rv is not null)
            {
                return rv;
            }

            if (abbv)
            {
                rv = _query.CheckWord(scw + ".", ref resultType, out root);
                if (rv is not null)
                {
                    return rv;
                }
            }

            // Spec. prefix handling for Catalan, French, Italian:
            // prefixes separated by apostrophe (SANT'ELIA -> Sant'+Elia).
            var textInfo = TextInfo;
            var apos = scw.IndexOf('\'');
            if (apos >= 0)
            {
                scw = StringEx.MakeAllSmall(scw, textInfo);

                // conversion may result in string with different len than before MakeAllSmall2 so re-scan
                if (apos < scw.Length - 1)
                {
                    scw = StringEx.ConcatString(scw.AsSpan(0, apos + 1), textInfo.ToUpper(scw[apos + 1]), scw.AsSpan(apos + 2));
                    rv = _query.CheckWord(scw, ref resultType, out root);
                    if (rv is not null)
                    {
                        return rv;
                    }

                    scw = StringEx.MakeInitCap(scw, textInfo);
                    rv = _query.CheckWord(scw, ref resultType, out root);
                    if (rv is not null)
                    {
                        return rv;
                    }
                }
            }

            if (Affix.CheckSharps && scw.Contains("SS"))
            {
                scw = StringEx.MakeAllSmall(scw, textInfo);
                var u8buffer = scw;
                rv = SpellSharps(ref u8buffer, ref resultType, out root);
                if (rv is null)
                {
                    scw = StringEx.MakeInitCap(scw, textInfo);
                    rv = SpellSharps(ref scw, ref resultType, out root);
                }

                if (abbv && rv is null)
                {
                    u8buffer += ".";
                    rv = SpellSharps(ref u8buffer, ref resultType, out root);
                    if (rv is null)
                    {
                        u8buffer = scw + ".";
                        rv = SpellSharps(ref u8buffer, ref resultType, out root);
                    }
                }
            }

            return rv;
        }

        private WordEntry? CheckDetailsInitCap(bool abbv, CapitalizationType capType, ref string scw, ref SpellCheckResultType resultType, out string? root)
        {
            var u8buffer = new StringBuilderSpan(scw.Length + 1);
            u8buffer.AppendLower(scw, Affix.Culture);

            scw = u8buffer.ToStringInitCap(TextInfo);

            resultType |= SpellCheckResultType.OrigCap;
            if (capType == CapitalizationType.Init)
            {
                resultType |= SpellCheckResultType.InitCap;
            }

            var rv = _query.CheckWord(scw, ref resultType, out root);

            if (capType == CapitalizationType.Init)
            {
                resultType &= ~SpellCheckResultType.InitCap;
            }

            // forbid bad capitalization
            // (for example, ijs -> Ijs instead of IJs in Dutch)
            // use explicit forms in dic: Ijs/F (F = FORBIDDENWORD flag)

            if (resultType.HasFlagEx(SpellCheckResultType.Forbidden))
            {
                u8buffer.Dispose();
                return null;
            }

            if (capType == CapitalizationType.All && rv is not null && IsKeepCase(rv))
            {
                rv = null;
            }

            if (rv is not null || (!Affix.CultureUsesDottedI && scw.StartsWith('İ')))
            {
                u8buffer.Dispose();
                return rv;
            }

            rv = _query.CheckWord(u8buffer.ToString(), ref resultType, out root);

            if (abbv && rv is null)
            {
                u8buffer.Append('.');
                rv = _query.CheckWord(u8buffer.ToString(), ref resultType, out root);
                if (rv is null)
                {
                    u8buffer.Set(scw);
                    u8buffer.Append('.');
                    if (capType == CapitalizationType.Init)
                    {
                        resultType |= SpellCheckResultType.InitCap;
                    }

                    rv = _query.CheckWord(u8buffer.ToString(), ref resultType, out root);

                    if (capType == CapitalizationType.Init)
                    {
                        resultType &= ~SpellCheckResultType.InitCap;
                    }
                    else if (capType == CapitalizationType.All && rv is not null && IsKeepCase(rv))
                    {
                        rv = null;
                    }

                    u8buffer.Dispose();
                    return rv;
                }
            }

            if (
                rv is not null
                &&
                IsKeepCase(rv)
                &&
                (
                    capType == CapitalizationType.All
                    ||
                    // if CHECKSHARPS: KEEPCASE words with \xDF  are allowed in INITCAP form, too.
                    !(Affix.CheckSharps && u8buffer.Contains('ß'))
                )
            )
            {
                rv = null;
            }

            u8buffer.Dispose();
            return rv;
        }

        /// <summary>
        /// Recursive search for right ss - sharp s permutations
        /// </summary>
        private WordEntry? SpellSharps(ref string @base, ref SpellCheckResultType info, out string? root) =>
            SpellSharps(ref @base, 0, 0, 0, ref info, out root);

        /// <summary>
        /// Recursive search for right ss - sharp s permutations
        /// </summary>
        private WordEntry? SpellSharps(ref string @base, int nPos, int n, int repNum, ref SpellCheckResultType info, out string? root)
        {
            var pos = @base.IndexOf("ss", nPos, StringComparison.Ordinal);
            if (pos >= 0 && n < MaxSharps)
            {
                WordEntry? h;

                using (var baseBuilder = new StringBuilderSpan(@base))
                {
                    baseBuilder[pos] = 'ß';
                    baseBuilder.Remove(pos + 1, 1);
                    @base = baseBuilder.ToString();

                    h = SpellSharps(ref @base, pos + 1, n + 1, repNum + 1, ref info, out root);
                    if (h is not null)
                    {
                        return h;
                    }

                    baseBuilder.Set(@base);
                    baseBuilder[pos] = 's';
                    baseBuilder.Insert(pos + 1, 's');
                    @base = baseBuilder.ToString();
                }

                h = SpellSharps(ref @base, pos + 2, n + 1, repNum, ref info, out root);
                if (h is not null)
                {
                    return h;
                }
            }
            else if (repNum > 0)
            {
                return _query.CheckWord(@base, ref info, out root);
            }

            root = null;
            return null;
        }

        private readonly bool IsKeepCase(WordEntry rv) => rv.ContainsFlag(Affix.KeepCase);
    }
}

