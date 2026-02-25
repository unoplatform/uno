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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WeCantSpell.Hunspell;

public partial class WordList
{
    private struct QuerySuggest
    {
        public QuerySuggest(WordList wordList, QueryOptions? options, CancellationToken cancellationToken, bool testSimpleSuggestion = false)
        {
            _query = new(wordList, options, cancellationToken);
            _testSimpleSuggestion = testSimpleSuggestion;
            _suggestCandidateStack = new();
        }

        internal QuerySuggest(in QuerySuggest source)
        {
            _query = new(source._query);
            _testSimpleSuggestion = source._testSimpleSuggestion;
            _suggestCandidateStack = source._suggestCandidateStack;
        }

        private Query _query;

        internal CandidateStack _suggestCandidateStack;

        /// <summary>
        /// For testing compound words formed from 3 or more words.
        /// </summary>
        /// <remarks>
        /// When <c>true</c>, don't suggest compound words, and <see cref="Suggest(List{string}, string, ref bool)"/> returns <c>true</c> when the first suggestion is found.
        /// </remarks>
        private readonly bool _testSimpleSuggestion;

        public readonly WordList WordList => _query.WordList;

        public readonly AffixConfig Affix => _query.Affix;

        public readonly TextInfo TextInfo => _query.TextInfo;

        public readonly QueryOptions Options => _query.Options;

        public readonly int MaxCharDistance => Options.MaxCharDistance;

        public readonly int MaxCompoundSuggestions => Options.MaxCompoundSuggestions;

        public readonly int MaxSuggestions => Options.MaxSuggestions;

        public readonly int MaxRoots => Options.MaxRoots;

        public readonly int MaxWords => Options.MaxWords;

        public readonly int MaxGuess => Options.MaxGuess;

        public readonly int MaxPhonSugs => Options.MaxPhoneticSuggestions;

        public List<string> Suggest(string word)
        {
            if (_query.WordList.IsEmpty)
            {
                goto fail;
            }

            // process XML input of the simplified API (see manual)
            if (word.StartsWith(Query.DefaultXmlTokenCheckPrefix, StringComparison.Ordinal))
            {
                goto fail; // TODO: complete support for XML input
            }

            if (word.Length >= Options.MaxWordLen)
            {
                goto fail;
            }

            // something very broken if suggest ends up calling itself with the same word
            if (_suggestCandidateStack.Contains(word))
            {
                goto fail;
            }

            // input conversion
            if (!Affix.InputConversions.TryConvert(word, out var scw))
            {
                scw = word;
            }

            scw = _query.CleanWord2(scw, out var capType, out var abbv);

            if (scw.Length == 0)
            {
                goto fail;
            }

            CandidateStack.Push(ref _suggestCandidateStack, word);

            var slst = SuggestInternal(word, scw, capType, abbv);

            CandidateStack.Pop(ref _suggestCandidateStack);

            return slst;

        fail:

            return [];
        }

        public List<string> Suggest(ReadOnlySpan<char> word)
        {
            if (_query.WordList.IsEmpty)
            {
                goto fail;
            }

            // process XML input of the simplified API (see manual)
            if (word.StartsWithOrdinal(Query.DefaultXmlTokenCheckPrefix))
            {
                goto fail; // TODO: complete support for XML input
            }

            if (word.Length >= Options.MaxWordLen)
            {
                goto fail;
            }

            // something very broken if suggest ends up calling itself with the same word
            if (_suggestCandidateStack.ExceedsArbitraryDepthLimit || _suggestCandidateStack.Contains(word))
            {
                goto fail;
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
                goto fail;
            }

            // NOTE: because a string isn't formed until this point, scw is pushed instead. It isn't the same, but might be good enough.

            CandidateStack.Push(ref _suggestCandidateStack, scw);

            var slst = SuggestInternal(word, scw, capType, abbv);

            CandidateStack.Pop(ref _suggestCandidateStack);

            return slst;

        fail:

            return [];
        }

        private List<string> SuggestInternal(ReadOnlySpan<char> word, string scw, CapitalizationType capType, int abbv)
        {
            var slst = new List<string>();

            var opLimiter = new OperationTimedLimiter(Options.TimeLimitSuggestGlobal, _query.CancellationToken);

            var textInfo = _query.TextInfo;

            var onlyCompoundSuggest = false;
            var capWords = false;
            var good = false;
            string? wspace;

            switch (capType)
            {
                case CapitalizationType.None:

                    // check capitalized form for FORCEUCASE
                    if (Affix.ForceUpperCase.HasValue)
                    {
                        var info = SpellCheckResultType.OrigCap;
                        if (_query.CheckWord(scw, ref info, out _) is not null)
                        {
                            slst.Add(StringEx.MakeInitCap(scw, textInfo));
                            goto result;
                        }
                    }

                    good |= Suggest(slst, scw, ref onlyCompoundSuggest);

                    if (opLimiter.QueryForCancellation())
                    {
                        goto result;
                    }

                    if (abbv != 0)
                    {
                        wspace = scw + ".";
                        good |= Suggest(slst, wspace, ref onlyCompoundSuggest);

                        if (opLimiter.QueryForCancellation())
                        {
                            goto result;
                        }
                    }

                    break;

                case CapitalizationType.Init:
                    capWords = true;
                    good |= Suggest(slst, scw, ref onlyCompoundSuggest);

                    if (opLimiter.QueryForCancellation())
                    {
                        goto result;
                    }

                    good |= Suggest(slst, StringEx.MakeAllSmall(scw, textInfo), ref onlyCompoundSuggest);

                    if (opLimiter.QueryForCancellation())
                    {
                        goto result;
                    }

                    break;

                case CapitalizationType.HuhInit:
                    capWords = true;
                    goto case CapitalizationType.Huh;

                case CapitalizationType.Huh:

                    good |= Suggest(slst, scw, ref onlyCompoundSuggest);

                    if (opLimiter.QueryForCancellation())
                    {
                        goto result;
                    }

                    // something.The -> something. The
                    var dotPos = scw.IndexOf('.');
                    if (
                        dotPos >= 0
                        &&
                        StringEx.GetCapitalizationType(scw.AsSpan(dotPos + 1), textInfo) == CapitalizationType.Init
                    )
                    {
                        InsertSuggestion(slst, scw.Insert(dotPos + 1, " "));
                    }

                    if (capWords)
                    {
                        // TheOpenOffice.org -> The OpenOffice.org
                        good |= Suggest(slst, StringEx.MakeInitSmall(scw, textInfo), ref onlyCompoundSuggest);

                        if (opLimiter.QueryForCancellation())
                        {
                            goto result;
                        }
                    }

                    wspace = StringEx.MakeAllSmall(scw, textInfo);
                    if (Check(wspace))
                    {
                        InsertSuggestion(slst, wspace);
                    }

                    var prevns = slst.Count;
                    good |= Suggest(slst, wspace, ref onlyCompoundSuggest);

                    if (opLimiter.QueryForCancellation())
                    {
                        goto result;
                    }

                    if (capWords)
                    {
                        wspace = StringEx.MakeInitCap(wspace, textInfo);
                        if (Check(wspace))
                        {
                            InsertSuggestion(slst, wspace);
                        }

                        good |= Suggest(slst, wspace, ref onlyCompoundSuggest);

                        if (opLimiter.QueryForCancellation())
                        {
                            goto result;
                        }
                    }

                    // aNew -> "a New" (instead of "a new")
                    for (var j = prevns; j < slst.Count; j++)
                    {
                        var toRemove = slst[j];
                        var spaceIndex = toRemove.IndexOf(' ');
                        if (spaceIndex >= 0)
                        {
                            var slen = toRemove.Length - spaceIndex - 1;

                            // different case after space (need capitalisation)
                            if (slen < scw.Length && !scw.AsSpan(scw.Length - slen).SequenceEqual(toRemove.AsSpan(spaceIndex + 1)))
                            {
                                // set as first suggestion
                                removeFromIndexThenInsertAtFront(
                                    slst,
                                    j,
                                    StringEx.ConcatString(
                                        toRemove.AsSpan(0, spaceIndex + 1),
                                        textInfo.ToUpper(toRemove[spaceIndex + 1]),
                                        toRemove.AsSpan(spaceIndex + 2)));

                                static void removeFromIndexThenInsertAtFront(List<string> list, int removeIndex, string insertValue)
                                {
                                    if (list.Count != 0)
                                    {
                                        while (removeIndex > 0)
                                        {
                                            var sourceIndex = removeIndex - 1;
                                            list[removeIndex] = list[sourceIndex];
                                            removeIndex = sourceIndex;
                                        }

                                        list[0] = insertValue;
                                    }
                                }
                            }
                        }
                    }

                    break;

                case CapitalizationType.All:
                    wspace = StringEx.MakeAllSmall(scw, textInfo);
                    good |= Suggest(slst, wspace, ref onlyCompoundSuggest);

                    if (opLimiter.QueryForCancellation())
                    {
                        goto result;
                    }

                    if (Affix.KeepCase.HasValue && Check(wspace))
                    {
                        InsertSuggestion(slst, wspace);
                    }

                    wspace = StringEx.MakeInitCap(wspace, textInfo);
                    good |= Suggest(slst, wspace, ref onlyCompoundSuggest);

                    if (opLimiter.QueryForCancellation())
                    {
                        goto result;
                    }

                    for (var j = 0; j < slst.Count; j++)
                    {
                        slst[j] = StringEx.MakeAllCap(slst[j], textInfo).Replace("ß", "SS");
                    }

                    break;
            }

            // LANG_hu section: replace '-' with ' ' in Hungarian
            if (Affix.IsHungarian)
            {
                for (var j = 0; j < slst.Count; j++)
                {
                    var sitem = slst[j];
                    var pos = sitem.IndexOf('-');
                    if (pos >= 0)
                    {
                        var desiredChar = CheckDetails(sitem.Remove(pos, 1)).Info.HasFlagEx(SpellCheckResultType.Compound | SpellCheckResultType.Forbidden)
                            ? ' '
                            : '-';

                        if (sitem[pos] != desiredChar)
                        {
                            slst[j] = StringEx.ConcatString(sitem.AsSpan(0, pos), desiredChar, sitem.AsSpan(pos + 1));
                        }
                    }
                }
            }

            // END OF LANG_hu section
            // try ngram approach since found nothing good suggestion
            if (!good && Affix.MaxNgramSuggestions != 0 && (slst.Count == 0 || onlyCompoundSuggest))
            {
                switch (capType)
                {
                    case CapitalizationType.None:
                        NGramSuggest(slst, scw, capType);
                        break;

                    case CapitalizationType.HuhInit:
                        capWords = true;
                        goto case CapitalizationType.Huh;

                    case CapitalizationType.Huh:
                        NGramSuggest(slst, StringEx.MakeAllSmall(scw, textInfo), CapitalizationType.Huh);
                        break;

                    case CapitalizationType.Init:
                        capWords = true;
                        NGramSuggest(slst, StringEx.MakeAllSmall(scw, textInfo), capType);
                        break;

                    case CapitalizationType.All:
                        var j = slst.Count;
                        NGramSuggest(slst, StringEx.MakeAllSmall(scw, textInfo), capType);

                        for (; j < slst.Count; j++)
                        {
                            slst[j] = StringEx.MakeAllCap(slst[j], textInfo);
                        }

                        break;
                }

                if (opLimiter.QueryForCancellation())
                {
                    goto result;
                }
            }

            // try dash suggestion (Afo-American -> Afro-American)
            // Note: LibreOffice was modified to treat dashes as word
            // characters to check "scot-free" etc. word forms, but
            // we need to handle suggestions for "Afo-American", etc.,
            // while "Afro-American" is missing from the dictionary.
            // TODO avoid possible overgeneration
            var dashPos = scw.IndexOf('-');
            if (dashPos >= 0)
            {
                var noDashSug = true;
                for (var j = 0; j < slst.Count; j++)
                {
                    if (slst[j].Contains('-'))
                    {
                        noDashSug = false;
                        break;
                    }
                }

                var prevPos = 0;
                var last = false;

                while (!good && noDashSug && !last)
                {
                    if (dashPos == scw.Length)
                    {
                        last = true;
                    }

                    var chunk = scw.AsSpan(prevPos, dashPos - prevPos);
                    if (!chunk.SequenceEqual(word) && !Check(chunk))
                    {
                        var nlst = SuggestNested(chunk);

                        foreach (var j in nlst)
                        {
                            wspace = last
                                ? scw.AsSpan(0, prevPos).ConcatString(j)
                                : StringEx.ConcatString(scw.AsSpan(0, prevPos), j, '-', scw.AsSpan(dashPos + 1));

                            var info = SpellCheckResultType.None;
                            if (Affix.ForbiddenWord.HasValue)
                            {
                                _ = _query.CheckWord(wspace, ref info, out _);
                            }

                            if (info.IsMissingFlag(SpellCheckResultType.Forbidden))
                            {
                                InsertSuggestion(slst, wspace);
                            }
                        }

                        noDashSug = false;
                    }

                    if (!last)
                    {
                        prevPos = dashPos + 1;
                        dashPos = scw.IndexOf('-', prevPos);
                    }

                    if (dashPos < 0)
                    {
                        dashPos = scw.Length;
                    }
                }
            }

        result:

            // word reversing wrapper for complex prefixes
            if (Affix.ComplexPrefixes)
            {
                for (var j = 0; j < slst.Count; j++)
                {
                    slst[j] = slst[j].GetReversed();
                }
            }

            // capitalize
            if (capWords)
            {
                for (var j = 0; j < slst.Count; j++)
                {
                    slst[j] = StringEx.MakeInitCap(slst[j], textInfo);
                }
            }

            // expand suggestions with dot(s)
            if (abbv != 0 && Affix.SuggestWithDots && word.Length >= abbv)
            {
                for (var j = 0; j < slst.Count; j++)
                {
                    slst[j] = slst[j].ConcatString(word.Slice(word.Length - abbv));
                }
            }

            // remove bad capitalized and forbidden forms
            if (
                (Affix.KeepCase.HasValue || Affix.ForbiddenWord.HasValue)
                &&
                (capType is (CapitalizationType.Init or CapitalizationType.All))
            )
            {
                var l = 0;
                for (var j = 0; j < slst.Count; j++)
                {
                    var sitem = slst[j];
                    if (!sitem.Contains(' ') && !Check(sitem))
                    {
                        var s = StringEx.MakeAllSmall(sitem, textInfo);
                        if (Check(s))
                        {
                            slst[l++] = s;
                        }
                        else
                        {
                            s = StringEx.MakeInitCap(s, textInfo);
                            if (Check(s))
                            {
                                slst[l++] = s;
                            }
                        }
                    }
                    else
                    {
                        slst[l++] = sitem;
                    }
                }

                slst.RemoveRange(l, slst.Count - l);
            }

            // remove duplications
            slst.RemoveDuplicates(Affix.StringComparer);

            // output conversion
            Affix.OutputConversions.ConvertAll(slst);

            return slst;
        }

        private readonly List<string> SuggestNested(ReadOnlySpan<char> word) => new QuerySuggest(this).Suggest(word);

        private readonly bool Check(string word) => new QueryCheck(_query).Check(word);

        private readonly bool Check(ReadOnlySpan<char> word) => new QueryCheck(_query).Check(word);

        private readonly bool TryLookupFirstDetail(ReadOnlySpan<char> word, out WordEntryDetail wordEntryDetail) => WordList.TryGetFirstEntryDetailByRootWord(word, out wordEntryDetail);

        private readonly bool TryLookupFirstDetail(string word, out WordEntryDetail wordEntryDetail) => WordList.TryFindFirstEntryDetailByRootWord(word, out wordEntryDetail);

        private ref struct SuggestState
        {
            public SuggestState(string word, List<string> slst)
            {
                SuggestionList = slst;
                _rawCandidateBuffer = ArrayPool<char>.Shared.Rent(word.Length + 2);
                Word = word.AsSpan();
                CandidateBuffer = _rawCandidateBuffer.AsSpan(0, word.Length + 2);
                Info = SpellCheckResultType.None;
                CpdSuggest = 0;
                GoodSuggestion = false;
            }

            public List<string> SuggestionList;
            private char[] _rawCandidateBuffer;
            public ReadOnlySpan<char> Word;
            public Span<char> CandidateBuffer;
            public SpellCheckResultType Info;
            public byte CpdSuggest;
            public bool GoodSuggestion;

            public readonly bool IsCpdSuggest => CpdSuggest != 0;

            public readonly Span<char> GetBufferForWord()
            {
                var result = CandidateBuffer.Slice(0, Word.Length);
                Word.CopyTo(result);
                return result;
            }

            public void DestroyBuffer()
            {
                if (_rawCandidateBuffer.Length != 0)
                {
                    ArrayPool<char>.Shared.Return(_rawCandidateBuffer);
                    _rawCandidateBuffer = [];
                    CandidateBuffer = [];
                }
            }
        }

        /// <summary>
        /// Generate suggestions for a misspelled word
        /// </summary>
        /// <param name="slst">Resulting suggestion list.</param>
        /// <param name="word">The word to base suggestions on.</param>
        /// <param name="onlyCompoundSug">Indicates there may be bad suggestions.</param>
        /// <returns>True when there may be a good suggestion.</returns>
        internal bool Suggest(List<string> slst, string word, ref bool onlyCompoundSug)
        {
            var noCompoundTwoWords = false; // no second or third loops, see below
            var nSugOrig = slst.Count;
            var oldSug = 0;

            // word reversing wrapper for complex prefixes
            if (Affix.ComplexPrefixes)
            {
                word = word.GetReversed();
            }

            // three loops:
            // - the first without compounding,
            // - the second one with 2-word compounding,
            // - the third one with 3-or-more-word compounding
            // Run second and third loops only if:
            // - no ~good suggestion in the first loop
            // - not for testing compound words with 3 or more words (test_simplesug == false)

            var state = new SuggestState(word, slst);

            for (state.CpdSuggest = 0; state.CpdSuggest < 3 && !noCompoundTwoWords; state.CpdSuggest++)
            {
                // initialize both in non-compound and compound cycles
                var opLimiter = new OperationTimedLimiter(Options.TimeLimitCompoundSuggest, _query.CancellationToken);

                // limit compound suggestion
                if (state.CpdSuggest > 0)
                {
                    oldSug = slst.Count;
                }

                var sugLimit = oldSug + MaxCompoundSuggestions;

                // suggestions for an uppercase word (html -> HTML)
                if (slst.Count < MaxSuggestions)
                {
                    var i = slst.Count;
                    CapChars(word, ref state);
                    if (slst.Count > i)
                    {
                        state.GoodSuggestion = true;
                    }
                }

                // perhaps we made a typical fault of spelling
                if (slst.Count < MaxSuggestions && (!state.IsCpdSuggest || slst.Count < sugLimit))
                {
                    var i = slst.Count;
                    ReplChars(word, ref state);
                    if (slst.Count > i)
                    {
                        state.GoodSuggestion = true;
                        if (state.Info.HasFlagEx(SpellCheckResultType.BestSug))
                        {
                            goto bestSug;
                        }
                    }
                }

                if (opLimiter.QueryForCancellation()) goto timerExit;
                if (_testSimpleSuggestion && slst.Any()) goto testSimpleSugExit;

                // perhaps we made chose the wrong char from a related set
                if (slst.Count < MaxSuggestions && (!state.IsCpdSuggest || slst.Count < sugLimit))
                {
                    MapChars(word, ref state);
                }

                if (opLimiter.QueryForCancellation()) goto timerExit;
                if (_testSimpleSuggestion && slst.Any()) goto testSimpleSugExit;

                // only suggest compound words when no other ~good suggestion
                if (state.CpdSuggest == 0 && slst.Count > nSugOrig)
                {
                    noCompoundTwoWords = true;
                }

                // did we swap the order of chars by mistake
                if (slst.Count < MaxSuggestions && (!state.IsCpdSuggest || slst.Count < sugLimit))
                {
                    SwapChar(ref state);
                }

                if (opLimiter.QueryForCancellation()) goto timerExit;
                if (_testSimpleSuggestion && slst.Any()) goto testSimpleSugExit;

                // did we swap the order of non adjacent chars by mistake
                if (slst.Count < MaxSuggestions && (!state.IsCpdSuggest || slst.Count < sugLimit))
                {
                    LongSwapChar(ref state);
                }

                if (opLimiter.QueryForCancellation()) goto timerExit;
                if (_testSimpleSuggestion && slst.Any()) goto testSimpleSugExit;

                // did we just hit the wrong key in place of a good char (case and keyboard)
                if (slst.Count < MaxSuggestions && (!state.IsCpdSuggest || slst.Count < sugLimit))
                {
                    BadCharKey(ref state);
                }

                if (opLimiter.QueryForCancellation()) goto timerExit;
                if (_testSimpleSuggestion && slst.Any()) goto testSimpleSugExit;

                // did we add a char that should not be there
                if (slst.Count < MaxSuggestions && (!state.IsCpdSuggest || slst.Count < sugLimit))
                {
                    ExtraChar(ref state);
                }

                if (opLimiter.QueryForCancellation()) goto timerExit;
                if (_testSimpleSuggestion && slst.Any()) goto testSimpleSugExit;

                // did we forgot a char
                if (slst.Count < MaxSuggestions && (!state.IsCpdSuggest || slst.Count < sugLimit))
                {
                    ForgotChar(ref state);
                }

                if (opLimiter.QueryForCancellation()) goto timerExit;
                if (_testSimpleSuggestion && slst.Any()) goto testSimpleSugExit;

                // did we move a char
                if (slst.Count < MaxSuggestions && (!state.IsCpdSuggest || slst.Count < sugLimit))
                {
                    MoveChar(ref state);
                }

                if (opLimiter.QueryForCancellation()) goto timerExit;
                if (_testSimpleSuggestion && slst.Any()) goto testSimpleSugExit;

                // did we just hit the wrong key in place of a good char
                if (slst.Count < MaxSuggestions && (!state.IsCpdSuggest || slst.Count < sugLimit))
                {
                    BadChar(ref state);
                }

                if (opLimiter.QueryForCancellation()) goto timerExit;
                if (_testSimpleSuggestion && slst.Any()) goto testSimpleSugExit;

                // did we double two characters
                if (slst.Count < MaxSuggestions && (!state.IsCpdSuggest || slst.Count < sugLimit))
                {
                    DoubleTwoChars(ref state);
                }

                if (opLimiter.QueryForCancellation()) goto timerExit;
                if (_testSimpleSuggestion && slst.Any()) goto testSimpleSugExit;

                // perhaps we forgot to hit space and two words ran together
                // (dictionary word pairs have top priority here, so
                // we always suggest them, in despite of nosplitsugs, and
                // drop compound word and other suggestions)
                if (!state.IsCpdSuggest || (!Affix.NoSplitSuggestions && slst.Count < sugLimit))
                {
                    TwoWords(ref state);

                    if (state.Info.HasFlagEx(SpellCheckResultType.BestSug))
                    {
                        goto bestSug;
                    }
                }

                if (opLimiter.QueryForCancellation()) goto timerExit;

                // testing returns after the first loop
                if (_testSimpleSuggestion)
                {
                    goto testSimpleSugExit;
                }

                // don't need third loop, if the second loop was successful or
                // the first loop found a dictionary-based compound word
                // (we don't need more, likely worse and false 3-or-more-word compound words)
                if (state.CpdSuggest == 1 && (slst.Count > oldSug || state.Info.HasFlagEx(SpellCheckResultType.Compound)))
                {
                    noCompoundTwoWords = true;
                }

            } // repeating ``for'' statement compounding support

            if (!noCompoundTwoWords && slst.Any())
            {
                onlyCompoundSug = true;
            }

            goto actualExit;

        testSimpleSugExit:
            state.GoodSuggestion = slst.Any();
            goto actualExit;

        timerExit:
            goto actualExit;

        bestSug:
            state.GoodSuggestion = true;
            goto actualExit;

        actualExit:
            state.DestroyBuffer();
            return state.GoodSuggestion;
        }

        private readonly SpellCheckResult CheckDetails(string word) => new QueryCheck(_query).CheckDetails(word);

        /// <summary>
        /// perhaps we doubled two characters (pattern aba -> ababa, for example vacation -> vacacation)
        /// </summary>
        /// <remarks>
        /// (for example vacation -> vacacation)
        /// The recognized pattern with regex back-references:
        /// "(.)(.)\1\2\1" or "..(.)(.)\1\2"
        /// </remarks>
        private void DoubleTwoChars(ref SuggestState sugState)
        {
            var word = sugState.Word;
            if (word.Length < 5)
            {
                return;
            }

            var state = 0;
            var candidate = sugState.CandidateBuffer.Slice(0, word.Length - 2);
            for (var i = 2; i < word.Length; i++)
            {
                if (word[i] == word[i - 2])
                {
                    state++;
                    if (state == 3 || (state == 2 && i >= 4))
                    {
                        word.Slice(0, i - 1).CopyTo(candidate);
                        word.Slice(i + 1).CopyTo(candidate.Slice(i - 1));

                        TestSug(candidate, ref sugState);
                        state = 0;
                    }
                }
                else
                {
                    state = 0;
                }
            }
        }

        /// <summary>
        /// Error is wrong char in place of correct one.
        /// </summary>
        private void BadChar(ref SuggestState state)
        {
            if (Affix.TryString is { Length: > 0 } tryString)
            {
                var timer = new OperationTimedCountLimiter(Options.TimeLimitSuggestStep, Options.MinTimer, _query.CancellationToken);

                var candidate = state.GetBufferForWord();

                // swap out each char one by one and try all the tryme
                // chars in its place to see if that makes a good word
                for (var j = 0; j < tryString.Length; j++)
                {
                    for (var i = candidate.Length - 1; i >= 0; i--)
                    {
                        var tmpc = candidate[i];
                        if (tryString[j] == tmpc)
                        {
                            continue;
                        }

                        candidate[i] = tryString[j];
                        TestSug(candidate, ref state, ref timer);
                        candidate[i] = tmpc;

                        if (timer.HasBeenCanceled)
                        {
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Error is a letter was moved.
        /// </summary>
        private void MoveChar(ref SuggestState state)
        {
            var word = state.Word;
            if (word.Length < 2)
            {
                return;
            }

            var candidate = state.GetBufferForWord();

            // try moving a char
            for (var p = 0; p < word.Length; p++)
            {
                word.CopyTo(candidate);

                var qMax = Math.Min(MaxCharDistance + p + 1, candidate.Length);
                for (var q = p + 1; q < qMax; q++)
                {
                    candidate.Swap(q, q - 1);

                    if (q - p < 2)
                    {
                        continue; // omit swap char
                    }

                    TestSug(candidate, ref state);
                }
            }

            for (var p = word.Length - 1; p >= 0; p--)
            {
                word.CopyTo(candidate);

                var qMin = Math.Max(p - MaxCharDistance, 0);
                for (var q = p - 1; q >= qMin; q--)
                {
                    candidate.Swap(q, q + 1);

                    if (p - q < 2)
                    {
                        continue;  // omit swap char
                    }

                    TestSug(candidate, ref state);
                }
            }
        }

        /// <summary>
        /// Error is missing a letter it needs.
        /// </summary>
        private void ForgotChar(ref SuggestState state)
        {
            if (Affix.TryString is { Length: > 0 })
            {
                var timer = new OperationTimedCountLimiter(Options.TimeLimitSuggestStep, Options.MinTimer, _query.CancellationToken);

                var word = state.Word;
                var candidate = state.CandidateBuffer.Slice(0, word.Length + 1);

                // try inserting a tryme character before every letter (and the null terminator)
                foreach (var tryChar in Affix.TryString)
                {
                    word.CopyTo(candidate);
                    candidate[word.Length] = tryChar;

                    TestSug(candidate, ref state, ref timer);

                    if (timer.HasBeenCanceled)
                    {
                        return;
                    }

                    for (var index = word.Length; index >= 0; index--)
                    {
                        candidate[index] = tryChar;
                        word.Slice(index).CopyTo(candidate.Slice(index + 1));

                        TestSug(candidate, ref state, ref timer);

                        if (timer.HasBeenCanceled)
                        {
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Error is word has an extra letter it does not need.
        /// </summary>
        private void ExtraChar(ref SuggestState state)
        {
            var word = state.Word;

            if (word.Length >= 2)
            {
                var buffer = state.CandidateBuffer.Slice(0, word.Length - 1);
                word.Slice(0, word.Length - 1).CopyTo(buffer);

                TestSug(buffer, ref state);

                for (var index = word.Length - 2; index > 0; index--)
                {
                    word.Slice(index + 1).CopyTo(buffer.Slice(index));
                    TestSug(buffer, ref state);
                }

                TestSug(word.Slice(1), ref state);
            }
        }

        /// <summary>
        /// error is wrong char in place of correct one (case and keyboard related version)
        /// </summary>
        private void BadCharKey(ref SuggestState state)
        {
            var candidate = state.GetBufferForWord();
            var keyString = Affix.KeyString;

            // swap out each char one by one and try uppercase and neighbor
            // keyboard chars in its place to see if that makes a good word
            for (var i = 0; i < candidate.Length; i++)
            {
                var tmpc = candidate[i];
                // check with uppercase letters
                candidate[i] = TextInfo.ToUpper(tmpc);
                if (tmpc != candidate[i])
                {
                    TestSug(candidate, ref state);
                    candidate[i] = tmpc;
                }

                // check neighbor characters in keyboard string
                var loc = keyString.IndexOf(tmpc);
                while (loc >= 0)
                {
                    if (loc > 0 && keyString[loc - 1] != '|')
                    {
                        candidate[i] = keyString[loc - 1];
                        TestSug(candidate, ref state);
                    }

                    if ((loc + 1) < keyString.Length && keyString[loc + 1] != '|')
                    {
                        candidate[i] = keyString[loc + 1];
                        TestSug(candidate, ref state);
                    }

                    loc = keyString.IndexOf(tmpc, loc + 1);
                }

                candidate[i] = tmpc;
            }
        }

        /// <summary>
        /// Error is not adjacent letter were swapped.
        /// </summary>
        private void LongSwapChar(ref SuggestState state)
        {
            var candidate = state.GetBufferForWord();

            // try swapping not adjacent chars one by one
            for (var p = 0; p < candidate.Length; p++)
            {
                var oldp = candidate[p];
                var qMax = Math.Min(candidate.Length, p + MaxCharDistance + 1);
                var pLow = p - 1;
                var pHigh = p + 1;
                for (var q = Math.Max(0, p - MaxCharDistance); q < qMax; q++)
                {
                    if (q < pLow || q > pHigh)
                    {
                        var oldq = candidate[q];
                        candidate[p] = oldq;
                        candidate[q] = oldp;

                        TestSug(candidate, ref state);

                        candidate[q] = oldq;
                        candidate[p] = oldp;
                    }
                }
            }
        }

        /// <summary>
        /// Error is adjacent letter were swapped.
        /// </summary>
        private void SwapChar(ref SuggestState state)
        {
            var word = state.Word;
            if (word.Length < 2)
            {
                return;
            }

            var candidate = state.GetBufferForWord();

            // try swapping adjacent chars one by one
            for (var i = 1; i < candidate.Length; i++)
            {
#pragma warning disable IDE0180 // Use tuple to swap values
                var c = candidate[i];
                candidate[i] = candidate[i - 1];
                candidate[i - 1] = c;
                TestSug(candidate, ref state);
                candidate[i - 1] = candidate[i];
                candidate[i] = c;
#pragma warning restore IDE0180 // Use tuple to swap values
            }

            // try double swaps for short words
            // ahev -> have, owudl -> would
            if (candidate.Length == 4 || candidate.Length == 5)
            {
                candidate[0] = word[1];
                candidate[1] = word[0];
                candidate[2] = word[2];
                candidate[candidate.Length - 2] = word[word.Length - 1];
                candidate[candidate.Length - 1] = word[word.Length - 2];

                TestSug(candidate, ref state);

                if (candidate.Length == 5)
                {
                    candidate[0] = word[0];
                    candidate[1] = word[2];
                    candidate[2] = word[1];

                    TestSug(candidate, ref state);
                }
            }
        }

        private void CapChars(string word, ref SuggestState state) =>
            TestSug(StringEx.MakeAllCap(word, TextInfo), ref state);

        private void MapChars(string word, ref SuggestState state)
        {
            if (word.Length < 2 || Affix.RelatedCharacterMap.IsEmpty)
            {
                return;
            }

            var candidate = string.Empty;
            var timer = new OperationTimedCountLimiter(Options.TimeLimitSuggestStep, Options.MinTimer, _query.CancellationToken);
            MapRelated(word, ref candidate, wn: 0, ref state, ref timer, depth: 0);
        }

        private void MapRelated(string word, ref string candidate, int wn, ref SuggestState state, ref OperationTimedCountLimiter timer, int depth)
        {
            if (word.Length == wn)
            {
                if (
                    CanAcceptSuggestion(state.SuggestionList, candidate)
                    &&
                    CheckWord(candidate, state.CpdSuggest, ref timer) != 0
                )
                {
                    state.SuggestionList.Add(candidate);
                }

                return;
            }

            if (depth > Options.RecursiveDepthLimit)
            {
                return;
            }

            var inMap = false;
            foreach (var mapEntry in Affix.RelatedCharacterMap.RawArray)
            {
                foreach (var mapEntryValue in mapEntry.RawArray)
                {
                    if (word.AsSpan(wn).StartsWithOrdinal(mapEntryValue))
                    {
                        inMap = true;
                        var candidatePrefix = candidate;
                        foreach (var otherMapEntryValue in mapEntry.RawArray)
                        {
                            candidate = candidatePrefix + otherMapEntryValue;
                            MapRelated(word, ref candidate, wn + mapEntryValue.Length, ref state, ref timer, depth + 1);

                            if (timer.HasBeenCanceled)
                            {
                                return;
                            }
                        }
                    }
                }
            }

            if (!inMap)
            {
                candidate = StringEx.ConcatString(candidate, word[wn]);
                MapRelated(word, ref candidate, wn + 1, ref state, ref timer, depth + 1);
            }
        }

        private bool TestSug(ReadOnlySpan<char> candidate, ref SuggestState state, ref OperationTimedCountLimiter timer)
        {
            if (CanAcceptSuggestion(state.SuggestionList, candidate))
            {
                var result = CheckWord(candidate, state.CpdSuggest, ref timer);
                if (result != 0)
                {
                    // compound word in the dictionary
                    if (state.CpdSuggest == 0 && result >= 2)
                    {
                        state.Info |= SpellCheckResultType.Compound;
                    }

                    state.SuggestionList.Add(candidate.ToString());
                    return true;
                }
            }

            return false;
        }

        private bool TestSug(string candidate, ref SuggestState state)
        {
            if (CanAcceptSuggestion(state.SuggestionList, candidate))
            {
                var result = CheckWord(candidate, state.CpdSuggest);
                if (result != 0)
                {
                    // compound word in the dictionary
                    if (state.CpdSuggest == 0 && result >= 2)
                    {
                        state.Info |= SpellCheckResultType.Compound;
                    }

                    state.SuggestionList.Add(candidate);
                    return true;
                }
            }

            return false;
        }

        private bool TestSug(ReadOnlySpan<char> candidate, ref SuggestState state)
        {
            if (CanAcceptSuggestion(state.SuggestionList, candidate))
            {
                var result = CheckWord(candidate, state.CpdSuggest);
                if (result != 0)
                {
                    // compound word in the dictionary
                    if (state.CpdSuggest == 0 && result >= 2)
                    {
                        state.Info |= SpellCheckResultType.Compound;
                    }

                    state.SuggestionList.Add(candidate.ToString());
                    return true;
                }
            }

            return false;
        }

        private byte CheckWord(string word, byte cpdSuggest, ref OperationTimedCountLimiter timer)
        {
            return timer.QueryForCancellation() ? (byte)0 : CheckWord(word, cpdSuggest);
        }

        private byte CheckWord(ReadOnlySpan<char> word, byte cpdSuggest, ref OperationTimedCountLimiter timer)
        {
            return timer.QueryForCancellation() ? (byte)0 : CheckWord(word, cpdSuggest);
        }

        private readonly WordEntry? CheckWordHomonymPortion(string word, WordEntryDetail[] rvDetails)
        {
            foreach (var rvDetail in rvDetails)
            {
                if (rvDetail.DoesNotContainAnyFlags(Affix.Flags_NeedAffix_OnlyInCompound_OnlyUpcase))
                {
                    return new WordEntry(word, rvDetail);
                }
            }

            return null;
        }

        private byte CheckWordAffixPortion(WordEntry? rv, ReadOnlySpan<char> word)
        {
            var noSuffix = rv is not null;
            if (!noSuffix)
            {
                rv = _query.SuffixCheck(word, AffixEntryOptions.None, null, default, default, CompoundOptions.Not); // only suffix
            }

            if (rv is null)
            {
                if (Affix.ContClasses.IsEmpty)
                {
                    goto noResult;
                }

                rv = _query.SuffixCheckTwoSfx(word, AffixEntryOptions.None, null, default)
                    ?? _query.PrefixCheckTwoSfx(word, CompoundOptions.Not, default);
            }

            if (rv is not null)
            {
                // check forbidden words
                if (rv.ContainsAnyFlags(Affix.Flags_ForbiddenWord_OnlyUpcase_NoSuggest_OnlyInCompound))
                {
                    goto noResult;
                }

                // XXX obsolete
                if (rv.ContainsFlag(Affix.CompoundFlag))
                {
                    return noSuffix ? (byte)3 : (byte)2;
                }

                return 1;
            }

        noResult:

            return 0;
        }


        /// <summary>
        /// See if a candidate suggestion is spelled correctly
        /// needs to check both root words and words with affixes.
        /// </summary>
        /// <remarks>
        /// Obsolote MySpell-HU modifications:
        /// return value 2 and 3 marks compounding with hyphen (-)
        /// `3' marks roots without suffix
        /// </remarks>
        private byte CheckWord(ReadOnlySpan<char> word, byte cpdSuggest)
        {
            WordEntry? rv;
            if (cpdSuggest >= 1)
            {
                if (Affix.HasCompound)
                {
                    var rwords = IncrementalWordList.GetRoot(); // buffer for COMPOUND pattern checking
                    rv = _query.CompoundCheck(
                        word, 0, 0, 100, rwords, huMovRule: false, isSug: true,
                        info: cpdSuggest == 1 ? SpellCheckResultType.Compound2 : SpellCheckResultType.None); // EXT
                    IncrementalWordList.ReturnRoot(ref rwords);

                    // TODO filter 3-word or more compound words, as in spell()
                    // (it's too slow to call suggest() here for all possible compound words)
                    if (rv is not null && (!TryLookupFirstDetail(word, out var rvDetail) || !rvDetail.ContainsAnyFlags(Affix.Flags_ForbiddenWord_NoSuggest)))
                    {
                        return 3; // XXX obsolote categorisation + only ICONV needs affix flag check?
                    }
                }

                return 0;
            }

            // get homonyms
            if (_query.TryLookupDetails(word, out var wordString, out var rvDetails))
            {
#if DEBUG
                if (rvDetails is not { Length: > 0 }) ExceptionEx.ThrowInvalidOperation();
#endif

                if (rvDetails[0].ContainsAnyFlags(Affix.Flags_ForbiddenWord_NoSuggest_SubStandard))
                {
                    return 0;
                }

                rv = CheckWordHomonymPortion(wordString, rvDetails);
            }
            else
            {
                rv = _query.PrefixCheck(word, CompoundOptions.Not, default); // only prefix, and prefix + suffix XXX
            }

            return CheckWordAffixPortion(rv, word);
        }

        /// <summary>
        /// See if a candidate suggestion is spelled correctly
        /// needs to check both root words and words with affixes.
        /// </summary>
        /// <remarks>
        /// Obsolote MySpell-HU modifications:
        /// return value 2 and 3 marks compounding with hyphen (-)
        /// `3' marks roots without suffix
        /// </remarks>
        private byte CheckWord(string word, byte cpdSuggest)
        {
            WordEntry? rv;
            if (cpdSuggest >= 1)
            {
                if (Affix.HasCompound)
                {
                    var rwords = IncrementalWordList.GetRoot(); // buffer for COMPOUND pattern checking
                    rv = _query.CompoundCheck(
                        word, 0, 0, 100, rwords, huMovRule: false, isSug: true,
                        info: cpdSuggest == 1 ? SpellCheckResultType.Compound2 : SpellCheckResultType.None); // EXT
                    IncrementalWordList.ReturnRoot(ref rwords);

                    // TODO filter 3-word or more compound words, as in spell()
                    // (it's too slow to call suggest() here for all possible compound words)
                    if (rv is not null && (!TryLookupFirstDetail(word, out var rvDetail) || !rvDetail.ContainsAnyFlags(Affix.Flags_ForbiddenWord_NoSuggest)))
                    {
                        return 3; // XXX obsolote categorisation + only ICONV needs affix flag check?
                    }
                }

                return 0;
            }

            // get homonyms
            if (_query.TryLookupDetails(word, out var rvDetails))
            {
#if DEBUG
                if (rvDetails is not { Length: > 0 }) ExceptionEx.ThrowInvalidOperation();
#endif

                if (rvDetails[0].ContainsAnyFlags(Affix.Flags_ForbiddenWord_NoSuggest_SubStandard))
                {
                    return 0;
                }

                rv = CheckWordHomonymPortion(word, rvDetails);
            }
            else
            {
                rv = _query.PrefixCheck(word, CompoundOptions.Not, default); // only prefix, and prefix + suffix XXX
            }

            return CheckWordAffixPortion(rv, word);
        }

        /// <summary>
        /// Suggestions for a typical fault of spelling, that
        /// differs with more, than 1 letter from the right form.
        /// </summary>
        private void ReplChars(string word, ref SuggestState state)
        {
            if (word.Length < 2 || WordList._allReplacements.IsEmpty)
            {
                return;
            }

            foreach (var replacement in WordList._allReplacements.RawArray)
            {
                if (replacement.Pattern.Length == 0)
                {
                    continue;
                }

                // search every occurence of the pattern in the word
                for (
                    var r = word.IndexOf(replacement.Pattern, StringComparison.Ordinal)
                    ;
                    r >= 0
                    ; 
                    r = word.IndexOf(replacement.Pattern, r + 1, StringComparison.Ordinal) // search for the next letter
                )
                {
                    var type = (r == 0) ? ReplacementValueType.Ini : ReplacementValueType.Med;
                    if ((replacement.Pattern.Length + r) == word.Length)
                    {
                        type |= ReplacementValueType.Fin;
                    }

                    while (type != ReplacementValueType.Med && replacement[type] is not { Length: > 0 })
                    {
                        type = (type == ReplacementValueType.Fin && r != 0) ? ReplacementValueType.Med : type - 1;
                    }

                    if (replacement[type] is { Length: > 0 } replacementValue)
                    {
                        var candidate = StringEx.ConcatString(word.AsSpan(0, r), replacementValue, word.AsSpan(r + replacement.Pattern.Length));
                        var sp = candidate.IndexOf(' ');

                        if (TestSug(candidate, ref state))
                        {
                            // REP suggestions are the best, don't search other type of suggestions
                            state.Info |= SpellCheckResultType.BestSug;
                        }

                        // check REP suggestions with space
                        var prev = 0;
                        while (sp >= 0)
                        {
                            if (CheckWord(candidate.AsSpan(prev, sp - prev), cpdSuggest: 0) != 0)
                            {
                                if (TestSug(candidate.AsSpan(sp + 1), ref state))
                                {
                                    state.SuggestionList.ReplaceLast(candidate);
                                }
                            }

                            prev = sp + 1;
                            sp = candidate.IndexOf(' ', prev);
                        }
                    }
                }
            }

            return;
        }

        /// <summary>
        /// Generate a set of suggestions for very poorly spelled words.
        /// </summary>
        private void NGramSuggest(List<string> wlst, string word, CapitalizationType capType)
        {
            int lval;
            int sc;

            // exhaustively search through all root words
            // keeping track of the MAX_ROOTS most similar root words

            var rootsRental = ArrayPool<NGramSuggestSearchRoot>.Shared.Rent(MaxRoots);
            var roots = rootsRental.AsSpan(0, MaxRoots);

            for (var i = 0; i < roots.Length; i++)
            {
                roots[i] = new(i);
            }

            var hasRoots = false;
            var hasRootsPhon = false;
            var lp = roots.Length - 1;
            var lpphon = lp;

            // word reversing wrapper for complex prefixes
            if (Affix.ComplexPrefixes)
            {
                word = word.GetReversed();
            }

            // ofz#59067 a replist entry can generate a very long word, abandon
            // ngram if that odd-edge case arises
            if (word.Length > Options.MaxWordLen * 4)
            {
                return;
            }

            var hasPhoneEntries = Affix.Phone.HasItems;
            var target = hasPhoneEntries
                ? Phonet(StringEx.MakeAllCap(word, TextInfo))
                : string.Empty;
            var isNonGermanLowercase = !Affix.IsGerman && capType == CapitalizationType.None;

            foreach (var hpSet in WordList.GetNGramAllowedDetailsByKeyLength(
                // skip it, if the word length different by 5 or
                // more characters (to avoid strange suggestions)
                minKeyLength: Math.Max(word.Length - 4, 0),
                maxKeyLength: word.Length + 4)
            )
            {
                var wordKeyLengthDifference = Math.Abs(word.Length - hpSet.Key.Length);

                foreach (var hpDetail in hpSet.Value)
                {
                    // don't suggest capitalized dictionary words for
                    // lower case misspellings in ngram suggestions, except
                    // - PHONE usage, or
                    // - in the case of German, where not only proper
                    //   nouns are capitalized, or
                    // - the capitalized word has special pronunciation
                    if (isNonGermanLowercase && !hasPhoneEntries && ((hpDetail.Options & (WordEntryOptions.InitCap | WordEntryOptions.Phon)) == WordEntryOptions.InitCap))
                    {
                        continue;
                    }

                    sc = NGramWithLowering(3, word, hpSet.Key, NGramOptions.LongerWorse)
                        + LeftCommonSubstring(word, hpSet.Key);

                    // check special pronunciation
                    var f = string.Empty;
                    if (hpDetail.Options.HasFlagEx(WordEntryOptions.Phon) && CopyField(ref f, hpDetail.Morphs, MorphologicalTags.Phon))
                    {
                        var sc2 = NGramWithLowering(3, word, f, NGramOptions.LongerWorse)
                            + LeftCommonSubstring(word, f);

                        if (sc2 > sc)
                        {
                            sc = sc2;
                        }
                    }

                    var scphon = -20000;
                    if (hasPhoneEntries && target.Length > 0 && sc > 2 && wordKeyLengthDifference <= 3)
                    {
                        scphon = NGramNoLowering(3, target, Phonet(StringEx.MakeAllCap(hpSet.Key, TextInfo)), NGramOptions.LongerWorse) * 2;
                    }

                    if (sc > roots[lp].Score)
                    {
                        roots[lp].Score = sc;
                        roots[lp].Root = new WordEntry(hpSet.Key, hpDetail);
                        hasRoots = true;
                        lval = sc;
                        for (var j = 0; j < roots.Length; j++)
                        {
                            if (roots[j].Score < lval)
                            {
                                lp = j;
                                lval = roots[j].Score;
                            }
                        }
                    }

                    if (scphon > roots[lpphon].ScorePhone)
                    {
                        roots[lpphon].ScorePhone = scphon;
                        roots[lpphon].RootPhon = hpSet.Key;
                        hasRootsPhon = true;
                        lval = scphon;
                        for (var j = 0; j < roots.Length; j++)
                        {
                            if (roots[j].ScorePhone < lval)
                            {
                                lpphon = j;
                                lval = roots[j].ScorePhone;
                            }
                        }
                    }
                }
            }

            if (!hasRoots && !hasRootsPhon)
            {
                // with no roots there will be no guesses and no point running ngram
                return;
            }

            // find minimum threshold for a passable suggestion
            // mangle original word three differnt ways
            // and score them to generate a minimum acceptable score
            var thresh = 0;

            {
                var wordLowered = StringEx.MakeAllSmall(word, TextInfo);
                var mw = new StringBuilderSpan(wordLowered.Length);
                for (var sp = 1; sp < 4; sp++)
                {
                    mw.Set(wordLowered);

                    for (var k = sp; k < mw.Length; k += 4)
                    {
                        mw[k] = '*';
                    }

                    thresh += NGramNoLowering(word.Length, word, mw.CurrentSpan, NGramOptions.AnyMismatch);
                }

                mw.Dispose();
            }

            thresh = (thresh / 3) - 1;

            // now expand affixes on each of these root words and
            // and use length adjusted ngram scores to select
            // possible suggestions
            var guessesRental = ArrayPool<NGramGuess>.Shared.Rent(MaxGuess);
            var guesses = guessesRental.AsSpan(0, MaxGuess);
            for (var i = 0; i < guesses.Length; i++)
            {
                guesses[i] = new(i);
            }

            lp = guesses.Length - 1;

            var glstRental = ArrayPool<GuessWord>.Shared.Rent(MaxWords);
            var glst = glstRental.AsSpan(0, MaxWords);
            for (var i = 0; i < roots.Length; i++)
            {
                var rp = roots[i].Root;
                if (rp is not null)
                {
                    var field = string.Empty;
                    if (rp.Options.IsMissingFlag(WordEntryOptions.Phon) || !CopyField(ref field, rp.Morphs, MorphologicalTags.Phon))
                    {
                        field = null;
                    }

                    var nw = ExpandRootWord(glst, rp, word, field);

                    for (var k = 0; k < nw; k++)
                    {
                        ref var guessWordK = ref glst[k];

                        if (guessWordK.Word is { Length: > 0 })
                        {
                            sc = NGramWithLowering(word.Length, word, guessWordK.Word, NGramOptions.AnyMismatch)
                                + LeftCommonSubstring(word, guessWordK.Word);

                            if (sc > thresh)
                            {
                                ref var guessNGramLp = ref guesses[lp];
                                if (sc > guessNGramLp.Score)
                                {
                                    guessNGramLp.Score = sc;
                                    guessNGramLp.Guess = guessWordK.Word;
                                    guessNGramLp.GuessOrig = guessWordK.Orig;
                                    lval = sc;
                                    for (var j = 0; j < guesses.Length; j++)
                                    {
                                        ref var guessNGramJ = ref guesses[j];
                                        if (guessNGramJ.Score < lval)
                                        {
                                            lp = j;
                                            lval = guessNGramJ.Score;
                                        }
                                    }

                                    continue;
                                }
                            }
                        }

                        guessWordK.ClearWordAndOrig();
                    }
                }
            }

            ArrayPool<GuessWord>.Shared.Return(glstRental);
            glstRental = null!;
            glst = [];

            // now we are done generating guesses
            // sort in order of decreasing score

            guesses.Sort(NGramGuess.ScoreComparison);

            if (hasPhoneEntries)
            {
                roots.Sort(NGramSuggestSearchRoot.ScorePhoneComparison);
            }

            // weight suggestions with a similarity index, based on
            // the longest common subsequent algorithm and resort

            var fact = 1.0;
            var isSwap = false;

            if (Affix.MaxDifferency.HasValue && Affix.MaxDifferency.GetValueOrDefault() >= 0)
            {
                fact = (10.0 - Affix.MaxDifferency.GetValueOrDefault()) / 5.0;
            }

            for (var i = 0; i < guesses.Length; i++)
            {
                var guess = guesses[i];
                if (guess.Guess is not null)
                {
                    // lowering guess[i]
                    var gl = TextInfo.ToLower(guess.Guess);
                    var len = guess.Guess.Length;

                    var lcsLength = LcsLen(word, gl);

                    // same characters with different casing
                    if (word.Length == len && word.Length == lcsLength)
                    {
                        guesses[i].Score += 2000;
                        break;
                    }

                    // using 2-gram instead of 3, and other weightening
                    var re = NGramNoLowering(2, word, gl, NGramOptions.AnyMismatch | NGramOptions.Weighted) // gl has already been lowered
                        + NGramWithLowering(2, gl, word, NGramOptions.AnyMismatch | NGramOptions.Weighted);

                    guesses[i].Score =
                        // length of longest common subsequent minus length difference
                        (2 * lcsLength) - Math.Abs(word.Length - len)
                        // weight length of the left common substring
                        + LeftCommonSubstring(word, gl)
                        // weight equal character positions
                        + ((CommonCharacterPositions(word, gl, ref isSwap) != 0) ? 1 : 0)
                        // swap character (not neighboring)
                        + (isSwap ? 10 : 0)
                        // ngram
                        + NGramNoLowering(4, word, gl, NGramOptions.AnyMismatch) // gl has already been lowered
                        // weighted ngrams
                        + re
                        // different limit for dictionaries with PHONE rules
                        + (hasPhoneEntries ? (re < len * fact ? -1000 : 0) : (re < (word.Length + len) * fact ? -1000 : 0));
                }
            }

            guesses.Sort(NGramGuess.ScoreComparison);

            // phonetic version
            if (hasPhoneEntries)
            {
                for (var i = 0; i < roots.Length; i++)
                {
                    var root = roots[i];
                    if (root.RootPhon is not null)
                    {
                        // lowering rootphon[i]
                        var gl = StringEx.MakeAllSmall(root.RootPhon, TextInfo);
                        var len = root.RootPhon.Length;

                        // heuristic weigthing of ngram scores
                        roots[i].ScorePhone += 2 * LcsLen(word, gl) - Math.Abs(word.Length - len)
                            // weight length of the left common substring
                            + LeftCommonSubstring(word, gl);
                    }
                }

                roots.Sort(NGramSuggestSearchRoot.ScorePhoneComparison);
            }

            // copy over
            var oldns = wlst.Count;

            var wlstLimit = Math.Min(MaxSuggestions, oldns + Affix.MaxNgramSuggestions);

            var same = false;
            for (var i = 0; i < guesses.Length; i++)
            {
                ref var guess = ref guesses[i];
                if (guess.Guess is not null)
                {
                    if (
                        wlst.Count < wlstLimit
                        &&
                        (
                            !same
                            ||
                            guess.Score > 1000
                        )
                    )
                    {
                        // leave only excellent suggestions, if exists
                        if (guess.Score > 1000)
                        {
                            same = true;
                        }
                        else if (guess.Score < -100)
                        {
                            same = true;
                            // keep the best ngram suggestions, unless in ONLYMAXDIFF mode
                            if (
                                wlst.Count > oldns
                                ||
                                Affix.OnlyMaxDiff
                            )
                            {
                                guess.ClearGuessAndOrig();
                                continue;
                            }
                        }

                        var unique = true;
                        for (var j = 0; j < wlst.Count; j++)
                        {
                            // don't suggest previous suggestions or a previous suggestion with
                            // prefixes or affixes
                            if (
                                (guess.GuessOrig ?? guess.Guess).Contains(wlst[j])
                                || // check forbidden words
                                CheckWord(guess.Guess, cpdSuggest: 0) == 0
                            )
                            {
                                unique = false;
                                break;
                            }
                        }

                        if (unique)
                        {
                            wlst.Add(guess.GuessOrig ?? guess.Guess);
                        }
                    }

                    guess.ClearGuessAndOrig();
                }
            }

            ArrayPool<NGramGuess>.Shared.Return(guessesRental);
            guessesRental = null!;
            guesses = [];

            oldns = wlst.Count;
            wlstLimit = Math.Min(MaxSuggestions, oldns + MaxPhonSugs);

            if (hasPhoneEntries)
            {
                for (var i = 0; i < roots.Length; i++)
                {
                    if (roots[i].RootPhon is { } rootPhon && wlst.Count < wlstLimit)
                    {
                        var unique = true;
                        for (var j = 0; j < wlst.Count; j++)
                        {
                            // don't suggest previous suggestions or a previous suggestion with
                            // prefixes or affixes
                            if (
                                rootPhon.Contains(wlst[j])
                                || // check forbidden words
                                CheckWord(rootPhon, cpdSuggest: 0) == 0
                            )
                            {
                                unique = false;
                                break;
                            }
                        }

                        if (unique)
                        {
                            wlst.Add(rootPhon);
                        }
                    }
                }
            }

            ArrayPool<NGramSuggestSearchRoot>.Shared.Return(rootsRental);
            rootsRental = null!;
            roots = [];
        }

        private readonly int CommonCharacterPositions(string s1, string s2, ref bool isSwap)
        {
            // decapitalize dictionary word
            var t = Affix.ComplexPrefixes
                ? s2.AsSpan(0, s2.Length - 1).ConcatString(TextInfo.ToLower(s2[s2.Length - 1]))
                : StringEx.MakeAllSmall(s2, TextInfo);

            var num = 0;
            var diff = 0;
            var diffPos0 = 0;
            var diffPos1 = 0;
            int i;
            for (i = 0; i < t.Length && i < s1.Length; i++)
            {
                if (s1[i] == t[i])
                {
                    num++;
                }
                else
                {
                    if (diff == 0)
                    {
                        diffPos0 = i;
                    }
                    else if (diff == 1)
                    {
                        diffPos1 = i;
                    }

                    diff++;
                }
            }

            isSwap =
                (
                    diff == 2
                    &&
                    i == t.Length
                    &&
                    i < s1.Length
                    &&
                    s1[diffPos0] == t[diffPos1]
                    &&
                    s1[diffPos1] == t[diffPos0]
                );

            return num;
        }

        /// <summary>
        /// Longest common subsequence.
        /// </summary>
        private static int LcsLen(ReadOnlySpan<char> s, ReadOnlySpan<char> s2)
        {
            var lcsLength = 0;
            var matchCount = s.CountMatchesFromLeft(s2);
            if (matchCount > 0)
            {
                lcsLength = matchCount;
                s = s.Slice(matchCount);
                s2 = s2.Slice(matchCount);
            }

            if (s.Length > 1 || s2.Length > 1)
            {
                matchCount = s.CountMatchesFromRight(s2);
                if (matchCount > 0)
                {
                    lcsLength += matchCount;
                    s = s.Slice(0, s.Length - matchCount);
                    s2 = s2.Slice(0, s2.Length - matchCount);
                }

                if (s.Length > 1 || s2.Length > 1)
                {
                    lcsLength += lcsAlgorithm(s, s2);
                }
            }

            return lcsLength;

            static int lcsAlgorithm(ReadOnlySpan<char> s, ReadOnlySpan<char> s2)
            {
                var nNext = s2.Length + 1;
                var requiredCapacity = (s.Length + 1) * nNext;

                var c = ArrayPool<LongestCommonSubsequenceType>.Shared.Rent(requiredCapacity);
                Array.Clear(c, 0, requiredCapacity);
                var b = ArrayPool<LongestCommonSubsequenceType>.Shared.Rent(requiredCapacity);
                Array.Clear(b, 0, requiredCapacity);

                int i, j;
                for (i = 1; i <= s.Length; i++)
                {
                    var iPrev = i - 1;
                    for (j = 1; j <= s2.Length; j++)
                    {
                        var inj = (i * nNext) + j;
                        ref var cInj = ref c[inj];
                        ref var bInj = ref b[inj];

                        var jPrev = j - 1;
                        var iPrevXNNext = iPrev * nNext;
                        if (s[iPrev] == s2[jPrev])
                        {
                            cInj = c[iPrevXNNext + jPrev] + 1;
                            bInj = LongestCommonSubsequenceType.UpLeft;
                            continue;
                        }

                        var cIPrevXNNext = c[iPrevXNNext + j];
                        var cInjMinux1 = c[inj - 1];
                        if (cIPrevXNNext >= cInjMinux1)
                        {
                            cInj = cIPrevXNNext;
                            bInj = LongestCommonSubsequenceType.Up;
                        }
                        else
                        {
                            cInj = cInjMinux1;
                            bInj = LongestCommonSubsequenceType.UpLeft;
                        }
                    }
                }

                ArrayPool<LongestCommonSubsequenceType>.Shared.Return(c);

                i = s.Length;
                j = s2.Length;
                var len = 0;
                while (i > 0 && j > 0)
                {
                    switch (b[(i * nNext) + j])
                    {
                        case LongestCommonSubsequenceType.UpLeft:
                            len++;
                            i--;
                            j--;
                            break;
                        case LongestCommonSubsequenceType.Up:
                            i--;
                            break;
                        default:
                            j--;
                            break;
                    }
                }

                ArrayPool<LongestCommonSubsequenceType>.Shared.Return(b);

                return len;
            }
        }

        private readonly int ExpandRootWord(Span<GuessWord> wlst, WordEntry entry, string bad, string? phon)
        {
            if (wlst.IsEmpty)
            {
                return 0;
            }

            ref var wlstNh = ref wlst[0];

            var nh = 0;
            // first add root word to list
            if (nh < wlst.Length && entry.DoesNotContainAnyFlags(Affix.Flags_NeedAffix_OnlyInCompound))
            {
                wlstNh = ref wlst[nh];

                wlstNh.Word = entry.Word;
                if (wlstNh.Word is null)
                {
                    return 0;
                }

                wlstNh.Allow = false;
                wlstNh.Orig = null;

                nh++;

                // add special phonetic version
                if (phon is not null && nh < wlst.Length)
                {
                    wlstNh = ref wlst[nh];

                    wlstNh.Word = phon;

#if DEBUG
                    // It should be impossible for wlstNh.Word to be null as phon is already checked.
                    // This case used to execute `return nh - 1;`.
                    // The logic behind removing this check was that phon which is non-null is assigned
                    // directly to it, meaning it too is non-null.
                    if (wlstNh.Word is null) ExceptionEx.ThrowInvalidOperation();
#endif

                    wlstNh.Allow = false;
                    wlstNh.Orig = entry.Word;
                    if (wlstNh.Orig is null)
                    {
                        return nh - 1;
                    }

                    nh++;
                }
            }

            // handle suffixes
            foreach (var sptr in Affix.Suffixes.GetByFlags(entry.Flags))
            {
                if (
                    (
                        sptr.Append.Length == 0
                        ||
                        (
                            bad.Length > sptr.Append.Length
                            &&
                            bad.EndsWith(sptr.Append, StringComparison.Ordinal)
                        )
                    )
                    && // check needaffix flag
                    !sptr.ContainsAnyContClass(Affix.Flags_NeedAffix_OnlyInCompound_Circumfix)
                )
                {
                    var newword = ConcatWithSuffix(entry.Word, sptr);
                    if (newword.Length != 0)
                    {
                        if (nh < wlst.Length)
                        {
                            wlstNh = ref wlst[nh];
                            wlstNh.Word = newword;
                            wlstNh.Allow = sptr.Options.HasFlagEx(AffixEntryOptions.CrossProduct);
                            wlstNh.Orig = null;

                            nh++;

                            // add special phonetic version
                            if (phon is not null && nh < wlst.Length)
                            {
                                wlstNh = ref wlst[nh];
                                wlstNh.Word = phon + sptr.Append;
                                if (wlstNh.Word is null)
                                {
                                    return nh - 1;
                                }

                                wlstNh.Allow = false;
                                wlstNh.Orig = newword;
                                if (wlstNh.Orig is null)
                                {
                                    return nh - 1;
                                }

                                nh++;
                            }
                        }
                    }
                }
            }

            var n = nh;

            // handle cross products of prefixes and suffixes
            if (Affix.Prefixes.HasAffixes)
            {
                for (var j = 1; j < n; j++)
                {
                    if (!wlst[j].Allow)
                    {
                        continue;
                    }

                    foreach (var pfxGroup in Affix.Prefixes.GetGroupsByFlags(entry.Flags))
                    {
                        if (pfxGroup.Options.HasFlagEx(AffixEntryOptions.CrossProduct))
                        {
                            foreach (var cptr in pfxGroup.RawArray)
                            {
                                if (
                                    cptr.Append.Length == 0
                                    ||
                                    (
                                        bad.Length > cptr.Append.Length
                                        &&
                                        bad.StartsWith(cptr.Append, StringComparison.Ordinal)
                                    )
                                )
                                {
                                    if (Add(cptr, wlst[j].Word ?? string.Empty) is { Length: > 0 } newword)
                                    {
                                        if (nh < wlst.Length)
                                        {
                                            wlstNh = ref wlst[nh];
                                            wlstNh.Word = newword;
                                            wlstNh.Allow = pfxGroup.Options.HasFlagEx(AffixEntryOptions.CrossProduct);
                                            wlstNh.Orig = null;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // now handle pure prefixes
            if (Affix.Prefixes.HasAffixes)
            {
                foreach (var ptr in Affix.Prefixes.GetByFlags(entry.Flags))
                {
                    if (
                        (
                            ptr.Append.Length == 0
                            ||
                            (
                                bad.Length > ptr.Append.Length
                                &&
                                bad.StartsWith(ptr.Append, StringComparison.Ordinal)
                            )
                        )
                        && // check needaffix flag
                        !ptr.ContainsAnyContClass(Affix.Flags_NeedAffix_OnlyInCompound_Circumfix)
                    )
                    {
                        var newword = Add(ptr, entry.Word);
                        if (newword.Length != 0 && nh < wlst.Length)
                        {
                            wlstNh = ref wlst[nh];
                            wlstNh.Word = newword;
                            wlstNh.Allow = ptr.Options.HasFlagEx(AffixEntryOptions.CrossProduct);
                            wlstNh.Orig = null;
                            nh++;
                        }
                    }
                }
            }

            return nh;
        }

        /// <summary>
        /// Error if should have been two words.
        /// </summary>
        /// <returns>Trye if there is a dictionary word pair or there was already a good suggestion before calling.</returns>
        private void TwoWords(ref SuggestState state)
        {
            ref var good = ref state.GoodSuggestion;

            if (state.Word.Length < 3)
            {
                good = false;
                return;
            }

            var isHungarianAndNotForbidden = Affix.IsHungarian && !CheckForbidden(state.Word);

            var candidate = new SimulatedCString(state.Word.Length + 2);
            candidate[0] = '\0';
            candidate.WriteChars(state.Word, 1);
            candidate[state.Word.Length + 1] = '\0';

            // split the string into two pieces after every char
            // if both pieces are good words make them a suggestion

            for (var p = 1; p + 1 < candidate.BufferLength; p++)
            {
                candidate[p - 1] = candidate[p];

                // Suggest only word pairs, if they are listed in the dictionary.
                // For example, adding "a lot" to the English dic file will
                // result only "alot" -> "a lot" suggestion instead of
                // "alto, slot, alt, lot, allot, aloft, aloe, clot, plot, blot, a lot".
                // Note: using "ph:alot" keeps the other suggestions:
                // a lot ph:alot
                // alot -> a lot, alto, slot...
                candidate[p] = ' ';
                if (state.CpdSuggest == 0 && CheckWord(candidate.TerminatedSpan, state.CpdSuggest) != 0)
                {
                    // best solution
                    state.Info |= SpellCheckResultType.BestSug;

                    // remove not word pair suggestions
                    if (!good)
                    {
                        good = true;
                        state.SuggestionList.Clear();
                    }

                    state.SuggestionList.Insert(0, candidate.TerminatedSpan.ToString());
                }

                // word pairs with dash?
                if (Affix.IsLanguageWithDashUsage)
                {
                    candidate[p] = '-';
                    if (state.CpdSuggest == 0 && CheckWord(candidate.TerminatedSpan, state.CpdSuggest) != 0)
                    {
                        // best solution
                        state.Info |= SpellCheckResultType.BestSug;

                        // remove not word pair suggestions
                        if (!good)
                        {
                            good = true;
                            state.SuggestionList.Clear();
                        }

                        state.SuggestionList.Insert(0, candidate.TerminatedSpan.ToString());
                    }
                }

                if (!good && !Affix.NoSplitSuggestions && CanAcceptSuggestion(state.SuggestionList))
                {
                    candidate[p] = '\0';

                    var c1 = CheckWord(candidate.TerminatedSpan, state.CpdSuggest);
                    if (c1 != 0)
                    {
                        var c2 = CheckWord(candidate.SliceToTerminatorFromOffset(p + 1), state.CpdSuggest);
                        if (c2 != 0)
                        {
                            // spec. Hungarian code (need a better compound word support)
                            candidate[p] =
                                (
                                    isHungarianAndNotForbidden
                                    && // if 3 repeating letter, use - instead of space
                                    (
                                        (
                                            candidate[p - 1] == candidate[p]
                                            &&
                                            (
                                                (
                                                    p > 1
                                                    &&
                                                    candidate[p - 1] == candidate[p - 2]
                                                )
                                                ||
                                                (
                                                    candidate[p - 1] == candidate[p + 2]
                                                )
                                            )
                                        )
                                        || // or multiple compounding, with more, than 6 syllables
                                        (
                                            c1 == 3
                                            &&
                                            c2 >= 2
                                        )
                                    )
                                ) ? '-' : ' ';

                            AddIfAcceptable(state.SuggestionList, candidate.TerminatedSpan);

                            // add two word suggestion with dash, depending on the language
                            // Note that cwrd doesn't modified for REP twoword sugg.
                            if (
                                !Affix.NoSplitSuggestions
                                &&
                                Affix.IsLanguageWithDashUsage
                                &&
                                p > 1
                                &&
                                candidate.BufferLength - (p + 1) > 1
                            )
                            {
                                candidate[p] = '-';

                                AddIfAcceptable(state.SuggestionList, candidate.TerminatedSpan);
                            }
                        }
                    }
                }
            }

            candidate.Dispose();
        }

        private bool CheckForbidden(ReadOnlySpan<char> word)
        {
            // check forbidden words

            if (_query.PrefixCheck(word, CompoundOptions.Begin, default) is null)
            {
                var rv = _query.SuffixCheck(word, AffixEntryOptions.None, null, default, default, CompoundOptions.Not); // prefix+suffix, suffix
                return (rv is not null && rv.ContainsFlag(Affix.ForbiddenWord));
            }
            else if (WordList.TryGetFirstEntryDetailByRootWord(word, out var rvDetail) && rvDetail.DoesNotContainAnyFlags(Affix.Flags_NeedAffix_OnlyInCompound))
            {
                return rvDetail.ContainsFlag(Affix.ForbiddenWord);
            }

            return false;
        }

        /// <summary>
        /// Add prefix to this word assuming conditions hold.
        /// </summary>
        private readonly string Add(PrefixEntry entry, string word)
        {
            if (word.Length >= entry.Strip.Length || (word.Length == 0 && Affix.FullStrip))
            {
                if (entry.TestCondition(word))
                {
                    if (entry.Strip.Length == 0)
                    {
                        // we have a match so add prefix
                        return string.Concat(entry.Append, word);
                    }

                    if (word.StartsWith(entry.Strip, StringComparison.Ordinal))
                    {
                        // we have a match so add prefix
                        return StringEx.ConcatString(entry.Append, word.AsSpan(entry.Strip.Length));
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Add suffix to this word assuming conditions hold.
        /// </summary>
        private readonly string ConcatWithSuffix(string word, SuffixEntry entry)
        {
            // make sure all conditions match
            if (word.Length > entry.Strip.Length || (word.Length == 0 && Affix.FullStrip))
            {
                if (entry.TestCondition(word))
                {
                    if (entry.Strip.Length == 0)
                    {
                        // we have a match so add suffix
                        return string.Concat(word, entry.Append);
                    }

                    if (word.AsSpan(word.Length - entry.Strip.Length).SequenceEqual(entry.Strip))
                    {
                        // we have a match so add suffix
                        return word.AsSpanRemoveFromEnd(entry.Strip.Length).ConcatString(entry.Append);
                    }
                }
            }

            return string.Empty;
        }

        private readonly bool CanAcceptSuggestion(List<string> suggestions) =>  suggestions.Count < Options.MaxSuggestions;

        private readonly bool CanAcceptSuggestion(List<string> suggestions, string candidate) => CanAcceptSuggestion(suggestions) && !suggestions.Contains(candidate);

        private readonly bool CanAcceptSuggestion(List<string> suggestions, ReadOnlySpan<char> candidate) => CanAcceptSuggestion(suggestions) && !suggestions.Contains(candidate);

        private readonly void AddIfAcceptable(List<string> suggestions, ReadOnlySpan<char> candidate)
        {
            if (CanAcceptSuggestion(suggestions, candidate))
            {
                suggestions.Add(candidate.ToString());
            }
        }

        private static bool CopyField(ref string dest, MorphSet morphs, string var)
        {
            if (morphs.Count > 0)
            {
                var morph = morphs.Join(' ').AsSpan();
                if (morph.Length > 0)
                {
                    var pos = morph.IndexOf(var, StringComparison.Ordinal);
                    if (pos >= 0)
                    {
                        morph = morph.Slice(pos + var.Length);
                        pos = morph.IndexOfAny(' ', '\t', '\n');
                        if (pos >= 0)
                        {
                            morph = morph.Slice(0, pos);
                        }

                        dest = morph.ToString();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Length of the left common substring of s1 and (decapitalised) s2.
        /// </summary>
        private readonly int LeftCommonSubstring(string s1, string s2)
        {
            if (s1.Length == 0 || s2.Length == 0)
            {
                return 0;
            }

            if (Affix.ComplexPrefixes)
            {
                return leftCommonSubstringComplex(s1, s2);
            }

            if (s1[0] != s2[0] && s1[0] != TextInfo.ToLower(s2[0]))
            {
                return 0;
            }

            var minIndex = Math.Min(s1.Length, s2.Length);
            var index = 1;

            for ( ; index < minIndex && s1[index] == s2[index]; index++) ;

            return index;

            static int leftCommonSubstringComplex(string s1, string s2) =>
                (s1[s1.Length - 1] == s2[s2.Length - 1]) ? 1 : 0;
        }

        private readonly int NGramWithLowering(int n, string s1, string s2, NGramOptions opt)
        {
            if (s1.Length == 0)
            {
                return 0;
            }

            return NGramNoLowering(
                n,
                s1,
                StringEx.MakeAllSmall(s2, TextInfo),
                opt);
        }

        private static int NGramNoLowering(int n, ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, NGramOptions opt)
        {
            var nscore = HasFlag(opt, NGramOptions.Weighted)
                ? NGramWeightedSearch(n, s1, s2)
                : NGramNonWeightedSearch(n, s1, s2);

            int ns;
            if (HasFlag(opt, NGramOptions.AnyMismatch))
            {
                ns = Math.Abs(s2.Length - s1.Length) - 2;
            }
            else if (HasFlag(opt, NGramOptions.LongerWorse))
            {
                ns = (s2.Length - s1.Length) - 2;
            }
            else
            {
                ns = 0;
            }

            if (ns > 0)
            {
                nscore -= ns;
            }

            return nscore;
        }

        /// <summary>Calculates a weighted score based on substring matching.</summary>
        /// <param name="n">The maximum size of substrings to generate.</param>
        /// <param name="s1">The text to generate substrings from.</param>
        /// <param name="t">The text to search.</param>
        /// <returns>The score.</returns>
        /// <remarks>
        /// This algorithm calculates a score which is based on all substrings in <paramref name="s1"/> that have a
        /// length between 1 and <paramref name="n"/>, that are also found in <paramref name="t"/>. The score is
        /// calculated as the number of substrings found minus the number of substrings that are not found. Also,
        /// for the substrings that are aligned to the beginning of s1 or the end of s1 the penalty for them not
        /// being found is doubled.
        ///
        /// To use an example, and invocation of (2, "nano", "banana") would produce 7 subrstrings to check and 5 would be found,
        /// resulting in a score of 1. The produced set of subrstrings would be: "n", "na", "a", "an", "n", "no", and "o".
        /// The reason the score is 1 instead of 3 is because the last two substrings deduct double from the score because they are
        /// aligned to the end of s1.
        /// Also note that in this example, the substring "n" from <paramref name="s1"/> is checked against <paramref name="t"/>
        /// twice and counted twice.
        /// </remarks>
        private static int NGramWeightedSearch(int n, ReadOnlySpan<char> s1, ReadOnlySpan<char> t)
        {
            // all substrings are left aligned for this first iteration so anything not matching needs to be double counted
            var needle = s1.Limit(n);
            var matchLength = FindLongestSubstringMatch(needle, t);
            var nscore = matchLength - ((needle.Length - matchLength) * 2);

            while (s1.Length > 1)
            {
                s1 = s1.Slice(1);

                needle = s1.Limit(n);
                matchLength = FindLongestSubstringMatch(needle, t);

                nscore += matchLength - (needle.Length - matchLength);

                if (needle.Length == s1.Length && needle.Length > matchLength)
                {
                    // in this case only the largest substring can be aligned to the end of s1 for double counting
                    nscore--;
                }
            }

            return nscore;
        }

        /// <summary>Calculates a non-weighted score based on substring matching.</summary>
        /// <param name="n">The maximum size of substrings to generate.</param>
        /// <param name="s1">The text to generate substrings from.</param>
        /// <param name="s2">The text to search.</param>
        /// <returns>The score.</returns>
        /// <remarks>
        /// This algorithm calculates a score which is the number of all substrings in <paramref name="s1"/> that have a
        /// length between 1 and <paramref name="n"/>, that are also found in <paramref name="s2"/>.
        ///
        /// To use an example, and invocation of (2, "nano", "banana") would produce 7 subrstrings to check and 5 would be found,
        /// resulting in a score of 5. The produced set of subrstrings would be: "n", "na", "a", "an", "n", "no", and "o".
        /// Note that in this example, the substring "n" from <paramref name="s1"/> is checked against <paramref name="s2"/> twice and counted twice.
        /// </remarks>
        private static int NGramNonWeightedSearch(int n, ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
        {
            var nscore = 0;

            do
            {
                nscore += FindLongestSubstringMatch(s1.Limit(n), s2);
                s1 = s1.Slice(1);
            }
            while (s1.Length > 0);

            return nscore;
        }

        private static int FindLongestSubstringMatch(ReadOnlySpan<char> needle, ReadOnlySpan<char> haystack)
        {
            // This brute force algorithm leans heavily on the performance benefits of IndexOf.
            // As an optimization, break out when a better result is not possible.

            var best = 0;
            var searchIndex = haystack.IndexOf(needle[0]);
            while (searchIndex >= 0)
            {
                haystack = haystack.Slice(searchIndex);

                for (searchIndex = best + 1; searchIndex < haystack.Length && searchIndex < needle.Length && needle[searchIndex] == haystack[searchIndex]; searchIndex++) ;

                if (searchIndex > best)
                {
                    best = searchIndex;

                    if (best >= needle.Length)
                    {
                        break;
                    }
                }

                searchIndex = haystack.IndexOf(needle.Slice(0, best + 1), 1);
            }

            return best;
        }

        /// <summary>
        /// Do phonetic transformation.
        /// </summary>
        /// <param name="inword">An uppercase string.</param>
        /// <remarks>
        /// Phonetic transcription algorithm
        /// see: http://aspell.net/man-html/Phonetic-Code.html
        /// convert string to uppercase before this call
        /// </remarks>
        private readonly string Phonet(string inword)
        {
            if (inword.Length > Options.MaxPhoneTLen)
            {
                return string.Empty;
            }

            var word = new SimulatedCString(inword);
            var target = new StringBuilderSpan(inword.Length);

            // check word

            // int len = inword.Length;
            int i = 0;
            int k = 0;
            int k0;
            int p;
            int p0 = -333;
            char c;
            char c0;
            bool z = false;
            bool z0;

            while (i < word.BufferLength && (c = word[i]) != '\0')
            {
                z0 = false;

                // check all rules for the same letter
                foreach (var phoneEntry in Affix.Phone.GetInternalArrayByFirstRuleChar(c))
                {
                    // check whole string
                    k = 1; // number of found letters
                    p = 5; // default priority
                    var sString = phoneEntry.Rule;
                    var sIndex = 1; // important for (see below)  "*(s-1)"
                    var sChar = sString.GetCharOrTerminator(sIndex);

                    while (sChar != '\0' && word[i + k] == sChar && !isAsciiDigit(sChar) && notConditionMarkup(sChar))
                    {
                        k++;
                        sChar = sString.GetCharOrTerminator(++sIndex);
                    }

                    if (sChar == '(')
                    {
                        // check letters in "(..)"
                        if (
                            StringEx.MyIsAlpha(word[i + k]) // NOTE: could be implied?
                            &&
                            (sIndex + 1) < sString.Length
                            &&
                            sString.IndexOf(word[i + k], sIndex + 1) >= 0
                        )
                        {
                            k++;
                            while (sChar != ')' && sChar != '\0')
                            {
                                sChar = sString.GetCharOrTerminator(++sIndex);
                            }

                            // It's safe to Assume if (sChar == ')') because the length check is built into GetCharOrTerminator
                            sChar = sString.GetCharOrTerminator(++sIndex);
                        }
                    }

                    p0 = sChar;
                    k0 = k;

                    while (sChar == '-' && k > 1)
                    {
                        k--;
                        sChar = sString.GetCharOrTerminator(++sIndex);
                    }

                    if (sChar == '<')
                    {
                        sChar = sString.GetCharOrTerminator(++sIndex);
                    }

                    if (isAsciiDigit(sChar))
                    {
                        // determine priority
                        p = sChar - '0';
                        sChar = sString.GetCharOrTerminator(++sIndex);
                    }

                    if (sChar == '^' && sString.GetCharOrTerminator(sIndex + 1) == '^')
                    {
                        sChar = sString.GetCharOrTerminator(++sIndex);
                    }

                    if (
                        sChar == '\0'
                        ||
                        (
                            sChar == '^'
                            &&
                            (
                                i == 0
                                ||
                                !StringEx.MyIsAlpha(word[i - 1])
                            )
                            &&
                            (
                                sString.GetCharOrTerminator(sIndex + 1) != '$'
                                ||
                                !StringEx.MyIsAlpha(word[i + k0])
                            )
                        )
                        ||
                        (
                            sChar == '$'
                            &&
                            i > 0
                            &&
                            StringEx.MyIsAlpha(word[i - 1])
                            &&
                            !StringEx.MyIsAlpha(word[i + k0])
                        )
                    )
                    {
                        // search for followup rules, if:
                        // parms.followup and k > 1  and  NO '-' in searchstring

                        c0 = word[i + k - 1];

                        if (k > 1 && p0 != '-' && word[i + k] != '\0')
                        {
                            // test follow-up rule for "word[i+k]"
                            foreach (var phoneEntryNested in Affix.Phone.GetInternalArrayByFirstRuleChar(c0))
                            {
                                // check whole string
                                k0 = k;
                                p0 = 5;
                                sString = phoneEntryNested.Rule;
                                sChar = sString.GetCharOrTerminator(++sIndex);

                                while (sChar != '\0' && word[i + k0] == sChar && !isAsciiDigit(sChar) && notConditionMarkup(sChar))
                                {
                                    k0++;
                                    sChar = sString.GetCharOrTerminator(++sIndex);
                                }

                                if (sChar == '(')
                                {
                                    // check letters
                                    if (
                                        (sIndex + 1) < sString.Length
                                        && StringEx.MyIsAlpha(word[i + k0])
                                        && sString.IndexOf(word[i + k0], sIndex + 1) >= 0)
                                    {
                                        k0++;
                                        while (sChar != ')' && sChar != '\0')
                                        {
                                            sChar = sString.GetCharOrTerminator(++sIndex);
                                        }

                                        if (sChar == ')')
                                        {
                                            sChar = sString.GetCharOrTerminator(++sIndex);
                                        }
                                    }
                                }

                                while (sChar == '-')
                                {
                                    // "k0" gets NOT reduced
                                    // because "if (k0 == k)"
                                    sChar = sString.GetCharOrTerminator(++sIndex);
                                }

                                if (sChar == '<')
                                {
                                    sChar = sString.GetCharOrTerminator(++sIndex);
                                }

                                if (isAsciiDigit(sChar))
                                {
                                    p0 = sChar - '0';
                                    sChar = sString.GetCharOrTerminator(++sIndex);
                                }

                                if (
                                    sChar == '\0'
                                    ||
                                    (
                                        sChar == '$'
                                        &&
                                        !StringEx.MyIsAlpha(word[i + k0])
                                    )
                                )
                                {
                                    if (k0 == k)
                                    {
                                        // this is just a piece of the string
                                        continue;
                                    }

                                    if (p0 < p)
                                    {
                                        // priority too low
                                        continue;
                                    }

                                    break;
                                }
                            }

                            if (p0 >= p)
                            {
                                continue;
                            }
                        }

                        // replace string
                        sString = phoneEntry.Replace;
                        sIndex = 0;
                        sChar = sString.GetCharOrTerminator(sIndex);
                        p0 = phoneEntry.Rule.IndexOf('<', 1) >= 0 ? 1 : 0;

                        if (p0 == 1 && !z)
                        {
                            // rule with '<' is used
                            if (target.Length != 0 && sChar != '\0' && (target.EndsWith(c) || target.EndsWith(sChar)))
                            {
                                target.Remove(target.Length - 1, 1);
                            }

                            z0 = true;
                            z = true;
                            k0 = 0;

                            while (sChar != '\0' && word[i + k0] != '\0')
                            {
                                word[i + k0] = sChar;
                                k0++;
                                sChar = sString.GetCharOrTerminator(++sIndex);
                            }

                            if (k > k0)
                            {
                                word.RemoveRange(i + k0, k - k0);
                            }

                            c = word[i];
                        }
                        else
                        {
                            // no '<' rule used
                            i += k - 1;
                            z = false;
                            while (sChar != '\0' && sString.GetCharOrTerminator(sIndex + 1) != '\0' && target.Length < inword.Length)
                            {
                                if (target.Length == 0 || !target.EndsWith(sChar))
                                {
                                    target.Append(sChar);
                                }

                                sChar = sString.GetCharOrTerminator(++sIndex);
                            }

                            // new "actual letter"
                            c = sChar;

                            if (phoneEntry.Rule.Contains("^^"))
                            {
                                if (c != '\0')
                                {
                                    target.Append(c);
                                }

                                word.RemoveRange(0, i + 1);

                                inword = ""; // len = 0
                                z0 = true;
                            }
                        }

                        break;
                    }
                }

                if (!z0)
                {
                    if (k != 0 && p0 == 0 && target.Length < inword.Length && c != '\0')
                    {
                        // condense only double letters
                        target.Append(c);
                    }

                    i++;
                    z = false;
                    k = 0;
                }
            }

            word.Dispose();
            return target.GetStringAndDispose();

            static bool notConditionMarkup(char c) => c is not '(' or '-' or '<' or '^' or '$';

            static bool isAsciiDigit(char c) => c is >= '0' and <= '9';
        }

        private static void InsertSuggestion(List<string> slst, string word)
        {
            slst.Insert(0, word);
        }

        [DebuggerDisplay("Score = {Score}, {Root}")]
        private struct NGramSuggestSearchRoot
        {
            public static int ScorePhoneComparison(NGramSuggestSearchRoot x, NGramSuggestSearchRoot y) => y.ScorePhone.CompareTo(x.ScorePhone);

            public NGramSuggestSearchRoot(int i)
            {
                Root = null;
                Score = -100 * i;
                RootPhon = null;
                ScorePhone = Score;
            }

            public WordEntry? Root;

            public string? RootPhon;

            public int Score;

            public int ScorePhone;
        }

        [DebuggerDisplay("Score = {Score}, Guess = {Guess}")]
        private struct NGramGuess
        {
            public static int ScoreComparison(NGramGuess x, NGramGuess y) => y.Score.CompareTo(x.Score);

            public NGramGuess(int i)
            {
                Guess = null;
                GuessOrig = null;
                Score = -100 * i;
            }

            public string? Guess;

            public string? GuessOrig;

            public int Score;

            public void ClearGuessAndOrig()
            {
                Guess = null;
                GuessOrig = null;
            }
        }

        [DebuggerDisplay("Word = {Word}")]
        private struct GuessWord
        {
            public string? Word;

            public string? Orig;

            public bool Allow;

            public void ClearWordAndOrig()
            {
                Word = null;
                Orig = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasFlag(NGramOptions value, NGramOptions flag) => (value & flag) == flag;

        [Flags]
        private enum NGramOptions : byte
        {
            None = 0,
            LongerWorse = 1 << 0,
            AnyMismatch = 1 << 1,
            [Obsolete("Use specialized methods instead")]
            Lowering = 1 << 2,
            Weighted = 1 << 3
        }

        public enum LongestCommonSubsequenceType : byte
        {
            Up = 0,
            Left = 1,
            UpLeft = 2
        }
    }
}

