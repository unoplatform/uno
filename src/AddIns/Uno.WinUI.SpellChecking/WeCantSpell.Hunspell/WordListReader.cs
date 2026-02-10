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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WeCantSpell.Hunspell;

public sealed class WordListReader
{
    public static Task<WordList> ReadFileAsync(string dictionaryFilePath, CancellationToken cancellationToken = default)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(dictionaryFilePath);
#else
        ExceptionEx.ThrowIfArgumentNull(dictionaryFilePath, nameof(dictionaryFilePath));
#endif

        var affixFilePath = FindAffixFilePath(dictionaryFilePath);
        return ReadFileAsync(dictionaryFilePath, affixFilePath, cancellationToken);
    }

    public static async Task<WordList> ReadFileAsync(string dictionaryFilePath, string affixFilePath, CancellationToken cancellationToken = default)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(dictionaryFilePath);
        ArgumentNullException.ThrowIfNull(affixFilePath);
#else
        ExceptionEx.ThrowIfArgumentNull(dictionaryFilePath, nameof(dictionaryFilePath));
        ExceptionEx.ThrowIfArgumentNull(affixFilePath, nameof(affixFilePath));
#endif

        var affixReader = new AffixReader(builder: null);
        var affix = await affixReader.ReadFileLinesAsync(affixFilePath, cancellationToken).ConfigureAwait(false);
        var wordListReader = new WordListReader(builder: null, affix, affixReader);
        return await wordListReader.ReadFileLinesAsync(dictionaryFilePath, cancellationToken);
    }

    public static Task<WordList> ReadFileAsync(string dictionaryFilePath, AffixConfig affix, CancellationToken cancellationToken = default)
    {
        return ReadFileAsync(dictionaryFilePath, affix, builder: null, cancellationToken);
    }

    public static async Task<WordList> ReadFileAsync(string dictionaryFilePath, AffixConfig affix, WordList.Builder? builder, CancellationToken cancellationToken = default)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(dictionaryFilePath);
        ArgumentNullException.ThrowIfNull(affix);
#else
        ExceptionEx.ThrowIfArgumentNull(dictionaryFilePath, nameof(dictionaryFilePath));
        ExceptionEx.ThrowIfArgumentNull(affix, nameof(affix));
#endif

        using var stream = StreamEx.OpenAsyncReadFileStream(dictionaryFilePath);
        return await ReadAsync(stream, affix, builder, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<WordList> ReadAsync(Stream dictionaryStream, Stream affixStream, CancellationToken cancellationToken = default)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(dictionaryStream);
        ArgumentNullException.ThrowIfNull(affixStream);
#else
        ExceptionEx.ThrowIfArgumentNull(dictionaryStream, nameof(dictionaryStream));
        ExceptionEx.ThrowIfArgumentNull(affixStream, nameof(affixStream));
#endif

        var affix = await AffixReader.ReadAsync(affixStream, cancellationToken).ConfigureAwait(false);
        return await ReadAsync(dictionaryStream, affix, cancellationToken);
    }

    public static Task<WordList> ReadAsync(Stream dictionaryStream, AffixConfig affix, CancellationToken cancellationToken = default)
    {
        return ReadAsync(dictionaryStream, affix, builder: null, cancellationToken);
    }

    public static Task<WordList> ReadAsync(Stream dictionaryStream, AffixConfig affix, WordList.Builder? builder, CancellationToken cancellationToken = default)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(dictionaryStream);
        ArgumentNullException.ThrowIfNull(affix);
#else
        ExceptionEx.ThrowIfArgumentNull(dictionaryStream, nameof(dictionaryStream));
        ExceptionEx.ThrowIfArgumentNull(affix, nameof(affix));
#endif

        return new WordListReader(builder, affix).ReadLinesAsync(dictionaryStream, cancellationToken);
    }

    public static WordList ReadFile(string dictionaryFilePath)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(dictionaryFilePath);
#else
        ExceptionEx.ThrowIfArgumentNull(dictionaryFilePath, nameof(dictionaryFilePath));
#endif

        var affixFilePath = FindAffixFilePath(dictionaryFilePath);
        return ReadFile(dictionaryFilePath, AffixReader.ReadFile(affixFilePath));
    }

    public static WordList ReadFile(string dictionaryFilePath, string affixFilePath)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(dictionaryFilePath);
        ArgumentNullException.ThrowIfNull(affixFilePath);
#else
        ExceptionEx.ThrowIfArgumentNull(dictionaryFilePath, nameof(dictionaryFilePath));
        ExceptionEx.ThrowIfArgumentNull(affixFilePath, nameof(affixFilePath));
#endif
        var affixReader = new AffixReader(builder: null);
        var affix = affixReader.ReadFileLines(affixFilePath);
        var wordListReader = new WordListReader(builder: null, affix, affixReader);
        return wordListReader.ReadFileLines(dictionaryFilePath);
    }

    public static WordList ReadFile(string dictionaryFilePath, AffixConfig affix)
    {
        return ReadFile(dictionaryFilePath, affix, builder: null);
    }

    public static WordList ReadFile(string dictionaryFilePath, AffixConfig affix, WordList.Builder? builder)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(dictionaryFilePath);
        ArgumentNullException.ThrowIfNull(affix);
#else
        ExceptionEx.ThrowIfArgumentNull(dictionaryFilePath, nameof(dictionaryFilePath));
        ExceptionEx.ThrowIfArgumentNull(affix, nameof(affix));
#endif

        using var stream = StreamEx.OpenReadFileStream(dictionaryFilePath);
        return Read(stream, affix, builder);
    }

    public static WordList Read(Stream dictionaryStream, Stream affixStream)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(dictionaryStream);
        ArgumentNullException.ThrowIfNull(affixStream);
#else
        ExceptionEx.ThrowIfArgumentNull(dictionaryStream, nameof(dictionaryStream));
        ExceptionEx.ThrowIfArgumentNull(affixStream, nameof(affixStream));
#endif

        var affix = AffixReader.Read(affixStream);
        return Read(dictionaryStream, affix);
    }

    public static WordList Read(Stream dictionaryStream, AffixConfig affix)
    {
        return Read(dictionaryStream, affix, builder: null);
    }

    public static WordList Read(Stream dictionaryStream, AffixConfig affix, WordList.Builder? builder)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(dictionaryStream);
        ArgumentNullException.ThrowIfNull(affix);
#else
        ExceptionEx.ThrowIfArgumentNull(dictionaryStream, nameof(dictionaryStream));
        ExceptionEx.ThrowIfArgumentNull(affix, nameof(affix));
#endif

        return new WordListReader(builder, affix).ReadLines(dictionaryStream);
    }

    private static string FindAffixFilePath(string dictionaryFilePath)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(dictionaryFilePath);
#else
        ExceptionEx.ThrowIfArgumentNull(dictionaryFilePath, nameof(dictionaryFilePath));
#endif

        var directoryName = Path.GetDirectoryName(dictionaryFilePath);
        if (!string.IsNullOrEmpty(directoryName))
        {
            var locatedAffFile = Directory.GetFiles(directoryName, Path.GetFileNameWithoutExtension(dictionaryFilePath) + ".*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(affFilePath => ".AFF".Equals(Path.GetExtension(affFilePath), StringComparison.OrdinalIgnoreCase));

            if (locatedAffFile is not null)
            {
                return locatedAffFile;
            }
        }

        return Path.ChangeExtension(dictionaryFilePath, "aff");
    }

    private WordListReader(WordList.Builder? builder, AffixConfig affix)
        : this(builder, affix, new(affix), new(affix))
    {
    }

    private WordListReader(
        WordList.Builder? builder,
        AffixConfig affix,
        AffixReader affixReader)
        : this(builder, affix, affixReader.FlagParser, affixReader.MorphParser)
    {
    }

    private WordListReader(
        WordList.Builder? builder,
        AffixConfig affix,
        FlagParser flagParser,
        MorphSetParser morphSetParser)
    {
        if (builder is null)
        {
            _ownsBuilder = true;
            builder = new WordList.Builder(affix);
        }

        Builder = builder;
        Affix = affix;
        _flagParser = flagParser;
        _morphParser = morphSetParser;

        _parseWordLineFlags = Affix.IsAliasF
            ? GetAliasedFlagSet
            : _flagParser.ParseFlagSet;

        _parseWordLineMorphs = Affix.IsAliasM
            ? GetAliasedMorphSet
            : _morphParser.ParseMorphSet;

        _parseWordLineWord = Affix.ComplexPrefixes || Affix.IgnoredChars.HasItems
            ? ParseWordGeneral
            : ParseWordSimple;
    }

    private readonly ParseFlagSetDelegate _parseWordLineFlags;
    private readonly ParseMorphSetDelegate _parseWordLineMorphs;
    private readonly ParseWordDelegate _parseWordLineWord;
    private readonly FlagParser _flagParser;
    private readonly MorphSetParser _morphParser;
    private readonly bool _ownsBuilder;
    private bool _hasInitialized;

    private WordList.Builder Builder { get; }

    private AffixConfig Affix { get; }

    private TextInfo TextInfo => Affix.Culture.TextInfo;

    public async Task<WordList> ReadFileLinesAsync(string filePath, CancellationToken cancellationToken)
    {
        using var stream = StreamEx.OpenAsyncReadFileStream(filePath);
        return await ReadLinesAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WordList> ReadLinesAsync(Stream dictionaryStream, CancellationToken cancellationToken = default)
    {
        using (var lineReader = new LineReader(dictionaryStream, Affix.Encoding))
        {
            while (await lineReader.ReadNextAsync(cancellationToken))
            {
                ParseLine(lineReader.CurrentSpan);
            }
        }

        return ExtractOrBuild();
    }

    public WordList ReadFileLines(string filePath)
    {
        using var stream = StreamEx.OpenAsyncReadFileStream(filePath);
        return ReadLines(stream);
    }

    public WordList ReadLines(Stream dictionaryStream)
    {
        using (var lineReader = new LineReader(dictionaryStream, Affix.Encoding))
        {
            while (lineReader.ReadNext())
            {
                ParseLine(lineReader.CurrentSpan);
            }
        }

        return Builder.Extract();
    }

    private void ParseLine(ReadOnlySpan<char> line)
    {
        if (line.Length > 0)
        {
            if (!_hasInitialized)
            {
                if (AttemptToProcessInitializationLine(line))
                {
                    return;
                }

                Builder.InitializeEntriesByRoot(-1);
            }

            int i;

            // read past any leading tabs or spaces
            for (i = 0; i < line.Length && line[i].IsTabOrSpace(); ++i) ;

            if (i < line.Length)
            {
                if (i > 0)
                {
                    line = line.Slice(i);
                }

                // Try to locate the end of the word part of a line, taking morphs into consideration
                i = findIndexOfFirstMorphByColonCharAndSpacingHints(line);
                if (i <= 0)
                {
                    i = line.IndexOf('\t'); // For some reason, this does not include space
                    if (i < 0)
                    {
                        i = line.Length;
                    }
                }

                for (; i > 0 && line[i - 1].IsTabOrSpace(); --i) ;

                if (i > 0)
                {
                    var wordPart = line.Slice(0, i);
                    FlagSet flags;

                    var flagsDelimiterPosition = indexOfFlagsDelimiter(wordPart);
                    if (flagsDelimiterPosition < wordPart.Length)
                    {
                        flags = _parseWordLineFlags(wordPart.Slice(flagsDelimiterPosition + 1));
                        wordPart = wordPart.Slice(0, flagsDelimiterPosition);
                    }
                    else
                    {
                        flags = FlagSet.Empty;
                    }

                    AddWord(
                        _parseWordLineWord(wordPart),
                        flags,
                        (i + 1) < line.Length ? _parseWordLineMorphs(line.Slice(i + 1)) : MorphSet.Empty);
                }
            }
        }

        static int findIndexOfFirstMorphByColonCharAndSpacingHints(ReadOnlySpan<char> text)
        {
            var index = 0;
            while ((index = text.IndexOf(':', index)) >= 0)
            {
                var checkLocation = index - 3;
                if (checkLocation >= 0 && text[checkLocation].IsTabOrSpace())
                {
                    return checkLocation;
                }

                index++;
            }

            return -1;
        }

        static int indexOfFlagsDelimiter(ReadOnlySpan<char> text)
        {
            // NOTE: the first character is ignored as a single slash should be treated as a word
            int i;
            for (i = 1; i < text.Length; i++)
            {
                if (text[i] == '/' && text[i - 1] != '\\')
                {
                    break;
                }
            }

            return i;
        }
    }

    private string ParseWordGeneral(ReadOnlySpan<char> text)
    {
        var word = text.ReplaceIntoString(@"\/", @"/");

        if (Affix.IgnoredChars.HasItems)
        {
            word = Affix.IgnoredChars.RemoveChars(word);
        }

        if (Affix.ComplexPrefixes)
        {
            word = word.GetReversed();
        }

        return word;
    }

    private string ParseWordSimple(ReadOnlySpan<char> text)
    {
        return text.ReplaceIntoString(@"\/", @"/");
    }

    private FlagSet GetAliasedFlagSet(ReadOnlySpan<char> flagNumberText)
    {
        if (Affix.AliasF.TryGetByNumber(flagNumberText, out var aliasedFlags))
        {
            return aliasedFlags;
        }
        else
        {
            // TODO: warn
            return FlagSet.Empty;
        }
    }

    MorphSet GetAliasedMorphSet(ReadOnlySpan<char> morphsText)
    {
        var morphBuilder = ArrayBuilder<string>.Pool.Get();
        foreach (var originalValue in morphsText.SplitOnTabOrSpace())
        {
            if (Affix.AliasM.TryGetByNumber(originalValue, out var aliasedMorph))
            {
                morphBuilder.AddRange(aliasedMorph.RawArray);
            }
            else
            {
                morphBuilder.Add(originalValue.ToString());
            }
        }

        return MorphSet.CreateUsingArray(ArrayBuilder<string>.Pool.ExtractAndReturn(morphBuilder));
    }

    private bool AttemptToProcessInitializationLine(ReadOnlySpan<char> text)
    {
        int i;
        _hasInitialized = true;

        // read through any leading spaces
        for (i = 0; i < text.Length && char.IsWhiteSpace(text[i]); i++) ;

        if (i > 0)
        {
            text = text.Slice(i);
        }

        // find the possible value
        for (i = 0; i < text.Length && !char.IsWhiteSpace(text[i]); i++) ;

        if (i < text.Length)
        {
            text = text.Slice(0, i);
        }

        if (text.Length > 0 && IntEx.TryParseInvariant(text, out var expectedSize))
        {
            Builder.InitializeEntriesByRoot(expectedSize);

            return true;
        }

        return false;
    }

    private void AddWord(string word, FlagSet flags, MorphSet morphs)
    {
        var capType = StringEx.GetCapitalizationType(word, TextInfo);
        AddWord(word, flags, morphs, onlyUpperCase: false, capType);
        AddWordCapitalized(word, flags, morphs, capType);
    }

    private void AddWord_HandleMorph(MorphSet morphs, string word, CapitalizationType capType, ref WordEntryOptions options)
    {
        var morphPhonEnumerator = new AddWordMorphFilterEnumerator(morphs);
        if (!morphPhonEnumerator.MoveNext())
        {
            return;
        }

        options |= WordEntryOptions.Phon;

        // store ph: fields (pronounciation, misspellings, old orthography etc.)
        // of a morphological description in reptable to use in REP replacements.

        do
        {
            if (morphPhonEnumerator.Current.Length <= MorphologicalTags.Phon.Length)
            {
                continue;
            }

            var ph = morphPhonEnumerator.Current.AsSpan(MorphologicalTags.Phon.Length);

            ReadOnlySpan<char> wordpart;
            // dictionary based REP replacement, separated by "->"
            // for example "pretty ph:prity ph:priti->pretti" to handle
            // both prity -> pretty and pritier -> prettiest suggestions.
            var strippatt = ph.IndexOf("->");
            if (strippatt > 0 && strippatt < (ph.Length - 2))
            {
                wordpart = ph.Slice(strippatt + 2);
                ph = ph.Slice(0, strippatt);
            }
            else
            {
                wordpart = word.AsSpan();
            }

            // when the ph: field ends with the character *,
            // strip last character of the pattern and the replacement
            // to match in REP suggestions also at character changes,
            // for example, "pretty ph:prity*" results "prit->prett"
            // REP replacement instead of "prity->pretty", to get
            // prity->pretty and pritiest->prettiest suggestions.
            if (ph.EndsWith('*'))
            {
                if (ph.Length > 2 && wordpart.Length > 1)
                {
                    ph = ph.Slice(0, ph.Length - 2);
                    wordpart = wordpart.Slice(0, word.Length - 1);
                }
            }

            var phString = ph.ToString();
            var wordpartString = wordpart.ToString();

            // capitalize lowercase pattern for capitalized words to support
            // good suggestions also for capitalized misspellings, eg.
            // Wednesday ph:wendsay
            // results wendsay -> Wednesday and Wendsay -> Wednesday, too.
            if (capType == CapitalizationType.Init)
            {
                var phCapitalized = StringEx.MakeInitCap(phString, Affix.Culture.TextInfo);
                if (phCapitalized.Length > 0)
                {
                    // add also lowercase word in the case of German or
                    // Hungarian to support lowercase suggestions lowercased by
                    // compound word generation or derivational suffixes
                    // (for example by adjectival suffix "-i" of geographical
                    // names in Hungarian:
                    // Massachusetts ph:messzecsuzec
                    // messzecsuzeci -> massachusettsi (adjective)
                    // For lowercasing by conditional PFX rules, see
                    // tests/germancompounding test example or the
                    // Hungarian dictionary.)
                    if (Affix.IsGerman || Affix.IsHungarian)
                    {
                        Builder._phoneticReplacements.Add(new SingleReplacement(
                            phString,
                            StringEx.MakeAllSmall(wordpartString, Affix.Culture.TextInfo),
                            ReplacementValueType.Med));
                    }

                    Builder._phoneticReplacements.Add(new SingleReplacement(phCapitalized, wordpartString, ReplacementValueType.Med));
                }
            }

            Builder._phoneticReplacements.Add(new SingleReplacement(phString, wordpartString, ReplacementValueType.Med));
        }
        while (morphPhonEnumerator.MoveNext());
    }

    private WordList ExtractOrBuild()
    {
        return _ownsBuilder ? Builder.Extract() : Builder.Build();
    }

    private void AddWord(string word, FlagSet flags, MorphSet morphs, bool onlyUpperCase, CapitalizationType capType)
    {
        // store the description string or its pointer
        var options = capType == CapitalizationType.Init ? WordEntryOptions.InitCap : WordEntryOptions.None;

        if (morphs.HasItems)
        {
            if (Affix.IsAliasM)
            {
                options |= WordEntryOptions.AliasM;
            }

            AddWord_HandleMorph(morphs, word, capType, ref options);
        }

        ref var details = ref Builder._entriesByRoot.GetOrAddValueRef(word);

        if (details is not null)
        {
#if DEBUG
            if (details.Length <= 0) ExceptionEx.ThrowInvalidOperation();
#endif

            if (onlyUpperCase)
            {
                return;
            }

            for (var i = 0; i < details.Length; i++)
            {
                ref var entry = ref details[i];
                if (entry.ContainsFlag(SpecialFlags.OnlyUpcaseFlag))
                {
                    entry = new(flags, entry.Morphs, entry.Options);
                    return;
                }
            }

            Array.Resize(ref details, details.Length + 1);
            details[details.Length - 1] = new(flags, morphs, options);
        }
        else
        {
            details = [new(flags, morphs, options)];
        }
    }

    private void AddWordCapitalized(string word, FlagSet flags, MorphSet morphs, CapitalizationType capType)
    {
        // add inner capitalized forms to handle the following allcap forms:
        // Mixed caps: OpenOffice.org -> OPENOFFICE.ORG
        // Allcaps with suffixes: CIA's -> CIA'S

        if (
            (
                capType is (CapitalizationType.Huh or CapitalizationType.HuhInit)
                ||
                (capType == CapitalizationType.All && flags.HasItems)
            )
            &&
            flags.DoesNotContain(Affix.ForbiddenWord)
        )
        {
            AddWord(
                StringEx.MakeTitleCase(word, Affix.Culture),
                flags.Union(SpecialFlags.OnlyUpcaseFlag),
                morphs,
                onlyUpperCase: true,
                CapitalizationType.Init);
        }
    }

    private struct AddWordMorphFilterEnumerator
    {
        public AddWordMorphFilterEnumerator(MorphSet morphs) : this(morphs.RawArray)
        {
        }

        public AddWordMorphFilterEnumerator(string[] morphs)
        {
            _morphs = morphs;
            _index = 0;
            Current = default!;
        }

        private readonly string[] _morphs;
        public string Current;
        private int _index;

        public bool MoveNext()
        {
            while (_index < _morphs.Length)
            {
                var morph = _morphs[_index++];
                if (morph.StartsWith(MorphologicalTags.Phon))
                {
                    Current = morph;
                    return true;
                }
            }

            Current = default!;
            return false;
        }
    }
}

