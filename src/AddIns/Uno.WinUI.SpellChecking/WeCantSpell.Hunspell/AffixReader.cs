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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeCantSpell.Hunspell;

public sealed partial class AffixReader
{
    public static readonly Encoding DefaultEncoding = EncodingEx.GetEncodingByName("ISO8859-1") ?? Encoding.UTF8;

    private static readonly CharacterSet DefaultCompoundVowels = CharacterSet.Create("AEIOUaeiou");
    private static readonly TextDictionary<AffixConfigOptions> BitFlagCommandMap;
    private static readonly TextDictionary<AffixReaderCommandKind> CommandMap;

    static AffixReader()
    {
        BitFlagCommandMap = TextDictionary<AffixConfigOptions>.MapFromPairs(
        [
            new("CHECKCOMPOUNDDUP", AffixConfigOptions.CheckCompoundDup),
            new("CHECKCOMPOUNDREP", AffixConfigOptions.CheckCompoundRep),
            new("CHECKCOMPOUNDTRIPLE", AffixConfigOptions.CheckCompoundTriple),
            new("CHECKCOMPOUNDCASE", AffixConfigOptions.CheckCompoundCase),
            new("CHECKNUM", AffixConfigOptions.CheckNum),
            new("CHECKSHARPS", AffixConfigOptions.CheckSharps),
            new("COMPLEXPREFIXES", AffixConfigOptions.ComplexPrefixes),
            new("COMPOUNDMORESUFFIXES", AffixConfigOptions.CompoundMoreSuffixes),
            new("FULLSTRIP", AffixConfigOptions.FullStrip),
            new("FORBIDWARN", AffixConfigOptions.ForbidWarn),
            new("NOSPLITSUGS", AffixConfigOptions.NoSplitSuggestions),
            new("ONLYMAXDIFF", AffixConfigOptions.OnlyMaxDiff),
            new("SIMPLIFIEDTRIPLE", AffixConfigOptions.SimplifiedTriple),
            new("SUGSWITHDOTS", AffixConfigOptions.SuggestWithDots),
        ]);

        CommandMap = TextDictionary<AffixReaderCommandKind>.MapFromPairs(
        [
            new("AF", AffixReaderCommandKind.AliasF),
            new("AM", AffixReaderCommandKind.AliasM),
            new("BREAK", AffixReaderCommandKind.Break),
            new("COMPOUNDBEGIN", AffixReaderCommandKind.CompoundBegin),
            new("COMPOUNDEND", AffixReaderCommandKind.CompoundEnd),
            new("COMPOUNDFLAG", AffixReaderCommandKind.CompoundFlag),
            new("COMPOUNDFORBIDFLAG", AffixReaderCommandKind.CompoundForbidFlag),
            new("COMPOUNDMIDDLE", AffixReaderCommandKind.CompoundMiddle),
            new("COMPOUNDMIN", AffixReaderCommandKind.CompoundMin),
            new("COMPOUNDPERMITFLAG", AffixReaderCommandKind.CompoundPermitFlag),
            new("COMPOUNDROOT", AffixReaderCommandKind.CompoundRoot),
            new("COMPOUNDRULE", AffixReaderCommandKind.CompoundRule),
            new("COMPOUNDSYLLABLE", AffixReaderCommandKind.CompoundSyllable),
            new("COMPOUNDWORDMAX", AffixReaderCommandKind.CompoundWordMax),
            new("CIRCUMFIX", AffixReaderCommandKind.Circumfix),
            new("CHECKCOMPOUNDPATTERN", AffixReaderCommandKind.CheckCompoundPattern),
            new("FLAG", AffixReaderCommandKind.Flag),
            new("FORBIDDENWORD", AffixReaderCommandKind.ForbiddenWord),
            new("FORCEUCASE", AffixReaderCommandKind.ForceUpperCase),
            new("ICONV", AffixReaderCommandKind.InputConversions),
            new("IGNORE", AffixReaderCommandKind.Ignore),
            new("KEY", AffixReaderCommandKind.KeyString),
            new("KEEPCASE", AffixReaderCommandKind.KeepCase),
            new("LANG", AffixReaderCommandKind.Language),
            new("LEMMA_PRESENT", AffixReaderCommandKind.LemmaPresent),
            new("MAP", AffixReaderCommandKind.Map),
            new("MAXNGRAMSUGS", AffixReaderCommandKind.MaxNgramSuggestions),
            new("MAXDIFF", AffixReaderCommandKind.MaxDifferency),
            new("MAXCPDSUGS", AffixReaderCommandKind.MaxCompoundSuggestions),
            new("NEEDAFFIX", AffixReaderCommandKind.NeedAffix),
            new("NOSUGGEST", AffixReaderCommandKind.NoSuggest),
            new("NONGRAMSUGGEST", AffixReaderCommandKind.NoNGramSuggest),
            new("OCONV", AffixReaderCommandKind.OutputConversions),
            new("ONLYINCOMPOUND", AffixReaderCommandKind.OnlyInCompound),
            new("PFX", AffixReaderCommandKind.Prefix),
            new("PSEUDOROOT", AffixReaderCommandKind.NeedAffix),
            new("PHONE", AffixReaderCommandKind.Phone),
            new("REP", AffixReaderCommandKind.Replacement),
            new("SFX", AffixReaderCommandKind.Suffix),
            new("SET", AffixReaderCommandKind.SetEncoding),
            new("SYLLABLENUM", AffixReaderCommandKind.CompoundSyllableNum),
            new("SUBSTANDARD", AffixReaderCommandKind.SubStandard),
            new("TRY", AffixReaderCommandKind.TryString),
            new("VERSION", AffixReaderCommandKind.Version),
            new("WORDCHARS", AffixReaderCommandKind.WordChars),
            new("WARN", AffixReaderCommandKind.Warn),
        ]);
    }

    public static Task<AffixConfig> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return ReadFileAsync(filePath, builder: null, cancellationToken);
    }

    public static Task<AffixConfig> ReadFileAsync(string filePath, AffixConfig.Builder? builder, CancellationToken cancellationToken = default)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(filePath);
#else
        ExceptionEx.ThrowIfArgumentNull(filePath, nameof(filePath));
#endif
        return new AffixReader(builder).ReadFileLinesAsync(filePath, cancellationToken);
    }

    public static Task<AffixConfig> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return ReadAsync(stream, builder: null, cancellationToken);
    }

    public static Task<AffixConfig> ReadAsync(Stream stream, AffixConfig.Builder? builder, CancellationToken cancellationToken = default)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(stream);
#else
        ExceptionEx.ThrowIfArgumentNull(stream, nameof(stream));
#endif

        return new AffixReader(builder).ReadLinesAsync(stream, cancellationToken);
    }

    public static AffixConfig ReadFile(string filePath)
    {
        return ReadFile(filePath, builder: null);
    }

    public static AffixConfig ReadFile(string filePath, AffixConfig.Builder? builder)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(filePath);
#else
        ExceptionEx.ThrowIfArgumentNull(filePath, nameof(filePath));
#endif

        return new AffixReader(builder).ReadFileLines(filePath);
    }

    public static AffixConfig ReadFromString(string contents)
    {
        return ReadFromString(contents, builder: null);
    }

    public static AffixConfig ReadFromString(string contents, AffixConfig.Builder? builder)
    {
        using var textReader = new StringReader(contents);
        return new AffixReader(builder).ReadLines(textReader);
    }

    public static AffixConfig Read(Stream stream)
    {
        return Read(stream, builder: null);
    }

    public static AffixConfig Read(Stream stream, AffixConfig.Builder? builder)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(stream);
#else
        ExceptionEx.ThrowIfArgumentNull(stream, nameof(stream));
#endif

        return new AffixReader(builder).ReadLines(stream);
    }

    public AffixReader(AffixConfig.Builder? builder)
    {
        if (builder is null)
        {
            builder = new();
            _ownsBuilder = true;
        }

        _builder = builder;
        FlagParser = new(_builder.FlagMode, _builder.Encoding);
        MorphParser = new(complexPrefixes: builder.Options.HasFlagEx(AffixConfigOptions.ComplexPrefixes));
    }

    internal AffixReader(AffixConfig.Builder builder, bool ownsBuilder)
    {
        _ownsBuilder = ownsBuilder;
        _builder = builder;
        FlagParser = new(_builder.FlagMode, _builder.Encoding);
        MorphParser = new(complexPrefixes: builder.Options.HasFlagEx(AffixConfigOptions.ComplexPrefixes));
    }

    private readonly AffixConfig.Builder _builder;
    internal FlagParser FlagParser;
    internal MorphSetParser MorphParser;
    private readonly bool _ownsBuilder;
    private EntryListType _initialized = EntryListType.None;

    private Encoding Encoding => _builder.Encoding ?? DefaultEncoding;

    public async Task<AffixConfig> ReadFileLinesAsync(string filePath, CancellationToken cancellationToken)
    {
        using var stream = StreamEx.OpenAsyncReadFileStream(filePath);
        return await ReadLinesAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AffixConfig> ReadLinesAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using (var lineReader = new LineReader(stream, Encoding, allowEncodingChanges: true))
        {
            while (await lineReader.ReadNextAsync(cancellationToken))
            {
                ParseLine(lineReader.CurrentSpan);
            }
        }

        return ExtractOrBuild();
    }

    public AffixConfig ReadFileLines(string filePath)
    {
        using var stream = StreamEx.OpenAsyncReadFileStream(filePath);
        return ReadLines(stream);
    }

    public AffixConfig ReadLines(Stream stream)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(stream);
#else
        ExceptionEx.ThrowIfArgumentNull(stream, nameof(stream));
#endif

        using (var lineReader = new LineReader(stream, Encoding, allowEncodingChanges: true))
        {
            while (lineReader.ReadNext())
            {
                ParseLine(lineReader.CurrentSpan);
            }
        }

        return ExtractOrBuild();
    }

    public AffixConfig ReadLines(TextReader reader)
    {
#if HAS_THROWNULL
        ArgumentNullException.ThrowIfNull(reader);
#else
        ExceptionEx.ThrowIfArgumentNull(reader, nameof(reader));
#endif

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            ParseLine(line);
        }

        return ExtractOrBuild();
    }

    private bool ParseLine(ReadOnlySpan<char> line)
    {
        // read through the initial ^[ \t]*
        var commandStartIndex = 0;
        for (; commandStartIndex < line.Length && line[commandStartIndex].IsTabOrSpace(); commandStartIndex++) ;

        if (commandStartIndex == line.Length || isCommentPrefix(line[commandStartIndex]))
        {
            return true; // empty, whitespace, or comment
        }

        // read through the final [ \t]*$
        var lineEndIndex = line.Length - 1;
        for (; lineEndIndex > commandStartIndex && line[lineEndIndex].IsTabOrSpace(); lineEndIndex--) ;

        // find the end of the command
        var commandEndIndex = commandStartIndex;
        for (; commandEndIndex <= lineEndIndex && !line[commandEndIndex].IsTabOrSpace(); commandEndIndex++) ;

        // first command exists between [lineStartIndex,commandEndIndex)
        var parameterStartIndex = commandEndIndex;
        for (; parameterStartIndex <= lineEndIndex && line[parameterStartIndex].IsTabOrSpace(); parameterStartIndex++) ;

        var command = line.Slice(commandStartIndex, commandEndIndex - commandStartIndex);

        if (CommandMap.TryGetValue(command, out var commandType))
        {
            var parameters = line.Slice(parameterStartIndex, lineEndIndex - parameterStartIndex + 1);
            if (TryHandleParameterizedCommand(commandType, parameters))
            {
                return true;
            }
        }
        else if (BitFlagCommandMap.TryGetValue(command, out var option))
        {
            _builder.EnableOptions(option);

            if (option == AffixConfigOptions.ComplexPrefixes)
            {
                UpdateMorphParserForState();
            }

            return true;
        }

        LogWarning("Failed to parse line: " + line.ToString());
        return false;

        static bool isCommentPrefix(char c) => c is '#' or '/';
    }

    private bool IsInitialized(EntryListType flags) => (_initialized & flags) == flags;

    private void SetInitialized(EntryListType flags)
    {
        _initialized |= flags;
    }

    private AffixConfig ExtractOrBuild()
    {
        if (!IsInitialized(EntryListType.Break) && _builder.BreakPointsBuilder.Count == 0)
        {
            _builder.BreakPointsBuilder.AddRange(["-", "^-", "-$"]);
        }

        return _ownsBuilder ? _builder.Extract() : _builder.Build();
    }

    private bool TryHandleParameterizedCommand(AffixReaderCommandKind command, ReadOnlySpan<char> parameters)
    {
        switch (command)
        {
            case AffixReaderCommandKind.Flag:
                return TrySetFlagMode(parameters);
            case AffixReaderCommandKind.KeyString:
                _builder.KeyString = parameters.ToString();
                return true;
            case AffixReaderCommandKind.TryString:
                _builder.TryString = parameters.ToString();
                return true;
            case AffixReaderCommandKind.SetEncoding:
                if (EncodingEx.GetEncodingByName(parameters) is not { } encoding)
                {
                    LogWarning("Failed to get encoding: " + parameters.ToString());
                    return false;
                }

                _builder.Encoding = encoding;
                FlagParser = FlagParser.WithEncoding(encoding);
                return true;
            case AffixReaderCommandKind.Language:
                _builder.Language = parameters.ToString();
                _builder.Culture = GetCultureFromLanguage(_builder.Language);
                return true;
            case AffixReaderCommandKind.CompoundSyllableNum:
                _builder.CompoundSyllableNum = parameters.ToString();
                return true;
            case AffixReaderCommandKind.WordChars:
                _builder.WordChars = CharacterSet.Create(parameters);
                return true;
            case AffixReaderCommandKind.Ignore:
                _builder.IgnoredChars = CharacterSet.Create(parameters);
                return true;
            case AffixReaderCommandKind.CompoundFlag:
                return FlagParser.TryParseFlag(parameters, out _builder.CompoundFlag);
            case AffixReaderCommandKind.CompoundMiddle:
                return FlagParser.TryParseFlag(parameters, out _builder.CompoundMiddle);
            case AffixReaderCommandKind.CompoundBegin:
                return _builder.Options.HasFlagEx(AffixConfigOptions.ComplexPrefixes)
                    ? FlagParser.TryParseFlag(parameters, out _builder.CompoundEnd)
                    : FlagParser.TryParseFlag(parameters, out _builder.CompoundBegin);
            case AffixReaderCommandKind.CompoundEnd:
                return _builder.Options.HasFlagEx(AffixConfigOptions.ComplexPrefixes)
                    ? FlagParser.TryParseFlag(parameters, out _builder.CompoundBegin)
                    : FlagParser.TryParseFlag(parameters, out _builder.CompoundEnd);
            case AffixReaderCommandKind.CompoundWordMax:
                _builder.CompoundWordMax = IntEx.TryParseInvariant(parameters);
                return _builder.CompoundWordMax.HasValue;
            case AffixReaderCommandKind.CompoundMin:
                _builder.CompoundMin = IntEx.TryParseInvariant(parameters);

                switch (_builder.CompoundMin)
                {
                    case null:
                        LogWarning("Failed to parse CompoundMin: " + parameters.ToString());
                        return false;
                    case < 1:
                        _builder.CompoundMin = 1;
                        break;
                }

                return true;
            case AffixReaderCommandKind.CompoundRoot:
                return FlagParser.TryParseFlag(parameters, out _builder.CompoundRoot);
            case AffixReaderCommandKind.CompoundPermitFlag:
                return FlagParser.TryParseFlag(parameters, out _builder.CompoundPermitFlag);
            case AffixReaderCommandKind.CompoundForbidFlag:
                return FlagParser.TryParseFlag(parameters, out _builder.CompoundForbidFlag);
            case AffixReaderCommandKind.CompoundSyllable:
                return TryParseCompoundSyllable(parameters);
            case AffixReaderCommandKind.NoSuggest:
                return FlagParser.TryParseFlag(parameters, out _builder.NoSuggest);
            case AffixReaderCommandKind.NoNGramSuggest:
                return FlagParser.TryParseFlag(parameters, out _builder.NoNgramSuggest);
            case AffixReaderCommandKind.ForbiddenWord:
                _builder.ForbiddenWord = FlagParser.ParseFlagOrDefault(parameters);
                return _builder.ForbiddenWord.HasValue;
            case AffixReaderCommandKind.LemmaPresent:
                return FlagParser.TryParseFlag(parameters, out _builder.LemmaPresent);
            case AffixReaderCommandKind.Circumfix:
                return FlagParser.TryParseFlag(parameters, out _builder.Circumfix);
            case AffixReaderCommandKind.OnlyInCompound:
                return FlagParser.TryParseFlag(parameters, out _builder.OnlyInCompound);
            case AffixReaderCommandKind.NeedAffix:
                return FlagParser.TryParseFlag(parameters, out _builder.NeedAffix);
            case AffixReaderCommandKind.Replacement:
                return TryParseStandardListItem(EntryListType.Replacements, parameters, _builder.ReplacementsBuilder, TryParseReplacements);
            case AffixReaderCommandKind.InputConversions:
                return TryParseConv(parameters, EntryListType.Iconv, ref _builder.InputConversionsBuilder);
            case AffixReaderCommandKind.OutputConversions:
                return TryParseConv(parameters, EntryListType.Oconv, ref _builder.OutputConversionsBuilder);
            case AffixReaderCommandKind.Phone:
                return TryParseStandardListItem(EntryListType.Phone, parameters, _builder.PhoneBuilder, TryParsePhone);
            case AffixReaderCommandKind.CheckCompoundPattern:
                return TryParseStandardListItem(EntryListType.CompoundPatterns, parameters, _builder.CompoundPatternsBuilder, TryParseCheckCompoundPatternIntoCompoundPatterns);
            case AffixReaderCommandKind.CompoundRule:
                return TryParseStandardListItem(EntryListType.CompoundRules, parameters, _builder.CompoundRulesBuilder, TryParseCompoundRuleIntoList);
            case AffixReaderCommandKind.Map:
                return TryParseStandardListItem(EntryListType.Map, parameters, _builder.RelatedCharacterMapBuilder, TryParseMapEntry);
            case AffixReaderCommandKind.Break:
                return TryParseStandardListItem(EntryListType.Break, parameters, _builder.BreakPointsBuilder, TryParseBreak);
            case AffixReaderCommandKind.Version:
                if (parameters.IsEmpty)
                {
                    return false;
                }

                _builder.Version = parameters.ToString();
                return true;
            case AffixReaderCommandKind.MaxNgramSuggestions:
                _builder.MaxNgramSuggestions = IntEx.TryParseInvariant(parameters);
                return _builder.MaxNgramSuggestions.HasValue;
            case AffixReaderCommandKind.MaxDifferency:
                _builder.MaxDifferency = IntEx.TryParseInvariant(parameters);
                return _builder.MaxDifferency.HasValue;
            case AffixReaderCommandKind.MaxCompoundSuggestions:
                _builder.MaxCompoundSuggestions = IntEx.TryParseInvariant(parameters);
                return _builder.MaxCompoundSuggestions.HasValue;
            case AffixReaderCommandKind.KeepCase:
                return FlagParser.TryParseFlag(parameters, out _builder.KeepCase);
            case AffixReaderCommandKind.ForceUpperCase:
                return FlagParser.TryParseFlag(parameters, out _builder.ForceUpperCase);
            case AffixReaderCommandKind.Warn:
                return FlagParser.TryParseFlag(parameters, out _builder.Warn);
            case AffixReaderCommandKind.SubStandard:
                return FlagParser.TryParseFlag(parameters, out _builder.SubStandard);
            case AffixReaderCommandKind.Prefix:
            case AffixReaderCommandKind.Suffix:
                var parseAsPrefix = AffixReaderCommandKind.Prefix == command;

                if (_builder.Options.HasFlagEx(AffixConfigOptions.ComplexPrefixes))
                {
                    parseAsPrefix = !parseAsPrefix;
                }

                return parseAsPrefix
                    ? TryParseAffixIntoList(parameters, _builder.Prefixes)
                    : TryParseAffixIntoList(parameters, _builder.Suffixes);
            case AffixReaderCommandKind.AliasF:
                return TryParseStandardListItemForAffix(EntryListType.AliasF, parameters, _builder.AliasFBuilder, TryParseAliasF);
            case AffixReaderCommandKind.AliasM:
                return TryParseStandardListItemForAffix(EntryListType.AliasM, parameters, _builder.AliasMBuilder, TryParseAliasM);
            default:
                LogWarning(string.Concat("Unknown parsed command ", command.ToString()));
                return false;
        }
    }

    private bool TryParseStandardListItemForAffix<T>(EntryListType entryListType, ReadOnlySpan<char> parameterText, ArrayBuilder<T> entries, EntryParserForBuilder<T> parse)
    {
        int expectedSize;

        {
            // Some aff files use comments on the AF and AM commands. This seems to work because the
            // Int parser is able to tolerate comments in the original code while this parser is a bit
            // more rigit in it's input expectations. I don't think comments on AF or AM are explicitly
            // supported, but there are affix files with comments, and they still should work here too.
            // It would be more correct to make the int parsers and flag parsers ignore these comments
            // but this should work well enough for now.
            expectedSize = parameterText.IndexOf('#', 1);
            if (expectedSize > 0 && parameterText[expectedSize - 1].IsTabOrSpace())
            {
                parameterText = parameterText.Slice(0, expectedSize - 1);
            }
        }

        if (!IsInitialized(entryListType))
        {
            SetInitialized(entryListType);
            UpdateMorphParserForState();

            if (IntEx.TryParseInvariant(parameterText, out expectedSize) && expectedSize is >= 0 and <= CollectionsEx.CollectionPreallocationLimit)
            {
                entries.SetCapacity(expectedSize);
                return true;
            }
        }

        return parse(parameterText, entries);
    }

    private bool TryParseStandardListItem<T>(EntryListType entryListType, ReadOnlySpan<char> parameterText, ArrayBuilder<T> entries, EntryParserForArray<T> parse)
    {
        if (!IsInitialized(entryListType))
        {
            SetInitialized(entryListType);

            if (IntEx.TryParseInvariant(parameterText, out var expectedSize) && expectedSize is >= 0 and <= CollectionsEx.CollectionPreallocationLimit)
            {
                entries.SetCapacity(expectedSize);
                return true;
            }
        }

        return parse(parameterText, entries);
    }

    private bool TryParseCompoundSyllable(ReadOnlySpan<char> parameters)
    {
        var state = 0;

        foreach (var part in parameters.SplitOnTabOrSpace())
        {
            if (state == 0)
            {
                if (IntEx.TryParseInvariant(part) is { } maxValue)
                {
                    _builder.CompoundMaxSyllable = maxValue;
                    _builder.CompoundVowels = DefaultCompoundVowels;
                    state = 1;
                    continue;
                }
            }
            else if (state == 1)
            {
                _builder.CompoundVowels = CharacterSet.Create(part);
                state = 2;
                continue;
            }

            state = -1;
            break;
        }

        if (state > 0)
        {
            return true;
        }

        LogWarning("Failed to parse CompoundMaxSyllable value from: " + parameters.ToString());
        return false;
    }

    private static CultureInfo GetCultureFromLanguage(string? language)
    {
        if (language is not { Length: > 0 })
        {
            return CultureInfo.InvariantCulture;
        }

        language = language.Replace('_', '-');

        try
        {
            return new CultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            var dashIndex = language.IndexOf('-');
            if (dashIndex > 0)
            {
                return GetCultureFromLanguage(language.Substring(0, dashIndex));
            }
            else
            {
                return CultureInfo.InvariantCulture;
            }
        }
        catch (ArgumentException)
        {
            return CultureInfo.InvariantCulture;
        }
    }

    private bool TryParsePhone(ReadOnlySpan<char> parameterText, ArrayBuilder<PhoneticEntry> entries)
    {
        var rule = ReadOnlySpan<char>.Empty;
        var replace = ReadOnlySpan<char>.Empty;
        var state = 0;

        foreach (var part in parameterText.SplitOnTabOrSpace())
        {
            if (state == 0)
            {
                rule = part;
                state = 1;
                continue;
            }
            else if (state == 1)
            {
                replace = part;
                state = 2;
                continue;
            }
            else
            {
                state = -1;
                break;
            }
        }

        if (state < 1)
        {
            LogWarning("Failed to parse phone line: " + parameterText.ToString());
            return false;
        }

        entries.Add(new PhoneticEntry(rule.ToString(), replace.ToStringWithoutChars('_')));

        return true;
    }

    private bool TryParseMapEntry(ReadOnlySpan<char> parameterText, ArrayBuilder<MapEntry> entries)
    {
        var valuesBuilder = new ArrayBuilder<string>(parameterText.Length / 2);

        for (var k = 0; k < parameterText.Length; k++)
        {
            if (isComment(parameterText, k))
            {
                break;
            }

            if (parameterText[k] == '(')
            {
                var searchPos = parameterText.IndexOf(')', k);
                if (searchPos >= 0)
                {
                    valuesBuilder.Add(parameterText.Slice(k + 1, searchPos - k - 1).ToString());
                    k = searchPos;
                    continue;
                }
            }

            valuesBuilder.Add(parameterText[k].ToString());
        }

        entries.Add(new MapEntry(valuesBuilder.Extract()));

        return true;

        static bool isComment(ReadOnlySpan<char> text, int i)
        {
            do
            {
                switch (text[i])
                {
                    case ' ' or '\t':
                        // Read through any leading spaces to they don't end up in the character set
                        i++;
                        break;
                    case '#':
                        // If a hash character is encountered, it's probably a comment
                        return true;
                    default:
                        return false;
                }
            }
            while (i < text.Length);

            return false;
        }
    }

    private bool TryParseConv(ReadOnlySpan<char> parameterText, EntryListType entryListType, ref TextDictionary<MultiReplacementEntry> entries)
    {
        if (!IsInitialized(entryListType))
        {
            SetInitialized(entryListType);

            if (IntEx.TryParseInvariant(ParseLeadingDigits(parameterText), out var expectedSize) && expectedSize >= 0)
            {
                entries ??= new(expectedSize);

                return true;
            }
        }

        entries ??= [];

        var pattern1 = ReadOnlySpan<char>.Empty;
        var pattern2 = ReadOnlySpan<char>.Empty;
        var state = 0;
        foreach (var part in parameterText.SplitOnTabOrSpace())
        {
            if (state == 0)
            {
                pattern1 = part;
                state = 1;
                continue;
            }
            else if (state == 1)
            {
                pattern2 = part;
                state = 2;
                break; // There may be comments after this, so processing needs to stop here
            }
            else
            {
                ExceptionEx.ThrowInvalidOperation("Invalid CONV");
            }
        }

        if (state < 2)
        {
            LogWarning(StringEx.ConcatString("Bad ", entryListType.ToString(), ": ", parameterText));
            return false;
        }

        var type = ReplacementValueType.Med;

        if (pattern1.StartsWith('_'))
        {
            type |= ReplacementValueType.Ini;
            pattern1 = pattern1.Slice(1);
        }

        if (pattern1.EndsWith('_'))
        {
            type |= ReplacementValueType.Fin;
            pattern1 = pattern1.Slice(0, pattern1.Length - 1);
        }

        var pattern1String = pattern1.ReplaceIntoString('_', ' ');

        // find existing entry
        if (!entries.TryGetValue(pattern1String, out var entry))
        {
            // make a new entry if none exists
            entry = new MultiReplacementEntry(pattern1String);
            entries.Add(entry.Pattern, entry);
        }

        entry.Set(type, pattern2.ReplaceIntoString('_', ' '));
        return true;
    }

    private bool TryParseBreak(ReadOnlySpan<char> parameterText, ArrayBuilder<string> entries)
    {
        entries.Add(parameterText.ToString());
        return true;
    }

    private bool TryParseAliasF(ReadOnlySpan<char> parameterText, ArrayBuilder<FlagSet> entries)
    {
        entries.Add(FlagParser.ParseFlagSet(parameterText));
        return true;
    }

    private bool TryParseAliasM(ReadOnlySpan<char> parameterText, ArrayBuilder<MorphSet> entries)
    {
        entries.Add(MorphParser.ParseMorphSet(parameterText));
        return true;
    }

    private bool TryParseCompoundRuleIntoList(ReadOnlySpan<char> parameterText, ArrayBuilder<CompoundRule> entries)
    {
        FlagValue[] values;
        if (parameterText.Contains('('))
        {
            var entryBuilder = new ArrayBuilder<FlagValue>();
            for (var index = 0; index < parameterText.Length; index++)
            {
                var indexBegin = index;
                var indexEnd = indexBegin + 1;
                if (parameterText[indexBegin] == '(' && parameterText.IndexOf(')', indexEnd) is int closeParenIndex and >= 0)
                {
                    indexBegin = indexEnd;
                    indexEnd = closeParenIndex;
                    index = closeParenIndex;
                }

                var beginFlagValue = new FlagValue(parameterText[indexBegin]);
                if (beginFlagValue.IsWildcard)
                {
                    entryBuilder.Add(beginFlagValue);
                }
                else
                {
                    entryBuilder.AddRange(FlagParser.ParseFlagsInOrder(parameterText.Slice(indexBegin, indexEnd - indexBegin)));
                }
            }

            values = entryBuilder.Extract();
        }
        else
        {
            values = FlagParser.ParseFlagsInOrder(parameterText);
        }

        entries.Add(new(values));
        return true;
    }

    private bool TryParseAffixIntoList<TAffixEntry>(ReadOnlySpan<char> parameterText, AffixCollection<TAffixEntry>.BuilderBase affixBuilder)
        where TAffixEntry : AffixEntry
    {
        var affixParser = new AffixParametersParser(parameterText);

        if (!affixParser.TryParseNextAffixFlag(FlagParser, out var aFlag))
        {
            LogWarning("Failed to parse affix flag: " + parameterText.ToString());
            return false;
        }

        var group1 = affixParser.ParseNextArgument();
        var group2 = affixParser.ParseNextArgument();

        if (group1.IsEmpty || group2.IsEmpty)
        {
            LogWarning("Failed to parse affix line: " + parameterText.ToString());
            return false;
        }

        var contClass = FlagSet.Empty;

        var groupBuilder = affixBuilder.ForGroup(aFlag);
        if (!groupBuilder.IsInitialized)
        {
            // If the affix group is new, this should be the init line for it
            var options = AffixEntryOptions.None;
            if (group1.StartsWith('Y'))
            {
                options |= AffixEntryOptions.CrossProduct;
            }
            if (_builder.AliasMBuilder is { Count: > 0 })
            {
                options |= AffixEntryOptions.AliasM;
            }
            if (_builder.AliasFBuilder is { Count: > 0 })
            {
                options |= AffixEntryOptions.AliasF;
            }

            _ = IntEx.TryParseInvariant(group2, out var expectedEntryCount);

            groupBuilder.Initialize(options, expectedEntryCount);

            return true;
        }

        var group3 = affixParser.ParseNextArgument();
        if (group3.IsEmpty && group2.EqualsOrdinal('.'))
        {
            // In some special cases it seems as if the group 2 is blank but groups 1 and 3 have values in them.
            // I think this is a way to make a blank affix value.
            group3 = group2;
            group2 = [];
        }

        // piece 3 - is string to strip or 0 for null
        string strip;
        if (group1.EqualsOrdinal('0'))
        {
            strip = string.Empty;
        }
        else if (_builder.Options.HasFlagEx(AffixConfigOptions.ComplexPrefixes))
        {
            strip = group1.ToStringReversed();
        }
        else
        {
            strip = group1.ToString();
        }

        // piece 4 - is affix string or 0 for null

        if (group2.IndexOf('/') is int affixSlashIndex and >= 0)
        {
            if (_builder.AliasFBuilder is { Count: > 0 } aliasF)
            {
                if (IntEx.TryParseInvariant(group2.Slice(affixSlashIndex + 1), out var aliasNumber) && aliasNumber > 0 && aliasNumber <= aliasF.Count)
                {
                    contClass = aliasF[aliasNumber - 1];
                }
                else
                {
                    LogWarning(StringEx.ConcatString("Failed to parse contclasses from : ", parameterText));
                    return false;
                }
            }
            else
            {
                contClass = FlagParser.ParseFlagSet(group2.Slice(affixSlashIndex + 1));
            }

            group2 = group2.Slice(0, affixSlashIndex);
        }

        string affixText;
        if (group2.IsEmpty || group2.EqualsOrdinal('0'))
        {
            affixText = string.Empty;
        }
        else
        {
            affixText = _builder.Options.HasFlagEx(AffixConfigOptions.ComplexPrefixes)
                ? group2.ToStringReversed()
                : group2.ToString();

            if (_builder.IgnoredChars.HasItems)
            {
                affixText = _builder.IgnoredChars.RemoveChars(affixText);

                if (affixText.EqualsOrdinal('0'))
                {
                    affixText = string.Empty; // This should be re-checked after characters are removed
                }
            }
        }

        // piece 5 - is the conditions descriptions
        var conditionText = group3;
        if (_builder.Options.HasFlagEx(AffixConfigOptions.ComplexPrefixes))
        {
            conditionText = ReverseCondition(conditionText);
        }

        var conditions = CharacterConditionGroup.Parse(conditionText);
        if (strip.Length != 0 && !conditions.MatchesAnySingleCharacter)
        {
            if (conditions.IsOnlyPossibleMatch(strip))
            {
                // determine if the condition is redundant
                conditions = CharacterConditionGroup.AllowAnySingleCharacter;
            }
        }

        var group4 = affixParser.ParseFinalArguments();

        // piece 6
        MorphSet morph;
        if (group4.Length > 0)
        {
            var morphAffixText = group4;
            if (_builder.AliasMBuilder is { Count: > 0 } aliasM)
            {
                if (IntEx.TryParseInvariant(morphAffixText, out var morphNumber) && morphNumber > 0 && morphNumber <= aliasM.Count)
                {
                    morph = aliasM[morphNumber - 1];
                }
                else
                {
                    LogWarning(StringEx.ConcatString("Failed to parse morph ", morphAffixText, " from: ", parameterText));
                    return false;
                }
            }
            else
            {
                morph = MorphParser.ParseMorphSet(morphAffixText);
            }
        }
        else
        {
            morph = MorphSet.Empty;
        }

        if (!_builder.HasContClass && contClass.HasItems)
        {
            _builder.HasContClass = true;
        }

        groupBuilder.AddEntry(
            strip,
            affixText,
            conditions,
            morph,
            contClass);

        return true;
    }

    private void UpdateMorphParserForState()
    {
        var complexPrefixes = _builder.Options.HasFlagEx(AffixConfigOptions.ComplexPrefixes);

        if (MorphParser.ComplexPrefixes != complexPrefixes)
        {
            MorphParser = new(complexPrefixes: complexPrefixes);
        }
    }

    private static ReadOnlySpan<char> ReverseCondition(ReadOnlySpan<char> conditionText)
    {
        return conditionText.Length <= 1 ? conditionText : reverseCharacters(conditionText);

        static ReadOnlySpan<char> reverseCharacters(ReadOnlySpan<char> conditionText)
        {
            Span<char> chars = conditionText.ToArray();
            chars.Reverse();

            var neg = false;
            var lastIndex = chars.Length - 1;

            for (var k = lastIndex; k >= 0; k--)
            {
                switch (chars[k])
                {
                    case '[':
                        if (neg)
                        {
                            if (k < lastIndex)
                            {
                                chars[k + 1] = '[';
                            }
                        }
                        else
                        {
                            chars[k] = ']';
                        }

                        break;
                    case ']':
                        chars[k] = '[';
                        if (neg && k < lastIndex)
                        {
                            chars[k + 1] = '^';
                        }

                        neg = false;

                        break;
                    case '^':
                        if (k < lastIndex)
                        {
                            if (chars[k + 1] == ']')
                            {
                                neg = true;
                            }
                            else if (neg)
                            {
                                chars[k + 1] = chars[k];
                            }
                        }

                        break;
                    default:
                        if (neg && k < lastIndex)
                        {
                            chars[k + 1] = chars[k];
                        }
                        break;
                }
            }

            return chars;
        }
    }

    private bool TryParseReplacements(ReadOnlySpan<char> parameterText, ArrayBuilder<SingleReplacement> entries)
    {
        var pattern = ReadOnlySpan<char>.Empty;
        var outString = ReadOnlySpan<char>.Empty;
        var state = 0;
        foreach (var part in parameterText.SplitOnTabOrSpace())
        {
            if (state == 0)
            {
                pattern = part;
                state = 1;
                continue;
            }
            else if (state == 1)
            {
                outString = part;
                state = 2;
                continue;
            }
            else
            {
                break;
            }
        }

        if (state < 1)
        {
            LogWarning(StringEx.ConcatString("Failed to parse replacements from: ", parameterText));
            return false;
        }

        var type = ReplacementValueType.Med;

        if (pattern.StartsWith('^'))
        {
            type |= ReplacementValueType.Ini;
            pattern = pattern.Slice(1);
        }

        if (pattern.EndsWith('$'))
        {
            type |= ReplacementValueType.Fin;
            pattern = pattern.Slice(0, pattern.Length - 1);
        }

        entries.Add(new SingleReplacement(
            pattern.ReplaceIntoString('_', ' '),
            outString.ReplaceIntoString('_', ' '),
            type));

        return true;
    }

    private enum ParseCheckCompoundPatternState : sbyte
    {
        ParsePattern1 = 0,
        ParsePattern2 = 1,
        ParsePattern3 = 2,
        Done = 3,
        UnknownFailure = -1,
        FailCondition1 = -2,
        FailCondition2 = -3
    }

    private bool TryParseCheckCompoundPatternIntoCompoundPatterns(ReadOnlySpan<char> parameterText, ArrayBuilder<PatternEntry> entries)
    {
        int slashIndex;
        var pattern1 = ReadOnlySpan<char>.Empty;
        var pattern2 = ReadOnlySpan<char>.Empty;
        var pattern3 = ReadOnlySpan<char>.Empty;
        FlagValue condition1 = default;
        FlagValue condition2 = default;
        var state = ParseCheckCompoundPatternState.ParsePattern1;

        foreach (var part in parameterText.SplitOnTabOrSpace())
        {
            if (state == ParseCheckCompoundPatternState.ParsePattern1)
            {
                slashIndex = part.IndexOf('/');
                if (slashIndex >= 0)
                {
                    condition1 = FlagParser.ParseFlagOrDefault(part.Slice(slashIndex + 1));
                    if (!condition1.HasValue)
                    {
                        state = ParseCheckCompoundPatternState.FailCondition1;
                        break;
                    }

                    pattern1 = part.Slice(0, slashIndex);
                }
                else
                {
                    pattern1 = part;
                }


                state = ParseCheckCompoundPatternState.ParsePattern2;
                continue;
            }
            else if (state == ParseCheckCompoundPatternState.ParsePattern2)
            {
                slashIndex = part.IndexOf('/');
                if (slashIndex >= 0)
                {
                    condition2 = FlagParser.ParseFlagOrDefault(part.Slice(slashIndex + 1));
                    if (!condition2.HasValue)
                    {
                        state = ParseCheckCompoundPatternState.FailCondition2;
                        break;
                    }

                    pattern2 = part.Slice(0, slashIndex);
                }
                else
                {
                    pattern2 = part;
                }

                state = ParseCheckCompoundPatternState.ParsePattern3;
                continue;
            }
            else if (state == ParseCheckCompoundPatternState.ParsePattern3)
            {
                pattern3 = part;
                _builder.EnableOptions(AffixConfigOptions.SimplifiedCompound);

                state = ParseCheckCompoundPatternState.Done;
                continue;
            }
            else
            {
                state = ParseCheckCompoundPatternState.UnknownFailure;
                break;
            }
        }

        if (state < 0)
        {
            if (state == ParseCheckCompoundPatternState.FailCondition1)
            {
                LogWarning(StringEx.ConcatString("Failed to parse pattern condition 1 from: ", parameterText));
            }
            else if (state == ParseCheckCompoundPatternState.FailCondition2)
            {
                LogWarning(StringEx.ConcatString("Failed to parse pattern condition 2 from: ", parameterText));
            }
            else
            {
                LogWarning(StringEx.ConcatString("Failed to parse compound pattern from: ", parameterText));
            }

            return false;
        }

        entries.Add(new PatternEntry(
            pattern1.ToString(),
            pattern2.ToString(),
            pattern3.ToString(),
            condition1,
            condition2));

        return true;
    }

    private bool TrySetFlagMode(ReadOnlySpan<char> modeText)
    {
        if (modeText.IsEmpty)
        {
            LogWarning("Attempt to set empty flag mode.");
            return false;
        }

        if (TryParseFlagMode(modeText) is not { } mode)
        {
            LogWarning(StringEx.ConcatString("Unknown FlagMode: ", modeText));
            return false;
        }

        if (_builder.FlagMode == mode)
        {
            LogWarning(StringEx.ConcatString("Redundant FlagMode: ", modeText));
            return false;
        }

        _builder.FlagMode = mode;
        FlagParser = FlagParser.WithMode(mode);
        return true;
    }

    private void LogWarning(string text)
    {
        _builder.LogWarning(text);
    }

    private static ReadOnlySpan<char> ParseLeadingDigits(ReadOnlySpan<char> text)
    {
        text = text.TrimStart();

        if (text.Length > 0)
        {
            var firstNonDigitIndex = 0;
            for (; firstNonDigitIndex < text.Length && char.IsDigit(text[firstNonDigitIndex]); firstNonDigitIndex++) ;

            if (firstNonDigitIndex < text.Length)
            {
                text = text.Slice(0, firstNonDigitIndex);
            }
        }

        return text;
    }

    private static FlagParsingMode? TryParseFlagMode(ReadOnlySpan<char> value)
    {
        if (value.Length >= 3)
        {
            if (value.StartsWith("CHAR", StringComparison.OrdinalIgnoreCase))
            {
                return FlagParsingMode.Char;
            }
            if (value.StartsWith("LONG", StringComparison.OrdinalIgnoreCase))
            {
                return FlagParsingMode.Long;
            }
            if (value.StartsWith("NUM", StringComparison.OrdinalIgnoreCase))
            {
                return FlagParsingMode.Num;
            }
            if (value.StartsWith("UTF", StringComparison.OrdinalIgnoreCase) || value.StartsWith("UNI", StringComparison.OrdinalIgnoreCase))
            {
                return FlagParsingMode.Uni;
            }
        }

        return null;
    }

    private delegate bool EntryParserForBuilder<T>(ReadOnlySpan<char> parameterText, ArrayBuilder<T> entries);

    private delegate bool EntryParserForArray<T>(ReadOnlySpan<char> parameterText, ArrayBuilder<T> entries);

    [Flags]
    private enum EntryListType : short
    {
        None = 0,
        Replacements = 1 << 0,
        CompoundRules = 1 << 1,
        CompoundPatterns = 1 << 2,
        AliasF = 1 << 3,
        AliasM = 1 << 4,
        Break = 1 << 5,
        Iconv = 1 << 6,
        Oconv = 1 << 7,
        Map = 1 << 8,
        Phone = 1 << 9
    }

    private enum AffixReaderCommandKind : byte
    {
        /// <summary>
        /// parse in the try string
        /// </summary>
        Flag,
        /// <summary>
        /// parse in the keyboard string
        /// </summary>
        KeyString,
        /// <summary>
        /// parse in the try string
        /// </summary>
        TryString,
        /// <summary>
        /// parse in the name of the character set used by the .dict and .aff
        /// </summary>
        SetEncoding,
        /// <summary>
        /// parse in the language for language specific codes
        /// </summary>
        Language,
        /// <summary>
        /// parse in the flag used by compound_check() method
        /// </summary>
        CompoundSyllableNum,
        /// <summary>
        /// parse in the extra word characters
        /// </summary>
        WordChars,
        /// <summary>
        /// parse in the ignored characters (for example, Arabic optional diacretics characters)
        /// </summary>
        Ignore,
        /// <summary>
        /// parse in the flag used by the controlled compound words
        /// </summary>
        CompoundFlag,
        /// <summary>
        /// parse in the flag used by compound words
        /// </summary>
        CompoundMiddle,
        /// <summary>
        /// parse in the flag used by compound words
        /// </summary>
        CompoundBegin,
        /// <summary>
        /// parse in the flag used by compound words
        /// </summary>
        CompoundEnd,
        /// <summary>
        /// parse in the data used by compound_check() method
        /// </summary>
        CompoundWordMax,
        /// <summary>
        /// parse in the minimal length for words in compounds
        /// </summary>
        CompoundMin,
        /// <summary>
        /// parse in the flag sign compounds in dictionary
        /// </summary>
        CompoundRoot,
        /// <summary>
        /// parse in the flag used by compound_check() method
        /// </summary>
        CompoundPermitFlag,
        /// <summary>
        /// parse in the flag used by compound_check() method
        /// </summary>
        CompoundForbidFlag,
        /// <summary>
        /// parse in the max. words and syllables in compounds
        /// </summary>
        CompoundSyllable,
        NoSuggest,
        NoNGramSuggest,
        /// <summary>
        /// parse in the flag used by forbidden words
        /// </summary>
        ForbiddenWord,
        /// <summary>
        /// parse in the flag used by forbidden words
        /// </summary>
        LemmaPresent,
        /// <summary>
        /// parse in the flag used by circumfixes
        /// </summary>
        Circumfix,
        /// <summary>
        /// parse in the flag used by fogemorphemes
        /// </summary>
        OnlyInCompound,
        /// <summary>
        /// parse in the flag used by `needaffixs'
        /// </summary>
        NeedAffix,
        /// <summary>
        /// parse in the typical fault correcting table
        /// </summary>
        Replacement,
        /// <summary>
        /// parse in the input conversion table
        /// </summary>
        InputConversions,
        /// <summary>
        /// parse in the output conversion table
        /// </summary>
        OutputConversions,
        /// <summary>
        /// parse in the phonetic conversion table
        /// </summary>
        Phone,
        /// <summary>
        /// parse in the checkcompoundpattern table
        /// </summary>
        CheckCompoundPattern,
        /// <summary>
        /// parse in the defcompound table
        /// </summary>
        CompoundRule,
        /// <summary>
        /// parse in the related character map table
        /// </summary>
        Map,
        /// <summary>
        /// parse in the word breakpoints table
        /// </summary>
        Break,
        Version,
        MaxNgramSuggestions,
        MaxDifferency,
        MaxCompoundSuggestions,
        /// <summary>
        /// parse in the flag used by forbidden words
        /// </summary>
        KeepCase,
        ForceUpperCase,
        Warn,
        SubStandard,
        Prefix,
        Suffix,
        AliasF,
        AliasM
    }
}

