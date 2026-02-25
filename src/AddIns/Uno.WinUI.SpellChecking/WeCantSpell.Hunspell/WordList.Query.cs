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
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WeCantSpell.Hunspell;

public partial class WordList
{
    private struct Query
    {
        internal const string DefaultXmlToken = "<?xml?>";
        internal const string DefaultXmlTokenCheckPrefix = "<?xml";

        private static readonly QueryOptions DefaultOptions = new();

        internal Query(WordList wordList, QueryOptions? options, CancellationToken cancellationToken)
        {
            WordList = wordList;
            Affix = wordList._affix;
            TextInfo = Affix.Culture.TextInfo;
            Options = options ?? DefaultOptions;
            CancellationToken = cancellationToken;
            _spellCandidateStack = new();
            _prefix = null;
            //PrefixAppend = null;
            _suffix = null;
            _suffixFlag = default;
            _suffixExtra = false;
            _suffixAppend = null;
        }

        internal Query(in Query source)
        {
            WordList = source.WordList;
            Affix = source.WordList._affix;
            TextInfo = Affix.Culture.TextInfo;
            Options = source.Options;
            CancellationToken = source.CancellationToken;
            _spellCandidateStack = source._spellCandidateStack;
            _prefix = null;
            //PrefixAppend = null;
            _suffix = null;
            _suffixFlag = default;
            _suffixExtra = false;
            _suffixAppend = null;
        }

        public readonly WordList WordList;

        public readonly AffixConfig Affix;

        public readonly QueryOptions Options;

        public readonly TextInfo TextInfo;

        /// <summary>
        /// A cancellation token that can be used to request the termination of a check or suggest operation.
        /// </summary>
        /// <remarks>
        /// Note that when cancellation is requested, operations may still take some time to stop.
        /// Cancellation should never result in an exception from a check or suggest query but may
        /// instead lead to incomplete results. Using cancellation can further impact the consistency
        /// of results. Even without cancellation, the consistency of results can't be gauranteed
        /// due to the use of timing checks throughout the code.
        /// </remarks>
        public readonly CancellationToken CancellationToken;

        internal CandidateStack _spellCandidateStack;

        private PrefixEntry? _prefix;

        //private string? PrefixAppend { get; set; } // Previous prefix for counting syllables of the prefix.

        private SuffixEntry? _suffix;

        private FlagValue _suffixFlag;

        /// <summary>
        /// Modifier for syllable count of <see cref="_suffixAppend"/>.
        /// </summary>
        private bool _suffixExtra;

        /// <summary>
        /// Previous suffix for counting syllables of the suffix.
        /// </summary>
        private string? _suffixAppend;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearPrefix()
        {
            _prefix = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearSuffix()
        {
            _suffix = null;
        }

        private void ClearAffixes()
        {
            ClearPrefix();
            ClearSuffix();
        }

        private void ClearSuffixAndFlag()
        {
            ClearSuffix();
            _suffixFlag = default;
        }

        private void ClearSuffixAppendAndExtra()
        {
            _suffixAppend = null;
            _suffixExtra = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearAllAppendAndExtra()
        {
            // _prefixAppend = null;
            ClearSuffixAppendAndExtra();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetPrefix(PrefixEntry prefix)
        {
            _prefix = prefix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetSuffix(SuffixEntry suffix)
        {
            _suffix = suffix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetSuffixFlag(FlagValue flag)
        {
            _suffixFlag = flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetSuffixExtra(bool extra)
        {
            _suffixExtra = extra;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetSuffixAppend(string append)
        {
            _suffixAppend = append;
        }

        private readonly bool AffixContainsContClass(FlagValue value) =>
            value.HasValue
            &&
            (
                (_prefix is not null && _prefix.ContainsContClass(value))
                ||
                (_suffix is not null && _suffix.ContainsContClass(value))
            );

        private readonly bool AffixContainsAnyContClass(FlagSet values) =>
            (_prefix is not null && _prefix.ContainsAnyContClass(values))
            ||
            (_suffix is not null && _suffix.ContainsAnyContClass(values));

        public WordEntry? CheckWord(string word, ref SpellCheckResultType info, out string? root)
        {
            root = null;

            if (word.Length == 0)
            {
                return null;
            }

            // remove IGNORE characters from the string
            if (Affix.IgnoredChars.HasItems)
            {
                word = Affix.IgnoredChars.RemoveChars(word);

                if (word.Length == 0)
                {
                    return null;
                }
            }

            // word reversing wrapper for complex prefixes
            if (Affix.ComplexPrefixes)
            {
                word = word.GetReversed();
            }

            // look word in hash table
            if (!TryCheckWord_HashTable(word, ref info, out var he))
            {
                // check with affixes

                // try stripping off affixes
                he = AffixCheck(word, default, CompoundOptions.Not);

                // check compound restriction and onlyupcase
                if (
                    he is not null
                    &&
                    (
                        info.HasFlagEx(SpellCheckResultType.InitCap)
                            ? he.ContainsAnyFlags(Affix.Flags_OnlyInCompound_OnlyUpcase)
                            : he.ContainsFlag(Affix.OnlyInCompound)
                    )
                )
                {
                    he = null;
                }

                if (he is not null)
                {
                    if (he.ContainsFlag(Affix.ForbiddenWord))
                    {
                        info |= SpellCheckResultType.Forbidden;

                        return null;
                    }

                    root = he.Word;
                    if (Affix.ComplexPrefixes)
                    {
                        root = root.GetReversed();
                    }
                }
                else if (Affix.HasCompound)
                {
                    // try check compound word
                    TryCheckWord_Compound(word, ref info, out he);

                    if (he is not null)
                    {
                        root = he.Word;
                        if (Affix.ComplexPrefixes)
                        {
                            root = root.GetReversed();
                        }

                        info |= SpellCheckResultType.Compound;
                    }
                }
            }

            return he;
        }

        /// <summary>
        /// look word in hash table
        /// </summary>
        /// <returns><c>true</c> when result should be used</returns>
        private readonly bool TryCheckWord_HashTable(string word, ref SpellCheckResultType info, out WordEntry? result)
        {
            if (WordList.TryGetEntryDetailsByRootWord(word, out var details))
            {
                // check forbidden and onlyincompound words
                if (TryCheckWord_HashTableIsForbidden(in details[0], ref info))
                {
                    result = null;
                    return true;
                }

                return details.Length == 1
                    ? TryCheckWord_HashTableFoundSingle(word, in details[0], ref info, out result)
                    : TryCheckWord_HashTableFoundMany(word, details, ref info, out result);
            }

            result = null;
            return false;
        }

        private readonly bool TryCheckWord_HashTableFoundSingle(string word, in WordEntryDetail heDetails, ref SpellCheckResultType info, out WordEntry? result)
        {
            // he = next not needaffix, onlyincompound homonym or onlyupcase word
            if (heDetails.ContainsAnyFlags(info.HasFlagEx(SpellCheckResultType.InitCap) ? Affix.Flags_NeedAffix_OnlyInCompound_OnlyUpcase : Affix.Flags_NeedAffix_OnlyInCompound))
            {
                result = null;
                return false;
            }

            result = new WordEntry(word, heDetails);
            return true;
        }

        private readonly bool TryCheckWord_HashTableFoundMany(string word, WordEntryDetail[] details, ref SpellCheckResultType info, out WordEntry? result)
        {
            var heFlags = info.HasFlagEx(SpellCheckResultType.InitCap) ? Affix.Flags_NeedAffix_OnlyInCompound_OnlyUpcase : Affix.Flags_NeedAffix_OnlyInCompound;

            // he = next not needaffix, onlyincompound homonym or onlyupcase word
            var heIndex = 0;
            ref readonly var heDetails = ref details[0];
            while (heDetails.ContainsAnyFlags(heFlags))
            {
                heIndex++;

                if (heIndex >= details.Length)
                {
                    result = null;
                    return false;
                }

                heDetails = ref details[heIndex];
            }

            result = new WordEntry(word, heDetails);
            return true;
        }

        private readonly bool TryCheckWord_HashTableIsForbidden(in WordEntryDetail heDetails, ref SpellCheckResultType info)
        {
            if (heDetails.ContainsFlag(Affix.ForbiddenWord))
            {
                info |= SpellCheckResultType.Forbidden;

                if (Affix.IsHungarian && heDetails.ContainsFlag(Affix.CompoundFlag))
                {
                    info |= SpellCheckResultType.Compound;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// try check compound word
        /// </summary>
        private void TryCheckWord_Compound(string word, ref SpellCheckResultType info, out WordEntry? he)
        {
            // try check compound word
            var rwords = IncrementalWordList.GetRoot();

            // first allow only 2 words in the compound
            he = CompoundCheck(word, 0, 0, 100, rwords, huMovRule: false, isSug: false, info | SpellCheckResultType.Compound2);

            // NOTE: This unset is left in case the input has Compound2
            info &= ~SpellCheckResultType.Compound2; // unset Compound2

#if DEBUG
            // NOTE: A lot of the code around the Compound2 seems redundant and might be a bug in origin
            // These debug checks might help alert to any issues if implementations change or bugs get fixed
            if (info.HasFlag(SpellCheckResultType.Compound2)) ExceptionEx.ThrowInvalidOperation();
#endif

            // if not 2-word compoud word, try with 3 or more words
            // (only if original info didn't forbid it)
            if (he is null)
            {
                // NOTE: This full condition should never be needed because Compound2 is always unset above
                // if (!he && info && !(*info & SPELL_COMPOUND_2))

#if DEBUG
                // NOTE: This line was skipped in the port as it seems redundant considering the check
                // *info &= ~SPELL_COMPOUND_2;
                if (info.HasFlag(SpellCheckResultType.Compound2)) ExceptionEx.ThrowInvalidOperation();
#endif

                he = CompoundCheck(word, 0, 0, 100, rwords, huMovRule: false, isSug: false, info);
                // accept the compound with 3 or more words only if it is
                // - not a dictionary word with a typo and
                // - not two words written separately,
                // - or if it's an arbitrary number accepted by compound rules (e.g. 999%)
                if (he is not null && word.Length > 0 && !char.IsDigit(word[0]))
                {
                    var querySuggest = new QuerySuggest(WordList, Options, CancellationToken, testSimpleSuggestion: true);
                    var onlyCompoundSug = false;
                    if (querySuggest.Suggest([], word, ref onlyCompoundSug))
                    {
                        he = null;
                    }
                }
            }

            if (he is null && word.EndsWith('-') && Affix.IsHungarian)
            {
                // LANG_hu section: `moving rule' with last dash
                he = CompoundCheck(word.AsSpan(0, word.Length - 1), -5, 0, 100, rwords, huMovRule: true, isSug: false, info);
            }

            IncrementalWordList.ReturnRoot(ref rwords);
        }

        private readonly WordEntry? HomonymWordSearch(ReadOnlySpan<char> homonymWord, IncrementalWordList? words, FlagValue condition2, bool scpdIsZero)
        {
            // perhaps without prefix
            if (TryLookupDetails(homonymWord, out var homonymWordString, out var details) && details.Length > 0)
            {
                // search homonym with compound flag
                foreach (var homonymCandidate in details)
                {
                    if (
                        homonymCandidate.DoesNotContainFlag(Affix.NeedAffix)
                        &&
                        (
                            words is null
                            ? homonymCandidate.ContainsAnyFlags(Affix.Flags_CompoundFlag_CompoundEnd)
                            : (Affix.CompoundRules.HasItems && DefCompoundCheck(words.CreateIncremented(), homonymCandidate, true))
                        )
                        &&
                        (
                            scpdIsZero
                            || !condition2.HasValue
                            || homonymCandidate.ContainsFlag(condition2)
                        )
                    )
                    {
                        return homonymCandidate.ToEntry(homonymWordString);
                    }
                }
            }

            return null;
        }

        public WordEntry? CompoundCheck(ReadOnlySpan<char> word, int wordNum, int numSyllable, int maxwordnum, IncrementalWordList rwords, bool huMovRule, bool isSug, SpellCheckResultType info)
        {
            // add a time limit to handle possible
            // combinatorical explosion of the overlapping words
            var opLimiter = new OperationTimedLimiter(Options.TimeLimitCompoundCheck, CancellationToken);
            return CompoundCheck(word, wordNum, numSyllable, maxwordnum, null, rwords, huMovRule, isSug, info, ref opLimiter);
        }

        public WordEntry? CompoundCheck(ReadOnlySpan<char> word, int wordNum, int numSyllable, int maxwordnum, IncrementalWordList? words, IncrementalWordList rwords, bool huMovRule, bool isSug, SpellCheckResultType info, ref OperationTimedLimiter opLimiter)
        {
            if (wordNum != 0)
            {
                // Reduce the number of clock checks by querying for cancellation once per method invocation
                opLimiter.QueryForCancellation();
            }

            WordEntry? rv;
            var oldWords = words;
            var len = word.Length;

            // setcminmax
            var cMin = Affix.CompoundMin;
            var cMax = len - cMin + 1;
            var scpd = 0;

            var oldWordNum = wordNum;
            var oldNumSyllable = numSyllable;
            var oldIndex = 0;
            var oldCMin = 0;
            var oldCMax = 0;
            var oldLen = 0;

            var ch = '\0';

            var checkedSimplifiedTriple = false;

            var st = new SimulatedCString(word);

            for (var i = cMin; i < cMax; i++)
            {
                words = oldWords;
                var onlyCpdRule = words is not null;

                do // onlycpdrule loop
                {
                    var checkedPrefix = false;

                    do // simplified checkcompoundpattern loop
                    {
                        if (opLimiter.HasBeenCanceled)
                        {
                            goto resultNull;
                        }

                        FlagValue scpdPatternEntryCondition = default;
                        FlagValue scpdPatternEntryCondition2 = default;
                        if (scpd > 0)
                        {
                            for (; scpd <= Affix.CompoundPatterns.Count && Affix.CompoundPatterns[scpd - 1].Pattern3DoesNotMatch(word, i); scpd++) ;

                            if (scpd > Affix.CompoundPatterns.Count)
                            {
                                break; // break simplified checkcompoundpattern loop
                            }

                            var scpdPatternEntry = Affix.CompoundPatterns[scpd - 1];

                            oldIndex = i;
                            i += scpdPatternEntry.Pattern.Length;
                            oldLen = len;
                            len += scpdPatternEntry.Pattern.Length + scpdPatternEntry.Pattern2.Length + scpdPatternEntry.Pattern3.Length;

                            st.WriteChars(scpdPatternEntry.Pattern, oldIndex);
                            st.WriteChars(scpdPatternEntry.Pattern2, i);
                            st.WriteChars(word.Slice(oldIndex + scpdPatternEntry.Pattern3.Length), i + scpdPatternEntry.Pattern2.Length);

                            oldCMin = cMin;
                            oldCMax = cMax;
                            cMin = Affix.CompoundMin;
                            cMax = len - cMin + 1;

                            scpdPatternEntryCondition = scpdPatternEntry.Condition;
                            scpdPatternEntryCondition2 = scpdPatternEntry.Condition2;
                        }

                        if (st.BufferLength < i)
                        {
                            // abandon early on dubious pattern replacement outcome
                            goto resultNull;
                        }

                        ch = st.ExchangeWithNull(i);

                        ClearAffixes();

                        // FIRST WORD

                        {
                            // perhaps without prefix
                            if (TryLookupDetails(st.TerminatedSpan, out var searchEntryWord, out var searchEntryDetails))
                            {
#if DEBUG
                                if (searchEntryDetails.Length == 0) ExceptionEx.ThrowInvalidOperation();
#endif

                                if (huMovRule)
                                {
                                    rv = searchEntryDetails[0].ToEntry(searchEntryWord);
                                }
                                else
                                {
                                    // forbid dictionary stems with COMPOUNDFORBIDFLAG in
                                    // compound words, overriding the effect of COMPOUNDPERMITFLAG
                                    if (searchEntryDetails[0].ContainsFlag(Affix.CompoundForbidFlag))
                                    {
                                        if (!onlyCpdRule && Affix.SimplifiedCompound) // would_continue
                                        {
                                            if (scpd == 0)
                                            {
                                                // given the while conditions that continue jumps to, this situation never ends
                                                // TODO: HUNSPELL_WARNING(stderr, "break infinite loop\n");
                                                break;
                                            }

                                            if (scpd > 0)
                                            {
                                                // under these conditions we loop again, but the assumption above
                                                // appears to be that cmin and cmax are the original values they
                                                // had in the outside loop
                                                cMin = oldCMin;
                                                cMax = oldCMax;
                                            }
                                        }

                                        continue;
                                    }

                                    if (onlyCpdRule)
                                    {
                                        if (Affix.CompoundRules.HasItems && (wordNum == 0 || words is not null))
                                        {
                                            rv = CompoundCheckWordSearch_OnlyCpdRule(
                                                searchEntryWord,
                                                searchEntryDetails,
                                                scpdPatternEntryCondition,
                                                ref words,
                                                rwords);
                                        }
                                        else
                                        {
                                            rv = null;
                                        }
                                    }
                                    else if (Affix.CompoundFlag.IsZero && Affix.CompoundBegin.IsZero && Affix.CompoundMiddle.IsZero)
                                    {
                                        rv = null;
                                    }
                                    else
                                    {
                                        if (words is null)
                                        {
                                            rv = CompoundCheckWordSearch_NormalNoWordList(
                                                searchEntryWord,
                                                searchEntryDetails,
                                                scpdPatternEntryCondition,
                                                wordNum == 0 ? Affix.CompoundBegin : Affix.CompoundMiddle);
                                        }
                                        else if (wordNum == 0)
                                        {
                                            rv = CompoundCheckWordSearch_NormalWithExistingWordList(
                                                searchEntryWord,
                                                searchEntryDetails,
                                                scpdPatternEntryCondition);
                                        }
                                        else
                                        {
                                            rv = null;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                rv = null;
                            }
                        }

                        var affixed = false;

                        if (rv is null)
                        {
                            if (onlyCpdRule)
                            {
                                break;
                            }

                            affixed = true;

                            if (Affix.CompoundFlag.HasValue)
                            {
                                rv = CompoundCheckWordSearch_AffixCheckCompoundFlag(st.TerminatedSpan, huMovRule);
                            }

                            if (rv is not null)
                            {
                                checkedPrefix = true;
                            }
                            else if (wordNum == 0)
                            {
                                if (Affix.CompoundBegin.HasValue)
                                {
                                    rv = CompoundCheckWordSearch_AffixCheckCompoundBegin(st.TerminatedSpan, huMovRule);
                                    checkedPrefix |= rv is not null;
                                }
                            }
                            else if (wordNum > 0)
                            {
                                if (Affix.CompoundMiddle.HasValue)
                                {
                                    rv = CompoundCheckWordSearch_AffixCheckCompoundMiddle(st.TerminatedSpan, huMovRule);
                                    checkedPrefix |= rv is not null;
                                }
                            }
                        }
                        else
                        {
                            if (rv.ContainsAnyFlags(isSug
                                ? Affix.Flags_NeedAffix_ForbiddenWord_OnlyUpcase_NoSuggest
                                : Affix.Flags_NeedAffix_ForbiddenWord_OnlyUpcase))
                            {
                                // else check forbiddenwords and needaffix
                                st[i] = ch;

                                break;
                            }
                        }

                        if (rv is not null)
                        {
                            if (
                                !huMovRule
                                &&
                                (
                                    checkedPrefix
                                        ? AffixContainsContClass(Affix.CompoundForbidFlag) // check non_compound flag in suffix and prefix
                                        : wordNum == 0
                                            // check non_compound flag in suffix and prefix
                                            // check compoundend flag in suffix and prefix
                                            // check compoundmiddle flag in suffix and prefix
                                            ? AffixContainsAnyContClass(Affix.Flags_CompoundForbid_CompoundMiddle_CompoundEnd)
                                            // check non_compound flag in suffix and prefix
                                            // check compoundend flag in suffix and prefix
                                            : AffixContainsAnyContClass(Affix.Flags_CompoundForbid_CompoundEnd)
                                )
                            )
                            {
                                rv = null;
                            }
                            else
                            {

                                // check forbiddenwords
                                if (rv.ContainsAnyFlags(isSug ? Affix.Flags_ForbiddenWord_OnlyUpcase_NoSuggest : Affix.Flags_ForbiddenWord_OnlyUpcase))
                                {
                                    goto resultNull;
                                }

                                // increment word number, if the second root has a compoundroot flag
                                if (rv.ContainsFlag(Affix.CompoundRoot))
                                {
                                    wordNum++;
                                }

                                if (
                                    (
                                        !checkedPrefix
                                        &&
                                        (words is null || !words.CheckIfCurrentIsNotNull())
                                        &&
                                        rv.DoesNotContainAnyFlags(oldWordNum == 0 ? Affix.Flags_CompoundFlag_CompoundBegin : Affix.Flags_CompoundFlag_CompoundMiddle)
                                        &&
                                        (
                                            !huMovRule // LANG_hu section: spec. Hungarian rule
                                            ||
                                            !Affix.IsHungarian // XXX hardwired Hungarian dictionary codes
                                            ||
                                            rv.DoesNotContainAnyFlags(SpecialFlags.SetFGH)
                                        ) // END of LANG_hu section
                                    )
                                    || // test CHECKCOMPOUNDPATTERN conditions
                                    (
                                        scpd == 0
                                        ? (
                                            words is null
                                            &&
                                            (
                                                (Affix.CheckCompoundTriple && compoundTripleCheck(word, i))
                                                ||
                                                (Affix.CheckCompoundCase && compoundCaseCheck(word, i))
                                            )
                                        )
                                        : (
                                            scpdPatternEntryCondition.HasValue
                                            &&
                                            rv.DoesNotContainFlag(scpdPatternEntryCondition)
                                        )
                                    )
                                )
                                {
                                    rv = null;
                                }

                                static bool compoundTripleCheck(ReadOnlySpan<char> word, int i)
                                {
#if DEBUG
                                    ExceptionEx.ThrowIfArgumentEmpty(word, nameof(word));
#endif

                                    // test triple letters
                                    return
                                        i > 0
                                        &&
                                        i < word.Length
                                        &&
                                        word[i - 1] == word[i]
                                        &&
                                        (
                                            (i >= 2 && word[i - 1] == word[i - 2])
                                            ||
                                            (i + 1 < word.Length && word[i - 1] == word[i + 1]) // may be word[i+1] == '\0'
                                        );
                                }

                                static bool compoundCaseCheck(ReadOnlySpan<char> word, int pos)
                                {
#if DEBUG
                                    ExceptionEx.ThrowIfArgumentEmpty(word, nameof(word));
#endif

                                    // Forbid compounding with neighbouring upper and lower case characters at word bounds.
                                    // NOTE: this implementation could be much simpler but an attempt is made here
                                    // to preserve the same result when indexes may be out of bounds
                                    var hasUpper = false;
                                    char c;

                                    if (pos < word.Length)
                                    {
                                        if (pos > 0)
                                        {
                                            c = word[pos - 1];

                                            if (c == '-')
                                            {
                                                return false;
                                            }

                                            if (char.IsUpper(c))
                                            {
                                                hasUpper = true;
                                            }
                                        }

                                        c = word[pos];

                                        if (c == '-')
                                        {
                                            return false;
                                        }

                                        if (!hasUpper && char.IsUpper(c))
                                        {
                                            hasUpper = true;
                                        }
                                    }

                                    return hasUpper;
                                }
                            }
                        }
                        else if (huMovRule && Affix.IsHungarian)
                        {
                            rv = AffixCheck(st.TerminatedSpan, default, CompoundOptions.Not);

                            if(
                                rv is not null
                                &&
                                _suffix is not null
                                &&
                                _suffix.ContainsAnyContClass(SpecialFlags.SetXPercent) // XXX hardwired Hungarian dic. codes
                            )
                            {
                                rv = null;
                            }
                        }

                        // first word is acceptable in compound words?
                        if (rv is not null)
                        {
                            // first word is ok condition
                            if (Affix.IsHungarian)
                            {
                                CompoundCheckHungarianAffix1(st.TerminatedSpan.Limit(i), ref wordNum, ref numSyllable);
                            }

#if DEBUG
                            if (rv is null) ExceptionEx.ThrowInvalidOperation();
#endif

                            // NEXT WORD(S)
                            var rvFirst = rv;
                            var simplifiedTripple = false;

                            st[i] = ch;

                            do
                            {
                                // striple loop

                                // check simplifiedtriple
                                if (Affix.SimplifiedTriple)
                                {
                                    if (simplifiedTripple)
                                    {
                                        checkedSimplifiedTriple = true;
                                        i--; // check "fahrt" instead of "ahrt" in "Schiffahrt"
                                    }
                                    else if (i > 2 && i <= word.Length && word[i - 1] == word[i - 2])
                                    {
                                        simplifiedTripple = true;
                                    }
                                }

                                // NOTE: st.TerminatedSpan should terminate at its full length, but we can't know that for sure
                                rv = HomonymWordSearch(st.SliceToTerminatorFromOffset(i), words, scpdPatternEntryCondition2, scpd == 0);

                                if (rv is not null)
                                {
                                    // check FORCEUCASE
                                    if (info.IsMissingFlag(SpellCheckResultType.OrigCap) && rv.ContainsFlag(Affix.ForceUpperCase))
                                    {
                                        rv = null;
                                    }
                                    else if (words is not null && words.CheckIfNextIsNotNull())
                                    {
                                        rv = rvFirst;
                                        goto resultRv;
                                    }
                                    else
                                    {
                                        switch (CompoundCheck_TrySecondRoot(rvFirst!, rv, word, i, len, tmpNumSyllable: numSyllable, tmpWordNum: wordNum, scpd: scpd, scpdPatternEntryCondition2, isSug))
                                        {
                                            case CompoundCheckOutcomes.Fail:
                                                goto resultNull;

                                            case CompoundCheckOutcomes.Permit:
                                                rv = rvFirst;
                                                goto resultRv;
                                        }
                                    }
                                }

                                // perhaps second word has prefix or/and suffix
                                ClearSuffixAndFlag();

                                if (i < word.Length)
                                {
                                    rv = onlyCpdRule || Affix.CompoundFlag.IsZero
                                        ? null
                                        : AffixCheck(word.Slice(i), Affix.CompoundFlag, CompoundOptions.End);

                                    if (rv is null && Affix.CompoundEnd.HasValue && !onlyCpdRule)
                                    {
                                        ClearAffixes();
                                        rv = AffixCheck(word.Slice(i), Affix.CompoundEnd, CompoundOptions.End);
                                    }

                                    if (rv is null && Affix.CompoundRules.HasItems && words is not null)
                                    {
                                        rv = AffixCheck(word.Slice(i), default, CompoundOptions.End);

                                        if (rv is not null)
                                        {
                                            if (DefCompoundCheck(words.CreateIncremented(), rv.Detail, true))
                                            {
                                                rv = rvFirst;
                                                goto resultRv;
                                            }

                                            rv = null;
                                        }

                                    }
                                }
                                else
                                {
                                    rv = null;
                                }

                                if (rv is not null)
                                {
                                    if (
                                        // check FORCEUCASE
                                        (
                                            info.IsMissingFlag(SpellCheckResultType.OrigCap)
                                            &&
                                            rv.ContainsFlag(Affix.ForceUpperCase)
                                        )
                                        ||
                                        // check non_compound flag in suffix and prefix
                                        AffixContainsContClass(Affix.CompoundForbidFlag)
                                        ||
                                        (
                                            scpd != 0
                                            // test CHECKCOMPOUNDPATTERN conditions (allowed forms)
                                            ? (
                                                scpdPatternEntryCondition2.HasValue
                                                &&
                                                rv.DoesNotContainFlag(scpdPatternEntryCondition2)
                                            )
                                            // test CHECKCOMPOUNDPATTERN conditions (forbidden compounds)
                                            : (
                                                Affix.CompoundPatterns.HasItems
                                                &&
                                                Affix.CompoundPatterns.Check(word, i, rvFirst!, rv, affixed)
                                            )
                                        )
                                    )
                                    {
                                        rv = null;
                                    }
                                    else if (rv.ContainsAnyFlags(isSug ? Affix.Flags_ForbiddenWord_OnlyUpcase_NoSuggest : Affix.Flags_ForbiddenWord_OnlyUpcase))
                                    {
                                        // check forbiddenwords
                                        goto resultNull;
                                    }
                                }

                                switch (CompoundCheck_TrySecondAffix(rvFirst!, rv, word, i, len, tmpWordNum: wordNum, tmpNumSyllable: numSyllable))
                                {
                                    case CompoundCheckOutcomes.Fail:
                                        goto resultNull;

                                    case CompoundCheckOutcomes.Permit:
                                        rv = rvFirst;
                                        goto resultRv;
                                }

                                // perhaps second word is a compound word (recursive call)
                                // (only if SPELL_COMPOUND_2 is not set and maxwordnum is not exceeded)
                                if (
                                    info.IsMissingFlag(SpellCheckResultType.Compound2)
                                    &&
                                    (wordNum + 2) < maxwordnum
                                )
                                {
                                    rv = CompoundCheck(st.SliceToTerminatorFromOffset(i), wordNum + 1, numSyllable, maxwordnum, words?.CreateIncremented(), rwords.CreateIncremented(), false, isSug, info, ref opLimiter);

                                    if (
                                        rv is not null
                                        &&
                                        (
                                            Affix.CompoundPatterns.IsEmpty
                                            ||
                                            i >= word.Length
                                            ||
                                            (scpd != 0) == Affix.CompoundPatterns.Check(word, i, rvFirst!, rv, affixed)
                                        )
                                    )
                                    {
                                        // forbid compound word, if it is a non-compound word with typical
                                        // fault, or a dictionary word pair
                                        switch (CompoundCheckDecideForbidFinal(word, len, i, st, rv))
                                        {
                                            case CompoundCheckOutcomes.Fail:
                                                goto resultNull;

                                            case CompoundCheckOutcomes.Permit:
                                                rv = rvFirst;
                                                goto resultRv;
                                        }
                                    }
                                }
                            }
                            while (simplifiedTripple && !checkedSimplifiedTriple);  // end of striple loop

                            if (checkedSimplifiedTriple)
                            {
                                i++;
                                checkedSimplifiedTriple = false;
                                simplifiedTripple = false;
                            }

                        } // first word is ok condition

                        if (oldIndex != 0)
                        {
                            i = oldIndex;
                            oldIndex = 0;
                            len = oldLen;
                            cMin = oldCMin;
                            cMax = oldCMax;
                        }

                        scpd++;
                    }
                    while (!onlyCpdRule && Affix.SimplifiedCompound && scpd <= Affix.CompoundPatterns.Count); // end of simplifiedcpd loop

                    scpd = 0;
                    wordNum = oldWordNum;
                    numSyllable = oldNumSyllable;

                    if (oldIndex != 0)
                    {
                        st.Assign(word); // XXX add more optim.

                        i = oldIndex;
                        oldIndex = 0;
                        len = oldLen;
                        cMin = oldCMin;
                        cMax = oldCMax;
                    }
                    else
                    {
                        st[i] = ch;
                    }
                }
                while (Affix.CompoundRules.HasItems && oldWordNum == 0 && inversePostfixIncrement(ref onlyCpdRule)); // end of onlycpd loop
            }

        resultNull:
            st.Dispose();
            return null;

        resultRv:
            st.Dispose();
            return rv;

            static bool inversePostfixIncrement(ref bool b)
            {
                // This is a strange implementation of a postfix increment to remove the use of an int in these cases.

                if (b)
                {
                    return false;
                }

                b = true;
                return true;
            }
        }

        private CompoundCheckOutcomes CompoundCheck_TrySecondRoot(WordEntry rvFirst, WordEntry rv, ReadOnlySpan<char> word, int i, int len, int tmpNumSyllable, int tmpWordNum, int scpd, FlagValue scpdPatternEntryCondition2, bool isSug)
        {
            if (
                Affix.IsHungarian
                &&
                rv.ContainsFlag(SpecialFlags.LetterI)
                &&
                rv.DoesNotContainFlag(SpecialFlags.LetterJ)
            )
            {
                tmpNumSyllable--;
            }

            // increment word number, if the second root has a compoundroot flag
            if (rv.ContainsFlag(Affix.CompoundRoot))
            {
                tmpWordNum++;
            }

            // check forbiddenwords
            if (rv.ContainsAnyFlags(isSug ? Affix.Flags_ForbiddenWord_OnlyUpcase_NoSuggest : Affix.Flags_ForbiddenWord_OnlyUpcase))
            {
                return CompoundCheckOutcomes.Fail;
            }

            // second word is acceptable, as a root?
            // hungarian conventions: compounding is acceptable,
            // when compound forms consist of 2 words, or if more,
            // then the syllable number of root words must be 6, or lesser.

            if (
                rv is not null
                &&
                rv.ContainsAnyFlags(Affix.Flags_CompoundFlag_CompoundEnd)
                &&
                (
                    !Affix.CompoundWordMax.HasValue
                    ||
                    (tmpWordNum + 1) < Affix.CompoundWordMax
                    ||
                    (
                        Affix.CompoundMaxSyllable != 0
                        &&
                        (GetSyllable(rv.Word) + tmpNumSyllable) <= Affix.CompoundMaxSyllable
                    )
                )
                &&
                (
                    !Affix.CheckCompoundDup
                    ||
                    rv != rvFirst
                )
                &&
                (
                    scpd != 0
                    // test CHECKCOMPOUNDPATTERN conditions
                    ? (
                        scpdPatternEntryCondition2.IsZero
                        ||
                        rv.ContainsFlag(scpdPatternEntryCondition2)
                    )
                    // test CHECKCOMPOUNDPATTERN
                    : (
                        Affix.CompoundPatterns.IsEmpty
                        ||
                        (
                            i < word.Length
                            &&
                            !Affix.CompoundPatterns.Check(word, i, rvFirst, rv, false)
                        )
                    )
                )
            )
            {
                // forbid compound word, if it is a non compound word with typical fault
                return CompoundReplacementOrWordPairCheck(word.Limit(len)) ? CompoundCheckOutcomes.Fail : CompoundCheckOutcomes.Permit;
            }

            return CompoundCheckOutcomes.Continue;
        }

        private CompoundCheckOutcomes CompoundCheck_TrySecondAffix(WordEntry rvFirst, WordEntry? rv, ReadOnlySpan<char> word, int i, int len, int tmpWordNum, int tmpNumSyllable)
        {
            if (Affix.IsHungarian)
            {
                CompoundCheckHungarianAffix2(word, i, rv, ref tmpWordNum, ref tmpNumSyllable);
            }

            if (rv is not null)
            {
                // increment word number, if the second word has a compoundroot flag
                if (rv.ContainsFlag(Affix.CompoundRoot))
                {
                    tmpWordNum++;
                }

                // second word is acceptable, as a word with prefix or/and suffix?
                // hungarian conventions: compounding is acceptable,
                // when compound forms consist 2 word, otherwise
                // the syllable number of root words is 6, or lesser.
                if (
                    (
                        !Affix.CompoundWordMax.HasValue
                        ||
                        tmpWordNum + 1 < Affix.CompoundWordMax.GetValueOrDefault()
                        ||
                        (
                            Affix.CompoundMaxSyllable != 0
                            &&
                            tmpNumSyllable <= Affix.CompoundMaxSyllable
                        )
                    )
                    &&
                    (
                        !Affix.CheckCompoundDup
                        ||
                        rv != rvFirst
                    )
                )
                {
                    // forbid compound word, if it is a non compound word with typical fault
                    return CompoundReplacementOrWordPairCheck(word.Limit(len)) ? CompoundCheckOutcomes.Fail : CompoundCheckOutcomes.Permit;
                }
            }

            return CompoundCheckOutcomes.Continue;
        }

        private WordEntry? CompoundCheckWordSearch_AffixCheckCompoundFlag(ReadOnlySpan<char> st, bool huMovRule)
        {
            var movCompoundOptions = huMovRule ? CompoundOptions.Other : CompoundOptions.Begin;

            var rv = PrefixCheck(st, movCompoundOptions, Affix.CompoundFlag);

            if (rv is null)
            {
                rv = SuffixCheck(st, AffixEntryOptions.None, null, FlagValue.Zero, Affix.CompoundFlag, movCompoundOptions);

                if (rv is null && Affix.CompoundMoreSuffixes)
                {
                    rv = SuffixCheckTwoSfx(st, AffixEntryOptions.None, null, Affix.CompoundFlag);
                }

                if (
                    rv is not null
                    &&
                    !huMovRule
                    &&
                    _suffix is not null
                    &&
                    _suffix.ContainsAnyContClass(Affix.Flags_CompoundForbid_CompoundEnd)
                )
                {
                    rv = null;
                }
            }

            return rv;
        }

        private WordEntry? CompoundCheckWordSearch_AffixCheckCompoundBegin(ReadOnlySpan<char> st, bool huMovRule)
        {
            var movCompoundOptions = huMovRule ? CompoundOptions.Other : CompoundOptions.Begin;

            var rv = SuffixCheck(st, AffixEntryOptions.None, null, default, Affix.CompoundBegin, movCompoundOptions);

            if (rv is null)
            {
                if (Affix.CompoundMoreSuffixes)
                {
                    rv = SuffixCheckTwoSfx(st, AffixEntryOptions.None, null, Affix.CompoundBegin);
                    if (rv is not null)
                    {
                        return rv;
                    }
                }

                rv = PrefixCheck(st, movCompoundOptions, Affix.CompoundBegin);
            }

            return rv;
        }

        private WordEntry? CompoundCheckWordSearch_AffixCheckCompoundMiddle(ReadOnlySpan<char> st, bool huMovRule)
        {
            var movCompoundOptions = huMovRule ? CompoundOptions.Other : CompoundOptions.Begin;

            var rv = SuffixCheck(st, AffixEntryOptions.None, null, default, Affix.CompoundMiddle, movCompoundOptions);

            if (rv is null)
            {
                if (Affix.CompoundMoreSuffixes)
                {
                    rv = SuffixCheckTwoSfx(st, AffixEntryOptions.None, null, Affix.CompoundMiddle);
                    if (rv is not null)
                    {
                        return rv;
                    }
                }

                rv = PrefixCheck(st, movCompoundOptions, Affix.CompoundMiddle);
            }

            return rv;
        }

        private readonly WordEntry? CompoundCheckWordSearch_OnlyCpdRule(
            string searchEntryWord,
            WordEntryDetail[] searchEntryDetails,
            FlagValue scpdPatternEntryCondition,
            ref IncrementalWordList? words,
            IncrementalWordList rwords)
        {
            foreach (var searchEntryDetail in searchEntryDetails)
            {
                if (searchEntryDetail.DoesNotContainFlag(Affix.NeedAffix))
                {
                    // NOTE: do not reorder DefCompoundCheck calls or other conditions due to side effects

                    bool defCpdCheck;
                    if (words is not null)
                    {
                        defCpdCheck = DefCompoundCheck(words, searchEntryDetail, false);
                    }
                    else if (DefCompoundCheck(rwords, searchEntryDetail, false))
                    {
                        words = rwords;
                        defCpdCheck = true;
                    }
                    else
                    {
                        defCpdCheck = false;
                    }

                    // NOTE: do not reorder DefCompoundCheck calls or other conditions due to side effects

                    if (defCpdCheck && (scpdPatternEntryCondition.IsZero || searchEntryDetail.ContainsFlag(scpdPatternEntryCondition)))
                    {
                        return searchEntryDetail.ToEntry(searchEntryWord);
                    }
                }
            }

            return null;
        }

        private readonly WordEntry? CompoundCheckWordSearch_NormalNoWordList(
            string searchEntryWord,
            WordEntryDetail[] searchEntryDetails,
            FlagValue scpdPatternEntryCondition,
            FlagValue compoundPart)
        {
            foreach (var searchEntryDetail in searchEntryDetails)
            {
                if (
                    searchEntryDetail.DoesNotContainFlag(Affix.NeedAffix)
                    &&
                    searchEntryDetail.Flags.ContainsAny(Affix.CompoundFlag, scpdPatternEntryCondition, compoundPart)
                )
                {
                    return searchEntryDetail.ToEntry(searchEntryWord);
                }
            }

            return null;
        }

        private readonly WordEntry? CompoundCheckWordSearch_NormalWithExistingWordList(
            string searchEntryWord,
            WordEntryDetail[] searchEntryDetails,
            FlagValue scpdPatternEntryCondition)
        {
            foreach (var searchEntryDetail in searchEntryDetails)
            {
                if (
                    searchEntryDetail.DoesNotContainFlag(Affix.NeedAffix)
                    &&
                    searchEntryDetail.ContainsFlag(Affix.CompoundBegin)
                    &&
                    (scpdPatternEntryCondition.IsZero || searchEntryDetail.ContainsFlag(scpdPatternEntryCondition))
                )
                {
                    return searchEntryDetail.ToEntry(searchEntryWord);
                }
            }

            return null;
        }

        /// <summary>
        /// Check if word with affixes is correctly spelled.
        /// </summary>
        private WordEntry? AffixCheck(ReadOnlySpan<char> word, FlagValue needFlag, CompoundOptions inCompound)
        {
            // check all prefixes (also crossed with suffixes if allowed)
            var rv = PrefixCheck(word, inCompound, needFlag);
            if (rv is null)
            {
                // if still not found check all suffixes
                rv = SuffixCheck(word, AffixEntryOptions.None, null, default, needFlag, inCompound);

                if (Affix.ContClasses.HasItems)
                {
                    ClearAffixes();

                    rv ??=
                        // if still not found check all two-level suffixes
                        SuffixCheckTwoSfx(word, AffixEntryOptions.None, null, needFlag)
                        ??
                        // if still not found check all two-level prefixes
                        PrefixCheckTwoSfx(word, CompoundOptions.Not, needFlag);
                }
            }

            return rv;
        }

        /// <summary>
        /// Check word for prefixes
        /// </summary>
        public WordEntry? PrefixCheck(ReadOnlySpan<char> word, CompoundOptions inCompound, FlagValue needFlag)
        {
            ClearPrefix();
            ClearAllAppendAndExtra();
            WordEntry? rv;

            if (inCompound == CompoundOptions.End && Affix.CompoundPermitFlag.IsZero)
            {
                // not possible to permit prefixes in compounds
                return null;
            }

            // first handle the special case of 0 length prefixes
            foreach (var pe in Affix.Prefixes.GetAffixesWithEmptyKeys())
            {
                if (
                    // fogemorpheme
                    (inCompound != CompoundOptions.Not || !pe.ContainsContClass(Affix.OnlyInCompound))
                    &&
                    // permit prefixes in compounds
                    (inCompound != CompoundOptions.End || pe.ContainsContClass(Affix.CompoundPermitFlag))
                )
                {
                    // check prefix
                    rv = CheckWordPrefix(pe, word, inCompound, needFlag);
                    if (rv is not null)
                    {
                        SetPrefix(pe);
                        return rv;
                    }
                }
            }

            foreach (var pe in Affix.Prefixes.GetMatchingAffixes(word))
            {
                if (
                    // fogemorpheme
                    (inCompound != CompoundOptions.Not || !pe.ContainsContClass(Affix.OnlyInCompound))
                    &&
                    // permit prefixes in compounds
                    (inCompound != CompoundOptions.End || pe.ContainsContClass(Affix.CompoundPermitFlag))
                )
                {
                    // check prefix
                    rv = CheckWordPrefix(pe, word, inCompound, needFlag);
                    if (rv is not null)
                    {
                        SetPrefix(pe);
                        return rv;
                    }
                }
            }

            return null;
        }

        public WordEntry? PrefixCheckTwoSfx(ReadOnlySpan<char> word, CompoundOptions inCompound, FlagValue needFlag)
        {
            ClearPrefix();
            ClearSuffixAppendAndExtra();
            WordEntry? rv;

            // first handle the special case of 0 length prefixes
            foreach (var pe in Affix.Prefixes.GetAffixesWithEmptyKeys())
            {
                rv = CheckTwoSfx(pe, word, inCompound, needFlag);
                if (rv is not null)
                {
                    return rv;
                }
            }

            // now handle the general case
            foreach (var pe in Affix.Prefixes.GetMatchingAffixes(word))
            {
                rv = CheckTwoSfx(pe, word, inCompound, needFlag);
                if (rv is not null)
                {
                    SetPrefix(pe);
                    return rv;
                }
            }

            return null;
        }

        /// <summary>
        /// Check if this prefix entry matches.
        /// </summary>
        private WordEntry? CheckTwoSfx(PrefixEntry pe, ReadOnlySpan<char> word, CompoundOptions inCompound, FlagValue needFlag)
        {
            // on entry prefix is 0 length or already matches the beginning of the word.
            // So if the remaining root word has positive length
            // and if there are enough chars in root word and added back strip chars
            // to meet the number of characters conditions, then test it

            var tmpl = word.Length - pe.Append.Length; // length of tmpword

            if (
                (tmpl > 0 || (tmpl == 0 && Affix.FullStrip))
                &&
                (tmpl + pe.Strip.Length >= pe.Conditions.Count)
            )
            {
                // generate new root word by removing prefix and adding
                // back any characters that would have been stripped

                var tmpword = StringEx.ConcatSpan(pe.Strip, word.Slice(pe.Append.Length));

                // now make sure all of the conditions on characters
                // are met.  Please see the appendix at the end of
                // this file for more info on exactly what is being
                // tested

                // if all conditions are met then check if resulting
                // root word in the dictionary

                if (pe.TestCondition(tmpword))
                {
                    // prefix matched but no root word was found
                    // if CrossProduct is allowed, try again but now
                    // cross checked combined with a suffix

                    if (pe.Options.HasFlagEx(AffixEntryOptions.CrossProduct) && (inCompound != CompoundOptions.Begin))
                    {
                        // find hash entry of root word
                        if (SuffixCheckTwoSfx(tmpword, AffixEntryOptions.CrossProduct, pe, needFlag) is { } he)
                        {
                            return he;
                        }
                    }
                }
            }

            return null;
        }

        public WordEntry? SuffixCheck(ReadOnlySpan<char> word, AffixEntryOptions sfxOpts, PrefixEntry? pfx, FlagValue cclass, FlagValue needFlag, CompoundOptions inCompound)
        {
            if (inCompound == CompoundOptions.Begin && Affix.CompoundPermitFlag.IsZero)
            {
                // not possible to be signed with compoundpermitflag flag
                return null;
            }

            WordEntry? rv;
            var checkWordCclassFlag = inCompound != CompoundOptions.Not ? default : Affix.OnlyInCompound;

            var pfxHasCircumfix = false;
            var pfxDoesNotNeedAffix = false;
            if (pfx is not null)
            {
                pfxHasCircumfix = pfx.ContainsContClass(Affix.Circumfix);
                pfxDoesNotNeedAffix = !pfx.ContainsContClass(Affix.NeedAffix);
            }

            // first handle the special case of 0 length suffixes
            foreach (var se in Affix.Suffixes.GetAffixesWithEmptyKeys())
            {
                // suffixes are not allowed in beginning of compounds
                if (
                    (
                        inCompound != CompoundOptions.Begin
                        ||
                        // except when signed with compoundpermitflag flag
                        se.ContainsContClass(Affix.CompoundPermitFlag)
                    )
                    &&
                    // fogemorpheme
                    (
                        inCompound != CompoundOptions.Not
                        ||
                        !se.ContainsContClass(Affix.OnlyInCompound)
                    )
                    &&
                    // needaffix on prefix or first suffix
                    (
                        pfxDoesNotNeedAffix
                        ||
                        cclass.HasValue
                        ||
                        !se.ContainsContClass(Affix.NeedAffix)
                    )
                    &&
                    (
                        Affix.Circumfix.IsZero
                        ||
                        // no circumfix flag in prefix and suffix
                        // circumfix flag in prefix AND suffix
                        se.ContainsContClass(Affix.Circumfix) == pfxHasCircumfix
                    )
                )
                {
                    rv = CheckWordSuffix(se, word, sfxOpts, pfx, cclass, needFlag, checkWordCclassFlag);
                    if (rv is not null)
                    {
                        SetSuffix(se);
                        return rv;
                    }
                }
            }

            // now handle the general case
            if (word.Length == 0)
            {
                return null;
            }

            foreach (var sptr in Affix.Suffixes.GetMatchingAffixes(word))
            {
                if (
                    (
                        // suffixes are not allowed in beginning of compounds
                        inCompound != CompoundOptions.Begin
                        ||
                        // except when signed with compoundpermitflag flag
                        sptr.ContainsContClass(Affix.CompoundPermitFlag)
                    )
                    &&
                    // fogemorpheme
                    (
                        inCompound != CompoundOptions.Not
                        ||
                        !sptr.ContainsContClass(Affix.OnlyInCompound)
                    )
                    &&
                    (
                        inCompound != CompoundOptions.End
                        ||
                        pfx is not null
                        ||
                        !sptr.ContainsContClass(Affix.OnlyInCompound)
                    )
                    &&
                    // needaffix on prefix or first suffix
                    (
                        pfxDoesNotNeedAffix
                        ||
                        cclass.HasValue
                        ||
                        !sptr.ContainsContClass(Affix.NeedAffix)
                    )
                    &&
                    (
                        Affix.Circumfix.IsZero
                        ||
                        // no circumfix flag in prefix and suffix
                        // circumfix flag in prefix AND suffix
                        sptr.ContainsContClass(Affix.Circumfix) == pfxHasCircumfix
                    )
                )
                {
                    rv = CheckWordSuffix(sptr, word, sfxOpts, pfx, cclass, needFlag, checkWordCclassFlag);
                    if (rv is not null)
                    {
                        SetSuffix(sptr);
                        SetSuffixFlag(sptr.AFlag);

                        if (!sptr.ContClass.HasItems)
                        {
                            SetSuffixAppend(sptr.Key);
                        }
                        else if (
                            Affix.IsHungarian
                            && sptr.Key.Length >= 2
                            && sptr.Key[0] == 'i'
                            && sptr.Key[1] is not ('y' or 't')
                        )
                        {
                            // LANG_hu section: spec. Hungarian rule
                            SetSuffixExtra(true);
                        }

                        // END of LANG_hu section
                        return rv;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Check word for two-level suffixes.
        /// </summary>
        public WordEntry? SuffixCheckTwoSfx(ReadOnlySpan<char> word, AffixEntryOptions sfxopts, PrefixEntry? pfx, FlagValue needflag)
        {
            if (Affix.ContClasses.IsEmpty)
            {
                return null;
            }

            WordEntry? rv;

            // first handle the special case of 0 length suffixes
            foreach (var se in Affix.Suffixes.GetAffixesWithEmptyKeys())
            {
                if (Affix.ContClasses.Contains(se.AFlag))
                {
                    rv = CheckTwoSfx(se, word, sfxopts, pfx, needflag);
                    if (rv is not null)
                    {
                        return rv;
                    }
                }
            }

            // now handle the general case
            if (word.IsEmpty)
            {
                return null; // FULLSTRIP
            }

            foreach (var affix in Affix.Suffixes.GetMatchingAffixes(word))
            {
                if (Affix.ContClasses.Contains(affix.AFlag))
                {
                    rv = CheckTwoSfx(affix, word, sfxopts, pfx, needflag);
                    if (rv is not null && _suffix is not null)
                    {
                        SetSuffixFlag(_suffix.AFlag);
                        if (!affix.ContClass.HasItems)
                        {
                            SetSuffixAppend(affix.Key);
                        }

                        return rv;
                    }
                }
            }

            return null;
        }

        public readonly bool TryLookupDetails(
            ReadOnlySpan<char> word,
#if !NO_EXPOSED_NULLANNOTATIONS
            [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)]
#endif
            out string actualKey,
#if !NO_EXPOSED_NULLANNOTATIONS
            [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)]
#endif
            out WordEntryDetail[] details) =>
            WordList._entriesByRoot.TryGetValue(word, out actualKey!, out details!);

        public readonly bool TryLookupDetails(
            string word,
#if !NO_EXPOSED_NULLANNOTATIONS
            [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)]
#endif
            out WordEntryDetail[] details) =>
            WordList._entriesByRoot.TryGetValue(word, out details!);

        /// <summary>
        /// Compound check patterns.
        /// </summary>
        private readonly bool DefCompoundCheck(IncrementalWordList words, in WordEntryDetail rv, bool all)
        {
            // has the last word COMPOUNDRULE flag?
            if (Affix.CompoundRules.EntryContainsRuleFlags(rv.Flags))
            {
                words.SetCurrent(rv);
                if (Affix.CompoundRules.CompoundCheck(words, all))
                {
                    return true;
                }
            }

            words.ClearCurrent();
            return false;
        }

        /// <summary>
        /// Calculate number of syllable for compound-checking.
        /// </summary>
        private readonly int GetSyllable(ReadOnlySpan<char> word)
        {
            var num = 0;
            if (Affix.CompoundMaxSyllable != 0 && Affix.CompoundVowels.HasItems)
            {
                var index = Affix.CompoundVowels.FindIndexOfMatch(word);
                while (index >= 0)
                {
                    num++;
                    index = Affix.CompoundVowels.FindIndexOfMatch(word, index + 1);
                }
            }

            return num;
        }

        private readonly void CompoundCheckHungarianAffix1(ReadOnlySpan<char> word, ref int wordNum, ref int numSyllable)
        {
            // calculate syllable number of the word
            numSyllable += GetSyllable(word);

            // - affix syllable num.
            // XXX only second suffix (inflections, not derivations)
            if (_suffixAppend is not null)
            {
                numSyllable -= GetSyllableReversed(_suffixAppend);
            }
            if (_suffixExtra)
            {
                numSyllable -= 1;
            }

            // + 1 word, if syllable number of the prefix > 1 (hungarian convention)
            if (_prefix is not null && GetSyllable(_prefix.Key) > 1)
            {
                wordNum++;
            }
        }

        private readonly void CompoundCheckHungarianAffix2(ReadOnlySpan<char> word, int i, WordEntry? rv, ref int wordNum, ref int numSyllable)
        {
            // pfxappnd = prefix of word+i, or NULL
            // calculate syllable number of prefix.
            // hungarian convention: when syllable number of prefix is more,
            // than 1, the prefix+word counts as two words.

            if (i < word.Length)
            {
                // calculate syllable number of the word
                numSyllable += GetSyllable(word.Slice(0, i));
            }

            // - affix syllable num.
            // XXX only second suffix (inflections, not derivations)
            if (_suffixAppend is not null)
            {
                numSyllable -= GetSyllableReversed(_suffixAppend);
            }

            if (_suffixExtra)
            {
                numSyllable -= 1;
            }

            // + 1 word, if syllable number of the prefix > 1 (hungarian
            // convention)
            if (_prefix is not null && GetSyllable(_prefix.Key) > 1)
            {
                wordNum++;
            }

            // increment syllable num, if last word has a SYLLABLENUM flag
            // and the suffix is beginning `s'

            if (Affix.CompoundSyllableNum is { Length: > 0 })
            {
                if (_suffixFlag == SpecialFlags.LetterCLower)
                {
                    numSyllable += 2;
                }
                else if (
                    _suffixFlag == SpecialFlags.LetterJ
                    ||
                    (
                        _suffixFlag == SpecialFlags.LetterI
                        &&
                        rv is not null
                        &&
                        rv.ContainsFlag(SpecialFlags.LetterJ)
                    )
                )
                {
                    numSyllable++;
                }
            }
        }

        /// <summary>
        /// Calculate number of syllable for compound-checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int GetSyllableReversed(ReadOnlySpan<char> word)
        {
            // Because the code is effectively the same, forward and reverse searches can use the same algorithm
            return GetSyllable(word);
        }

        private bool CompoundReplacementOrWordPairCheck(ReadOnlySpan<char> word)
        {
            return (Affix.CheckCompoundRep && CompoundReplacementCheck(word))
                || CompoundWordPairCheck(word);
        }

        /// <summary>
        /// Is word a non compound with a REP substitution?
        /// </summary>
        /// <seealso cref="AffixConfig.CheckCompoundRep"/>
        /// <seealso cref="AffixConfig.Replacements"/>
        /// <seealso cref="AllReplacements"/>
        private bool CompoundReplacementCheck(ReadOnlySpan<char> word)
        {
            if (word.Length >= 2)
            {
                // use only available mid patterns
                foreach (var replacementEntry in WordList._allReplacements.RawArray)
                {
                    if (replacementEntry.Med is { Length: > 0 })
                    {
                        // search every occurence of the pattern in the word
                        var rIndex = word.IndexOf(replacementEntry.Pattern);
                        while (rIndex >= 0)
                        {
                            if (CandidateCheck(word.ReplaceIntoString(rIndex, replacementEntry.Pattern.Length, replacementEntry.Med)))
                            {
                                return true;
                            }

                            rIndex = word.IndexOf(replacementEntry.Pattern, rIndex + 1);
                        }
                    }
                }
            }

            return false;
        }

        private bool CompoundWordPairCheck(ReadOnlySpan<char> wordSlice)
        {
            var ok = false;

            if (wordSlice.Length > 2)
            {
                var candidateBuffer = ArrayPool<char>.Shared.Rent(wordSlice.Length + 1);
                var candidate = candidateBuffer.AsSpan(0, wordSlice.Length + 1);
                candidate[0] = wordSlice[0];

                for (var i = 1; i < wordSlice.Length; i++)
                {
                    candidate[i] = ' ';
                    wordSlice.Slice(i).CopyTo(candidate.Slice(i + 1));
                    if (CandidateCheck(candidate))
                    {
                        ok = true;
                        break;
                    }

                    candidate[i] = wordSlice[i];
                }

                ArrayPool<char>.Shared.Return(candidateBuffer);
            }

            return ok;
        }

        private CompoundCheckOutcomes CompoundCheckDecideForbidFinal(ReadOnlySpan<char> word, int len, int i, SimulatedCString st, WordEntry rv)
        {
            // forbid compound word, if it is a non-compound word with typical
            // fault, or a dictionary word pair

            var wordLenPrefix = word.Limit(len);
            if (CompoundWordPairCheck(wordLenPrefix))
            {
                return CompoundCheckOutcomes.Fail;
            }

            if (Affix.CheckCompoundRep || Affix.ForbiddenWord.HasValue)
            {
                if (Affix.CheckCompoundRep && CompoundReplacementCheck(wordLenPrefix))
                {
                    return CompoundCheckOutcomes.Fail;
                }

                // check first part
                if (word.Length > i && word.Slice(i).StartsWith(rv.Word.AsSpan()))
                {
                    var exchangeIndex = rv.Word.Length + i;
                    var characterBackup = st.ExchangeWithNull(exchangeIndex);

                    if (CompoundReplacementOrWordPairCheck(st.TerminatedSpan))
                    {
                        st[exchangeIndex] = characterBackup;
                        return CompoundCheckOutcomes.ContinueNextIteration;
                    }

                    if (Affix.ForbiddenWord.HasValue && CompoundCheckExplicitlyForbidden(word, len, i, rv, st))
                    {
                        return CompoundCheckOutcomes.Fail;
                    }

                    st[exchangeIndex] = characterBackup;
                }
            }

            return CompoundCheckOutcomes.Permit;
        }

        private bool CompoundCheckExplicitlyForbidden(ReadOnlySpan<char> word, int len, int i, WordEntry rv, SimulatedCString st)
        {
            if (WordList.TryGetFirstEntryDetailByRootWord(word, out var rv2Details))
            {
                return rv2Details.ContainsFlag(Affix.ForbiddenWord)
                    && equalsOrdinalLimited(word, st.TerminatedSpan, rv.Word.Length + i);
            }
            else if (len <= word.Length)
            {
                var rv2 = AffixCheck(word.Slice(0, len), default, CompoundOptions.Not);
                return rv2 is not null
                    && rv2.ContainsFlag(Affix.ForbiddenWord)
                    && equalsOrdinalLimited(rv2.Word, st.TerminatedSpan, rv.Word.Length + i);
            }

            return false;

            static bool equalsOrdinalLimited(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int limit) =>
                a.Limit(limit).SequenceEqual(b.Limit(limit));
        }

        private WordEntry? CheckWordPrefix(PrefixEntry affix, ReadOnlySpan<char> word, CompoundOptions inCompound, FlagValue needFlag)
        {
            // on entry prefix is 0 length or already matches the beginning of the word.
            // So if the remaining root word has positive length
            // and if there are enough chars in root word and added back strip chars
            // to meet the number of characters conditions, then test it

            var tmpl = word.Length - affix.Append.Length; // length of tmpword

            if (tmpl > 0 || (tmpl == 0 && Affix.FullStrip))
            {
                // generate new root word by removing prefix and adding
                // back any characters that would have been stripped

                var tmpword = StringEx.ConcatSpan(affix.Strip, word.Slice(affix.Append.Length));

                // now make sure all of the conditions on characters
                // are met.  Please see the appendix at the end of
                // this file for more info on exactly what is being
                // tested

                // if all conditions are met then check if resulting
                // root word in the dictionary

                if (affix.TestCondition(tmpword))
                {
                    if (
                        !affix.ContainsContClass(Affix.NeedAffix) // forbid single prefixes with needaffix flag
                        &&
                        TryLookupDetails(tmpword, out var tmpwordString, out var details)
                    )
                    {
                        foreach (var detail in details)
                        {
                            if (
                                detail.ContainsFlag(affix.AFlag)
                                // && !affix.ContainsContClass(Affix.NeedAffix) // forbid single prefixes with needaffix flag
                                &&
                                (
                                    needFlag.IsZero
                                    ||
                                    detail.ContainsFlag(needFlag)
                                    ||
                                    affix.ContainsContClass(needFlag)
                                )
                            )
                            {
                                return new WordEntry(tmpwordString, detail);
                            }
                        }
                    }

                    // prefix matched but no root word was found
                    // if aeXPRODUCT is allowed, try again but now
                    // ross checked combined with a suffix

                    if (affix.Options.HasFlagEx(AffixEntryOptions.CrossProduct))
                    {
                        if (SuffixCheck(tmpword, AffixEntryOptions.CrossProduct, affix, default, needFlag, inCompound) is { } he)
                        {
                            return he;
                        }
                    }

                }
            }

            return null;
        }

        private readonly WordEntry? CheckWordSuffix(SuffixEntry affix, ReadOnlySpan<char> word, AffixEntryOptions optFlags, PrefixEntry? pfx, FlagValue cclass, FlagValue needFlag, FlagValue badFlag)
        {
            // if this suffix is being cross checked with a prefix
            // but it does not support cross products skip it

            var optFlagsHasCrossProduct = optFlags.HasFlagEx(AffixEntryOptions.CrossProduct);
            if (
                (
                    optFlagsHasCrossProduct
                    &&
                    (
                        pfx is null // enabled by prefix is impossible
                        ||
                        affix.Options.IsMissingFlag(AffixEntryOptions.CrossProduct)
                    )
                )
                ||
                (cclass.HasValue && !affix.ContainsContClass(cclass)) // ! handle cont. class
            )
            {
                return null;
            }

            // upon entry suffix is 0 length or already matches the end of the word.
            // So if the remaining root word has positive length
            // and if there are enough chars in root word and added back strip chars
            // to meet the number of characters conditions, then test it

            var tmpl = word.Length - affix.Append.Length;
            // the second condition is not enough for UTF-8 strings
            // it checked in test_condition()

            if (
                (tmpl > 0 || (Affix.FullStrip && tmpl == 0))
                &&
                (tmpl + affix.Strip.Length >= affix.Conditions.Count)
            )
            {
                // generate new root word by removing suffix and adding
                // back any characters that would have been stripped or
                // or null terminating the shorter string

                var tmpSpan = word.Slice(0, tmpl).ConcatSpan(affix.Strip);

                // now make sure all of the conditions on characters
                // are met.  Please see the appendix at the end of
                // this file for more info on exactly what is being
                // tested

                // if all conditions are met then check if resulting
                // root word in the dictionary
                if (affix.Conditions.IsEndingMatch(tmpSpan))
                {
                    if (TryLookupDetails(tmpSpan, out var tmpString, out var details))
                    {
                        foreach (var heDetail in details)
                        {
                            if (
                                (
                                    heDetail.ContainsFlag(affix.AFlag)
                                    ||
                                    (pfx is not null && pfx.ContainsContClass(affix.AFlag))
                                )
                                &&
                                (
                                    !optFlagsHasCrossProduct
                                    ||
                                    (
                                        pfx is not null
                                        &&
                                        (
                                            heDetail.ContainsFlag(pfx.AFlag)
                                            ||
                                            affix.ContainsContClass(pfx.AFlag) // enabled by prefix
                                        )
                                    )
                                )
                                && // check only in compound homonyms (bad flags)
                                heDetail.DoesNotContainFlag(badFlag)
                                && // handle required flag
                                (
                                    needFlag.IsZero
                                    ||
                                    heDetail.ContainsFlag(needFlag)
                                    ||
                                    affix.ContainsContClass(needFlag)
                                )
                            )
                            {
                                return new WordEntry(tmpString, heDetail);
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// See if two-level suffix is present in the word.
        /// </summary>
        private WordEntry? CheckTwoSfx(SuffixEntry se, ReadOnlySpan<char> word, AffixEntryOptions optflags, PrefixEntry? ppfx, FlagValue needflag)
        {
            // if this suffix is being cross checked with a prefix
            // but it does not support cross products skip it

            if (optflags.HasFlagEx(AffixEntryOptions.CrossProduct) && se.Options.IsMissingFlag(AffixEntryOptions.CrossProduct))
            {
                return null;
            }

            // upon entry suffix is 0 length or already matches the end of the word.
            // So if the remaining root word has positive length
            // and if there are enough chars in root word and added back strip chars
            // to meet the number of characters conditions, then test it

            var tmpl = word.Length - se.Append.Length; // length of tmpword

            // the second condition is not enough for UTF-8 strings
            // it checked in test_condition()

            if (
                (tmpl > 0 || (tmpl == 0 && Affix.FullStrip))
                &&
                (tmpl + se.Strip.Length >= se.Conditions.Count)
            )
            {
                // generate new root word by removing suffix and adding
                // back any characters that would have been stripped or
                // or null terminating the shorter string

                var tmpword = word.Slice(0, tmpl).ConcatSpan(se.Strip);

                // now make sure all of the conditions on characters
                // are met.  Please see the appendix at the end of
                // this file for more info on exactly what is being
                // tested

                // if all conditions are met then recall suffix_check
                if (se.TestCondition(tmpword))
                {
                    var he = ppfx is not null && se.ContainsContClass(ppfx.AFlag)
                        // handle conditional suffix
                        ? SuffixCheck(tmpword, AffixEntryOptions.None, null, se.AFlag, needflag, CompoundOptions.Not)
                        : SuffixCheck(tmpword, optflags, ppfx, se.AFlag, needflag, CompoundOptions.Not);

                    if (he is not null)
                    {
                        return he;
                    }
                }
            }

            return null;
        }

        private bool CandidateCheck(string word) =>
            WordList.ContainsEntriesForRootWord(word) || AffixCheck(word, default, CompoundOptions.Not) is not null;

        private bool CandidateCheck(ReadOnlySpan<char> word) =>
            WordList.ContainsEntriesForRootWord(word) || AffixCheck(word, default, CompoundOptions.Not) is not null;

        /// <summary>
        /// Make a copy of <paramref name="src"/> and returns it
        /// while removing all leading blanks and removing any trailing periods.
        /// </summary>
        /// <param name="src">The source text to clean and classify.</param>
        /// <param name="capType">The capitalization type the <paramref name="src"/> is classified as.</param>
        /// <param name="abbv">Abbreviation flag indicating the presence of trailing periods.</param>
        /// <returns>The cleaned source text.</returns>
        /// <remarks>
        /// Removes all leading blanks and removes any trailing periods after recording
        /// their presence with the abbreviation flag (<paramref name="abbv"/>)
        /// also since already going through character by character,
        /// set the capitalization type (<paramref name="capType"/>) and
        /// return the length of the "cleaned" (and UTF-8 encoded) word
        /// </remarks>
        public readonly string CleanWord2(string src, out CapitalizationType capType, out int abbv)
        {
            if (Affix.IgnoredChars.HasItems)
            {
                src = Affix.IgnoredChars.RemoveChars(src);
            }

            // first skip over any leading blanks
            var qIndex = StringEx.CountMatchingFromLeft(src, ' ');

            // now strip off any trailing periods (recording their presence)
            abbv = StringEx.CountMatchingFromRight(src, '.');

            var newLength = src.Length - qIndex - abbv;
            if (newLength <= 0)
            {
                // if no characters are left it can't be capitalized
                capType = CapitalizationType.None;
                return string.Empty;
            }

            if (newLength < src.Length)
            {
                src = src.Substring(qIndex, newLength);
            }

            capType = StringEx.GetCapitalizationType(src, TextInfo);
            return src;
        }

        public readonly string CleanWord2(ReadOnlySpan<char> src, out CapitalizationType capType, out int abbv)
        {
            if (Affix.IgnoredChars.HasItems)
            {
                src = Affix.IgnoredChars.RemoveChars(src);
            }

            // first skip over any leading blanks
            var qIndex = StringEx.CountMatchingFromLeft(src, ' ');

            // now strip off any trailing periods (recording their presence)
            abbv = StringEx.CountMatchingFromRight(src, '.');

            var newLength = src.Length - qIndex - abbv;
            if (newLength <= 0)
            {
                // if no characters are left it can't be capitalized
                capType = CapitalizationType.None;
                return string.Empty;
            }

            if (newLength < src.Length)
            {
                src = src.Slice(qIndex, newLength);
            }

            capType = StringEx.GetCapitalizationType(src, TextInfo);
            return src.ToString();
        }
    }

    private enum CompoundOptions : byte
    {
        Not = 0,
        Begin = 1,
        End = 2,
        Other = 3
    }

    private enum CompoundCheckOutcomes : byte
    {
        Fail = 0,
        Permit = 1,
        Continue = 2,
        ContinueNextIteration = 3
    }
}

