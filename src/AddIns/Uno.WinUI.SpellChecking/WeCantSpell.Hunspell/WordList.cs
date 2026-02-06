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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace WeCantSpell.Hunspell;

[DebuggerDisplay("RootCount = {RootCount}")]
public sealed partial class WordList
{
    public static WordList CreateFromStreams(Stream dictionaryStream, Stream affixStream) =>
        WordListReader.Read(dictionaryStream, affixStream);

    public static WordList CreateFromFiles(string dictionaryFilePath) =>
        WordListReader.ReadFile(dictionaryFilePath);

    public static WordList CreateFromFiles(string dictionaryFilePath, string affixFilePath) =>
        WordListReader.ReadFile(dictionaryFilePath, affixFilePath);

    public static Task<WordList> CreateFromStreamsAsync(Stream dictionaryStream, Stream affixStream, CancellationToken cancellationToken = default) =>
        WordListReader.ReadAsync(dictionaryStream, affixStream, cancellationToken);

    public static Task<WordList> CreateFromFilesAsync(string dictionaryFilePath, CancellationToken cancellationToken = default) =>
        WordListReader.ReadFileAsync(dictionaryFilePath, cancellationToken);

    public static Task<WordList> CreateFromFilesAsync(string dictionaryFilePath, string affixFilePath, CancellationToken cancellationToken = default) =>
        WordListReader.ReadFileAsync(dictionaryFilePath, affixFilePath, cancellationToken);

    public static WordList CreateFromWords(IEnumerable<string> words)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(words);
#else
        ExceptionEx.ThrowIfArgumentNull(words, nameof(words));
#endif

        return CreateFromWords(words, new AffixConfig.Builder().Extract());
    }

    public static WordList CreateFromWords(IEnumerable<string> words, AffixConfig affix)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(words);
        ArgumentNullException.ThrowIfNull(affix);
#else
        ExceptionEx.ThrowIfArgumentNull(words, nameof(words));
        ExceptionEx.ThrowIfArgumentNull(affix, nameof(affix));
#endif

        var wordListBuilder = new Builder(affix);

        wordListBuilder.InitializeEntriesByRoot(words.GetNonEnumeratedCountOrDefault());

        foreach (var word in words)
        {
            wordListBuilder.Add(word);
        }

        return wordListBuilder.Extract();
    }

    private WordList(
        AffixConfig affix,
        TextDictionary<WordEntryDetail[]> entriesByRoot,
        FlagSet nGramRestrictedFlags,
        SingleReplacementSet allReplacements
    )
    {
        _affix = affix;
        _entriesByRoot = entriesByRoot;
        _nGramRestrictedFlags = nGramRestrictedFlags;
        _allReplacements = allReplacements;
    }

    private readonly AffixConfig _affix;
    private readonly TextDictionary<WordEntryDetail[]> _entriesByRoot;
    private readonly FlagSet _nGramRestrictedFlags;
    private readonly SingleReplacementSet _allReplacements;

    public AffixConfig Affix => _affix;

    public SingleReplacementSet AllReplacements => _allReplacements;

    public IEnumerable<string> RootWords => _entriesByRoot.Keys;

    public bool HasEntries => _entriesByRoot.HasItems;

    public bool IsEmpty => _entriesByRoot.IsEmpty;

    public int RootCount => _entriesByRoot.Count;

    public WordEntryDetail[] this[string rootWord] => TryGetEntryDetailsByRootWord(rootWord, out var details) ? details : [];

    public bool ContainsEntriesForRootWord(string rootWord) => rootWord is not null && _entriesByRoot.ContainsKey(rootWord);

    public bool ContainsEntriesForRootWord(ReadOnlySpan<char> rootWord) => _entriesByRoot.ContainsKey(rootWord);

    public bool Check(string word) => Check(word, options: null, CancellationToken.None);

    public bool Check(ReadOnlySpan<char> word) => Check(word, options: null, CancellationToken.None);

    public bool Check(string word, QueryOptions? options) => Check(word, options, CancellationToken.None);

    public bool Check(ReadOnlySpan<char> word, QueryOptions? options) => Check(word, options, CancellationToken.None);

    public bool Check(string word, CancellationToken cancellationToken) => Check(word, options: null, cancellationToken);

    public bool Check(ReadOnlySpan<char> word, CancellationToken cancellationToken) => Check(word, options: null, cancellationToken);

    public bool Check(string word, QueryOptions? options, CancellationToken cancellationToken) => new QueryCheck(this, options, cancellationToken).Check(word);

    public bool Check(ReadOnlySpan<char> word, QueryOptions? options, CancellationToken cancellationToken) => new QueryCheck(this, options, cancellationToken).Check(word);

    public SpellCheckResult CheckDetails(string word) => CheckDetails(word, options: null, CancellationToken.None);

    public SpellCheckResult CheckDetails(ReadOnlySpan<char> word) => CheckDetails(word, options: null, CancellationToken.None);

    public SpellCheckResult CheckDetails(string word, QueryOptions? options) => CheckDetails(word, options, CancellationToken.None);

    public SpellCheckResult CheckDetails(ReadOnlySpan<char> word, QueryOptions? options) => CheckDetails(word, options, CancellationToken.None);

    public SpellCheckResult CheckDetails(string word, CancellationToken cancellationToken) => CheckDetails(word, options: null, cancellationToken);

    public SpellCheckResult CheckDetails(ReadOnlySpan<char> word, CancellationToken cancellationToken) => CheckDetails(word, options: null, cancellationToken);

    public SpellCheckResult CheckDetails(string word, QueryOptions? options, CancellationToken cancellationToken)
    {
        var result = new QueryCheck(this, options, cancellationToken).CheckDetails(word);
        ApplyRootOutputConversions(ref result);
        return result;
    }

    public SpellCheckResult CheckDetails(ReadOnlySpan<char> word, QueryOptions? options, CancellationToken cancellationToken)
    {
        var result = new QueryCheck(this, options, cancellationToken).CheckDetails(word);
        ApplyRootOutputConversions(ref result);
        return result;
    }

    public IEnumerable<string> Suggest(string word) => Suggest(word, options: null, CancellationToken.None);

    public IEnumerable<string> Suggest(ReadOnlySpan<char> word) => Suggest(word, options: null, CancellationToken.None);

    public IEnumerable<string> Suggest(string word, QueryOptions? options) => Suggest(word, options, CancellationToken.None);

    public IEnumerable<string> Suggest(ReadOnlySpan<char> word, QueryOptions? options) => Suggest(word, options, CancellationToken.None);

    public IEnumerable<string> Suggest(string word, CancellationToken cancellationToken) => Suggest(word, options: null, cancellationToken);

    public IEnumerable<string> Suggest(ReadOnlySpan<char> word, CancellationToken cancellationToken) => Suggest(word, options: null, cancellationToken);

    public IEnumerable<string> Suggest(string word, QueryOptions? options, CancellationToken cancellationToken) => new QuerySuggest(this, options, cancellationToken).Suggest(word);

    public IEnumerable<string> Suggest(ReadOnlySpan<char> word, QueryOptions? options, CancellationToken cancellationToken) => new QuerySuggest(this, options, cancellationToken).Suggest(word);

    /// <summary>
    /// Adds a root word to this in-memory dictionary.
    /// </summary>
    /// <param name="word">The root word to add.</param>
    /// <returns><c>true</c> when a root is added, <c>false</c> otherwise.</returns>
    /// <remarks>
    /// Changes made to this dictionary instance will not be saved.
    /// </remarks>
    public bool Add(string word)
    {
        return Add(word, FlagSet.Empty, MorphSet.Empty, WordEntryOptions.None);
    }

    /// <summary>
    /// Adds a root word to this in-memory dictionary.
    /// </summary>
    /// <param name="word">The root word to add.</param>
    /// <param name="flags">The flags associated with the root <paramref name="word"/> detail entry.</param>
    /// <param name="morphs">The morphs associated with the root <paramref name="word"/> detail entry.</param>
    /// <param name="options">The options associated with the root <paramref name="word"/> detail entry.</param>
    /// <returns><c>true</c> when a root is added, <c>false</c> otherwise.</returns>
    /// <remarks>
    /// Changes made to this dictionary instance will not be saved.
    /// </remarks>
    public bool Add(string word, FlagSet flags, IEnumerable<string> morphs, WordEntryOptions options)
    {
        return Add(word, flags, MorphSet.Create(morphs), options);
    }

    /// <summary>
    /// Adds a root word to this in-memory dictionary.
    /// </summary>
    /// <param name="word">The root word to add.</param>
    /// <param name="flags">The flags associated with the root <paramref name="word"/> detail entry.</param>
    /// <param name="morphs">The morphs associated with the root <paramref name="word"/> detail entry.</param>
    /// <param name="options">The options associated with the root <paramref name="word"/> detail entry.</param>
    /// <returns><c>true</c> when a root is added, <c>false</c> otherwise.</returns>
    /// <remarks>
    /// Changes made to this dictionary instance will not be saved.
    /// </remarks>
    public bool Add(string word, FlagSet flags, MorphSet morphs, WordEntryOptions options)
    {
        return Add(word, new WordEntryDetail(flags, morphs, options));
    }

    /// <summary>
    /// Adds a root word to this in-memory dictionary.
    /// </summary>
    /// <param name="word">The root word to add details for.</param>
    /// <param name="detail">The details to associate with the root <paramref name="word"/>.</param>
    /// <returns><c>true</c> when a root is added, <c>false</c> otherwise.</returns>
    /// <remarks>
    /// Changes made to this dictionary instance will not be saved.
    /// </remarks>
    public bool Add(string word, WordEntryDetail detail)
    {
        return Add(_entriesByRoot, Affix, word, detail);
    }

    private static bool Add(TextDictionary<WordEntryDetail[]> entries, AffixConfig affix, string word, WordEntryDetail detail)
    {
        if (affix.IgnoredChars.HasItems)
        {
            word = affix.IgnoredChars.RemoveChars(word);
        }

        if (affix.ComplexPrefixes)
        {
            word = word.GetReversed();
        }

        ref var details = ref entries.GetOrAddValueRef(word)!;
        if (details is null)
        {
            details = [detail];
        }
        else
        {
            if (details.Contains(detail))
            {
                return false;
            }

            Array.Resize(ref details, details.Length + 1);
            details[details.Length - 1] = detail;
        }

        return true;
    }

    /// <summary>
    /// Removes all detail entries for the given root <paramref name="word"/>.
    /// </summary>
    /// <param name="word">The root to delete all entries for.</param>
    /// <returns>The count of entries removed.</returns>
    public int Remove(string word)
    {
        return Remove(_entriesByRoot, Affix, word);
    }

    private static int Remove(TextDictionary<WordEntryDetail[]> entries, AffixConfig affix, string word)
    {
        if (affix.IgnoredChars.HasItems)
        {
            word = affix.IgnoredChars.RemoveChars(word);
        }

        if (affix.ComplexPrefixes)
        {
            word = word.GetReversed();
        }

        if (entries.TryGetValue(word, out var details))
        {
            entries.Remove(word);

            return details.Length;
        }

        return 0;
    }

    /// <summary>
    /// Removes a specific detail entry for the given root <paramref name="word"/> and detail arguments.
    /// </summary>
    /// <param name="word">The root word to delete a specific entry for.</param>
    /// <param name="flags">The flags to match on an entry.</param>
    /// <param name="morphs">The morphs to match on an entry.</param>
    /// <param name="options">The options to match on an entry.</param>
    /// <returns><c>true</c> when an entry is remove, otherwise <c>false</c>.</returns>
    public bool Remove(string word, FlagSet flags, MorphSet morphs, WordEntryOptions options)
    {
        return Remove(word, new WordEntryDetail(flags, morphs, options));
    }

    /// <summary>
    /// Removes a specific <paramref name="detail"/> entry for the given root <paramref name="word"/>.
    /// </summary>
    /// <param name="word">The root word to delete a specific entry for.</param>
    /// <param name="detail">The detail to delete for a specific root.</param>
    /// <returns><c>true</c> when an entry is remove, otherwise <c>false</c>.</returns>
    public bool Remove(string word, WordEntryDetail detail)
    {
        return Remove(_entriesByRoot, Affix, word, detail);
    }

    private static bool Remove(TextDictionary<WordEntryDetail[]> entries, AffixConfig affix, string word, WordEntryDetail detail)
    {
        if (affix.IgnoredChars.HasItems)
        {
            word = affix.IgnoredChars.RemoveChars(word);
        }

        if (affix.ComplexPrefixes)
        {
            word = word.GetReversed();
        }

        if (entries.TryGetValue(word, out var details))
        {
            var index = details.IndexOf(detail);
            if (index >= 0)
            {
                if (details.Length == 1)
                {
                    entries.Remove(word);
                }
                else
                {
                    var newDetails = new WordEntryDetail[details.Length - 1];
                    Array.Copy(details, 0, newDetails, 0, index);
                    Array.Copy(details, index + 1, newDetails, index, newDetails.Length - index);
                    entries[word] = newDetails;
                }

                return true;
            }
        }

        return false;
    }

    private void ApplyRootOutputConversions(ref SpellCheckResult result)
    {
        // output conversion
        if (result.Correct && _affix.OutputConversions.TryConvert(result.Root, out var converted) && !string.Equals(result.Root, converted, StringComparison.Ordinal))
        {
            result = SpellCheckResult.Success(converted, result.Info);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetEntryDetailsByRootWord(
        string rootWord,
#if !NO_EXPOSED_NULLANNOTATIONS
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)]
#endif
        out WordEntryDetail[] details)
    {
        return _entriesByRoot.TryGetValue(rootWord, out details);
    }

    private bool TryGetFirstEntryDetailByRootWord(ReadOnlySpan<char> rootWord, out WordEntryDetail entryDetail)
    {
        if (_entriesByRoot.TryGetValue(rootWord, out var details))
        {
#if DEBUG
            if (details.Length == 0) ExceptionEx.ThrowInvalidOperation();
#endif

            entryDetail = details[0];
            return true;
        }

        entryDetail = default;
        return false;
    }

    private bool TryFindFirstEntryDetailByRootWord(string rootWord, out WordEntryDetail entryDetail)
    {
        if (_entriesByRoot.TryGetValue(rootWord, out var details))
        {
#if DEBUG
            if (details.Length == 0) ExceptionEx.ThrowInvalidOperation();
#endif

            entryDetail = details[0];
            return true;
        }

        entryDetail = default;
        return false;
    }

    private NGramAllowedEntriesEnumerator GetNGramAllowedDetailsByKeyLength(int minKeyLength, int maxKeyLength) => new(this, minKeyLength: minKeyLength, maxKeyLength: maxKeyLength);

    private struct NGramAllowedEntriesEnumerator
    {
        public NGramAllowedEntriesEnumerator(WordList wordList, int minKeyLength, int maxKeyLength)
        {
            _nGramRestrictedFlags = wordList._nGramRestrictedFlags;
            _coreEnumerator = new(wordList._entriesByRoot, minKeyLength, maxKeyLength);
            _current = default;
        }

        private readonly FlagSet _nGramRestrictedFlags;
        private TextDictionary<WordEntryDetail[]>.KeyLengthEnumerator _coreEnumerator;
        private KeyValuePair<string, WordEntryDetail[]> _current;

        public readonly KeyValuePair<string, WordEntryDetail[]> Current => _current;

        public readonly NGramAllowedEntriesEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_nGramRestrictedFlags.HasItems)
            {
                return MoveNextWithRestrictedDetails();
            }

            if (_coreEnumerator.MoveNext())
            {
                _current = _coreEnumerator.Current;
                return true;
            }

            _current = default;
            return false;
        }

        private bool MoveNextWithRestrictedDetails()
        {
            while (_coreEnumerator.MoveNext())
            {
                _current = _coreEnumerator.Current;
                var details = _current.Value;

                var leftRestrictCount = 0;
                for (; leftRestrictCount < details.Length && details[leftRestrictCount].ContainsAnyFlags(_nGramRestrictedFlags); leftRestrictCount++) ;

                if (leftRestrictCount == details.Length)
                {
                    continue; // all are restricted so try the next one
                }

                var index = leftRestrictCount + 1;
                for (; index < details.Length && details[index].DoesNotContainAnyFlags(_nGramRestrictedFlags); index++) ;

                if (leftRestrictCount > 0 || index < details.Length)
                {
                    _current = new(_current.Key, FilterRestrictedDetails(details, leftRestrictCount, index));
                }

                return true;
            }

            _current = default;
            return false;
        }

        private readonly WordEntryDetail[] FilterRestrictedDetails(WordEntryDetail[] source, int leftRestrictCount, int index)
        {
            var builder = ArrayBuilder<WordEntryDetail>.Pool.Get(source.Length - leftRestrictCount);
            builder.AddRange(source.AsSpan(leftRestrictCount, index - leftRestrictCount));

            index++; // whatever is at the index isn't permitted, so skip it

            for (; index < source.Length; index++)
            {
                if (source[index].DoesNotContainAnyFlags(_nGramRestrictedFlags))
                {
                    builder.Add(source[index]);
                }
            }

            return ArrayBuilder<WordEntryDetail>.Pool.ExtractAndReturn(builder);
        }
    }
}

