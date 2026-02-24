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
using System.Globalization;
using System.Text;

namespace WeCantSpell.Hunspell;

public partial class AffixConfig
{
    [DebuggerDisplay("Prefixes = {Prefixes}, Suffixes = {Suffixes}")]
    public sealed class Builder
    {
        public Builder()
        {
        }

        /// <summary>
        /// Various affix options.
        /// </summary>
        /// <seealso cref="AffixConfig.Options"/>
        public AffixConfigOptions Options { get; set; }

        /// <summary>
        /// The flag type.
        /// </summary>
        /// <seealso cref="AffixConfig.FlagMode"/>
        public FlagParsingMode FlagMode { get; set; } = FlagParsingMode.Char;

        /// <summary>
        /// A string of text representing a keyboard layout.
        /// </summary>
        /// <seealso cref="AffixConfig.KeyString"/>
        public string? KeyString { get; set; }

        /// <summary>
        /// Characters used to permit some suggestions.
        /// </summary>
        /// <seealso cref="AffixConfig.TryString"/>
        public string? TryString { get; set; }

        /// <summary>
        /// The language code used for language specific functions.
        /// </summary>
        /// <seealso cref="AffixConfig.Language"/>
        public string? Language { get; set; }

        /// <summary>
        /// The culture associated with the language.
        /// </summary>
        /// <seealso cref="AffixConfig.Culture"/>
        public CultureInfo? Culture { get; set; }

        /// <summary>
        /// Flag indicating that a word may be in compound words.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundFlag"/>
        public FlagValue CompoundFlag;

        /// <summary>
        /// A flag indicating that a word may be the first element in a compound word.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundBegin"/>
        public FlagValue CompoundBegin;

        /// <summary>
        /// A flag indicating that a word may be the last element in a compound word.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundEnd"/>
        public FlagValue CompoundEnd;

        /// <summary>
        /// A flag indicating that a word may be a middle element in a compound word.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundMiddle"/>
        public FlagValue CompoundMiddle;

        /// <summary>
        /// Maximum word count in a compound word.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundWordMax"/>
        public int? CompoundWordMax { get; set; }

        /// <summary>
        /// Minimum length of words used for compounding.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundMin"/>
        public int? CompoundMin { get; set; }

        /// <summary>
        /// A flag marking compounds as a compound root.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundRoot"/>
        public FlagValue CompoundRoot;

        /// <summary>
        /// A flag indicating that an affix may be inside of compounds.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundPermitFlag"/>
        public FlagValue CompoundPermitFlag;

        /// <summary>
        /// A flag forbidding a suffix from compounding.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundForbidFlag"/>
        public FlagValue CompoundForbidFlag;

        /// <summary>
        /// Flag indicating that a word should not be used as a suggestion.
        /// </summary>
        /// <seealso cref="AffixConfig.NoSuggest"/>
        public FlagValue NoSuggest;

        /// <summary>
        /// Flag indicating that a word should not be used in ngram based suggestions.
        /// </summary>
        /// <seealso cref="AffixConfig.NoNgramSuggest"/>
        public FlagValue NoNgramSuggest;

        /// <summary>
        /// A flag indicating a forbidden word form.
        /// </summary>
        /// <seealso cref="AffixConfig.ForbiddenWord"/>
        public FlagValue? ForbiddenWord;

        /// <summary>
        /// A flag used by forbidden words.
        /// </summary>
        /// <seealso cref="AffixConfig.LemmaPresent"/>
        public FlagValue LemmaPresent;

        /// <summary>
        /// A flag indicating that affixes may be on a word when this word also has prefix with <see cref="Circumfix"/> flag and vice versa.
        /// </summary>
        /// <seealso cref="AffixConfig.Circumfix"/>
        public FlagValue Circumfix;

        /// <summary>
        /// A flag indicating that a suffix may be only inside of compounds.
        /// </summary>
        /// <seealso cref="AffixConfig.OnlyInCompound"/>
        public FlagValue OnlyInCompound;

        /// <summary>
        /// A flag signing virtual stems in the dictionary.
        /// </summary>
        /// <seealso cref="AffixConfig.NeedAffix"/>
        public FlagValue NeedAffix;

        /// <summary>
        /// Maximum number of n-gram suggestions. A value of 0 switches off the n-gram suggestions.
        /// </summary>
        /// <seealso cref="AffixConfig.MaxNgramSuggestions"/>
        public int? MaxNgramSuggestions { get; set; }

        /// <summary>
        /// Similarity factor for the n-gram based suggestions.
        /// </summary>
        /// <seealso cref="AffixConfig.MaxDifferency"/>
        public int? MaxDifferency { get; set; }

        /// <summary>
        /// Maximum number of suggested compound words generated by compound rule.
        /// </summary>
        /// <seealso cref="AffixConfig.MaxCompoundSuggestions"/>
        public int? MaxCompoundSuggestions { get; set; }

        /// <summary>
        /// A flag indicating that uppercased and capitalized forms of words are forbidden.
        /// </summary>
        /// <seealso cref="AffixConfig.KeepCase"/>
        public FlagValue KeepCase;

        /// <summary>
        /// A flag forcing capitalization of the whole compound word.
        /// </summary>
        /// <seealso cref="AffixConfig.ForceUpperCase"/>
        public FlagValue ForceUpperCase;

        /// <summary>
        /// Flag indicating a rare word that is also often a spelling mistake.
        /// </summary>
        /// <seealso cref="AffixConfig.Warn"/>
        public FlagValue Warn;

        /// <summary>
        /// Flag signing affix rules and dictionary words not used in morphological generation.
        /// </summary>
        /// <seealso cref="AffixConfig.SubStandard"/>
        public FlagValue SubStandard;

        /// <summary>
        /// A flag used by compound check.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundSyllableNum"/>
        public string? CompoundSyllableNum;

        /// <summary>
        /// The encoding to be used in morpheme, affix, and dictionary files.
        /// </summary>
        /// <seealso cref="AffixConfig.Encoding"/>
        public Encoding Encoding { get; set; } = AffixReader.DefaultEncoding;

        /// <summary>
        /// Specifies modifications to try first.
        /// </summary>
        /// <seealso cref="AffixConfig.Replacements"/>
        public IList<SingleReplacement> Replacements => ReplacementsBuilder;

        internal ArrayBuilder<SingleReplacement> ReplacementsBuilder = [];

        /// <summary>
        /// Suffixes attached to root words to make other words.
        /// </summary>
        /// <seealso cref="AffixConfig.Suffixes"/>
        public SuffixCollection.Builder Suffixes = new();

        /// <summary>
        /// Preffixes attached to root words to make other words.
        /// </summary>
        /// <seealso cref="AffixConfig.Prefixes"/>
        public PrefixCollection.Builder Prefixes = new();

        /// <summary>
        /// Ordinal numbers for affix flag compression.
        /// </summary>
        /// <seealso cref="AffixConfig.AliasF"/>
        public IList<FlagSet> AliasF => AliasFBuilder;

        internal ArrayBuilder<FlagSet> AliasFBuilder = [];

        /// <summary>
        /// Values used for morphological alias compression.
        /// </summary>
        /// <seealso cref="AffixConfig.AliasM"/>
        public IList<MorphSet> AliasM => AliasMBuilder;

        internal ArrayBuilder<MorphSet> AliasMBuilder = [];

        /// <summary>
        /// Defines custom compound patterns with a regex-like syntax.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundRules"/>
        public IList<CompoundRule> CompoundRules => CompoundRulesBuilder;

        internal ArrayBuilder<CompoundRule> CompoundRulesBuilder = [];

        /// <summary>
        /// Forbid compounding, if the first word in the compound ends with endchars, and
        /// next word begins with beginchars and(optionally) they have the requested flags.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundPatterns"/>
        public IList<PatternEntry> CompoundPatterns => CompoundPatternsBuilder;

        internal ArrayBuilder<PatternEntry> CompoundPatternsBuilder = [];

        /// <summary>
        /// Defines new break points for breaking words and checking word parts separately.
        /// </summary>
        /// <seealso cref="AffixConfig.BreakPoints"/>
        public IList<string> BreakPoints => BreakPointsBuilder;

        internal ArrayBuilder<string> BreakPointsBuilder = [];

        /// <summary>
        /// Input conversion entries.
        /// </summary>
        /// <seealso cref="AffixConfig.InputConversions"/>
        public IDictionary<string, MultiReplacementEntry> InputConversions => InputConversionsBuilder;

        internal TextDictionary<MultiReplacementEntry> InputConversionsBuilder = [];

        /// <summary>
        /// Output conversion entries.
        /// </summary>
        /// <seealso cref="AffixConfig.OutputConversions"/>
        public IDictionary<string, MultiReplacementEntry> OutputConversions => OutputConversionsBuilder;

        internal TextDictionary<MultiReplacementEntry> OutputConversionsBuilder = [];

        /// <summary>
        /// Mappings between related characters.
        /// </summary>
        /// <seealso cref="AffixConfig.RelatedCharacterMap"/>
        public IList<MapEntry> RelatedCharacterMap => RelatedCharacterMapBuilder;

        internal ArrayBuilder<MapEntry> RelatedCharacterMapBuilder = [];

        /// <summary>
        /// Phonetic transcription entries.
        /// </summary>
        /// <seealso cref="AffixConfig.Phone"/>
        public IList<PhoneticEntry> Phone => PhoneBuilder;

        internal ArrayBuilder<PhoneticEntry> PhoneBuilder = [];

        /// <summary>
        /// Maximum syllable number, that may be in a
        /// compound, if words in compounds are more than <see cref="CompoundWordMax"/>.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundMaxSyllable"/>
        public int CompoundMaxSyllable { get; set; }

        /// <summary>
        /// Voewls for calculating syllables.
        /// </summary>
        /// <seealso cref="AffixConfig.CompoundVowels"/>
        public CharacterSet CompoundVowels { get; set; } = CharacterSet.Empty;

        /// <summary>
        /// Extra word characters.
        /// </summary>
        /// <seealso cref="AffixConfig.WordChars"/>
        public CharacterSet WordChars { get; set; } = CharacterSet.Empty;

        /// <summary>
        /// Ignored characters (for example, Arabic optional diacretics characters)
        /// for dictionary words, affixes and input words.
        /// </summary>
        /// <seealso cref="AffixConfig.IgnoredChars"/>
        public CharacterSet IgnoredChars { get; set; } = CharacterSet.Empty;

        /// <summary>
        /// Affix and dictionary file version string.
        /// </summary>
        /// <seealso cref="AffixConfig.Version"/>
        public string? Version { get; set; }

        /// <summary>
        /// Indicates that some of the affix entries have "cont class".
        /// </summary>
        public bool HasContClass { get; set; }

        /// <summary>
        /// A list of the warnings that were produced while reading or building an <see cref="AffixConfig"/>.
        /// </summary>
        public List<string> Warnings { get; } = [];

        /// <summary>
        /// Builds a new <see cref="AffixConfig"/> based on the values set in this builder.
        /// </summary>
        /// <returns>A new affix config.</returns>
        /// <seealso cref="AffixConfig"/>
        public AffixConfig Build() => BuildOrExtract(extract: false);

        /// <summary>
        /// Builds a new <see cref="AffixConfig"/> based on the values set in the builder.
        /// </summary>
        /// <returns>A new affix config.</returns>
        /// <remarks>
        /// This method can leave the builder in an invalid state
        /// but provides better performance for file reads.
        /// </remarks>
        /// <seealso cref="AffixConfig"/>
        public AffixConfig Extract() => BuildOrExtract(extract: true);

        /// <summary>
        /// Builds a new <see cref="AffixConfig"/> based on the values set in the builder.
        /// </summary>
        /// <param name="extract"><c>true</c> to build an <see cref="AffixConfig"/> at the expense of this builder.</param>
        /// <returns>A new affix config.</returns>
        /// <seealso cref="AffixConfig"/>
        /// <remarks>
        /// This method can leave the builder in an invalid state
        /// but provides better performance for file reads.
        /// </remarks>
        private AffixConfig BuildOrExtract(bool extract)
        {
            var culture = CultureInfo.InvariantCulture;
            var comparer = StringComparer.InvariantCulture;

            if (Culture is not null)
            {
                culture = CultureInfo.ReadOnly(Culture);
                comparer = Culture.CompareInfo.GetStringComparer(CompareOptions.None) ?? StringComparer.InvariantCulture;
            }

            var config = new AffixConfig
            {
                Options = Options,
                FlagMode = FlagMode,
                KeyString = KeyString ?? DefaultKeyString,
                TryString = TryString ?? string.Empty,
                Language = Language,
                Culture = culture,
                IsHungarian = string.Equals(culture.TwoLetterISOLanguageName, "HU", StringComparison.OrdinalIgnoreCase),
                IsGerman = string.Equals(culture.TwoLetterISOLanguageName, "DE", StringComparison.OrdinalIgnoreCase),
                IsLanguageWithDashUsage = !string.IsNullOrEmpty(TryString) && TryString!.ContainsAny('-', 'a'),
                CultureUsesDottedI =
                    "AZ".Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)
                    || "TR".Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)
                    || "CRH".Equals(culture.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)
                    || "CRH".Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase), // wikipedia says: this is an ISO2 code even though it has 3 characters
                StringComparer = comparer,
                CompoundFlag = CompoundFlag,
                CompoundBegin = CompoundBegin,
                CompoundEnd = CompoundEnd,
                CompoundMiddle = CompoundMiddle,
                CompoundWordMax = CompoundWordMax,
                CompoundMin = CompoundMin ?? DefaultCompoundMinLength,
                CompoundRoot = CompoundRoot,
                CompoundPermitFlag = CompoundPermitFlag,
                CompoundForbidFlag = CompoundForbidFlag,
                NoSuggest = NoSuggest,
                NoNgramSuggest = NoNgramSuggest,
                ForbiddenWord = ForbiddenWord ?? SpecialFlags.ForbiddenWord,
                LemmaPresent = LemmaPresent,
                Circumfix = Circumfix,
                OnlyInCompound = OnlyInCompound,
                NeedAffix = NeedAffix,
                MaxNgramSuggestions = MaxNgramSuggestions ?? DefaultMaxNgramSuggestions,
                MaxDifferency = MaxDifferency,
                MaxCompoundSuggestions = MaxCompoundSuggestions ?? DefaultMaxCompoundSuggestions,
                KeepCase = KeepCase,
                ForceUpperCase = ForceUpperCase,
                Warn = Warn,
                SubStandard = SubStandard,
                CompoundSyllableNum = CompoundSyllableNum,
                Encoding = Encoding,
                CompoundMaxSyllable = CompoundMaxSyllable,
                CompoundVowels = CompoundVowels,
                WordChars = WordChars,
                IgnoredChars = IgnoredChars,
                Version = Version
            };

            if (extract)
            {
                config.InputConversions = InputConversionsBuilder.HasItems
                    ? MultiReplacementTable.TakeDictionary(InputConversionsBuilder)
                    : MultiReplacementTable.Empty;
                InputConversionsBuilder = [];
                config.OutputConversions = OutputConversionsBuilder.HasItems
                    ? MultiReplacementTable.TakeDictionary(OutputConversionsBuilder)
                    : MultiReplacementTable.Empty;
                OutputConversionsBuilder = [];
            }
            else
            {
                config.InputConversions = InputConversionsBuilder.HasItems
                    ? MultiReplacementTable.Create(InputConversionsBuilder)
                    : MultiReplacementTable.Empty;
                config.OutputConversions = OutputConversionsBuilder.HasItems
                    ? MultiReplacementTable.Create(OutputConversionsBuilder)
                    : MultiReplacementTable.Empty;
            }

            config.AliasF = new(AliasFBuilder.MakeOrExtractArray(extract));
            config.AliasM = new(AliasMBuilder.MakeOrExtractArray(extract));
            config.BreakPoints = new(BreakPointsBuilder.MakeOrExtractArray(extract));
            config.Replacements = new(ReplacementsBuilder.MakeOrExtractArray(extract));
            config.CompoundRules = new(CompoundRulesBuilder.MakeOrExtractArray(extract));
            config.CompoundPatterns = new(CompoundPatternsBuilder.MakeOrExtractArray(extract));
            config.RelatedCharacterMap = new(RelatedCharacterMapBuilder.MakeOrExtractArray(extract));
            config.Phone = new(PhoneBuilder.MakeOrExtractArray(extract));

            config.Prefixes = Prefixes.BuildOrExtract(extract);
            config.Suffixes = Suffixes.BuildOrExtract(extract);

            config.ContClasses = config.Prefixes.ContClasses.Union(config.Suffixes.ContClasses);

            config.Flags_CompoundFlag_CompoundBegin = FlagSet.Create(config.CompoundFlag, config.CompoundBegin);
            config.Flags_CompoundFlag_CompoundMiddle = FlagSet.Create(config.CompoundFlag, config.CompoundMiddle);
            config.Flags_CompoundFlag_CompoundEnd = FlagSet.Create(config.CompoundFlag, config.CompoundEnd);
            config.Flags_CompoundForbid_CompoundEnd = FlagSet.Create(config.CompoundForbidFlag, config.CompoundEnd);
            config.Flags_CompoundForbid_CompoundMiddle_CompoundEnd = config.Flags_CompoundForbid_CompoundEnd.Union(config.CompoundMiddle);
            config.Flags_OnlyInCompound_OnlyUpcase = FlagSet.Create(config.OnlyInCompound, SpecialFlags.OnlyUpcaseFlag);
            config.Flags_NeedAffix_OnlyInCompound = FlagSet.Create(config.NeedAffix, config.OnlyInCompound);
            config.Flags_NeedAffix_OnlyInCompound_OnlyUpcase = config.Flags_NeedAffix_OnlyInCompound.Union(SpecialFlags.OnlyUpcaseFlag);
            config.Flags_NeedAffix_OnlyInCompound_Circumfix = config.Flags_NeedAffix_OnlyInCompound.Union(config.Circumfix);
            config.Flags_NeedAffix_ForbiddenWord_OnlyUpcase = FlagSet.Create(config.NeedAffix, config.ForbiddenWord, SpecialFlags.OnlyUpcaseFlag);
            config.Flags_NeedAffix_ForbiddenWord_OnlyUpcase_NoSuggest = config.Flags_NeedAffix_ForbiddenWord_OnlyUpcase.Union(config.NoSuggest);
            config.Flags_ForbiddenWord_OnlyUpcase = FlagSet.Create(config.ForbiddenWord, SpecialFlags.OnlyUpcaseFlag);
            config.Flags_ForbiddenWord_OnlyUpcase_NoSuggest = config.Flags_ForbiddenWord_OnlyUpcase.Union(config.NoSuggest);
            config.Flags_ForbiddenWord_OnlyUpcase_NoSuggest_OnlyInCompound = config.Flags_ForbiddenWord_OnlyUpcase_NoSuggest.Union(config.OnlyInCompound);
            config.Flags_ForbiddenWord_NoSuggest = FlagSet.Create(config.ForbiddenWord, config.NoSuggest);
            config.Flags_ForbiddenWord_NoSuggest_SubStandard = config.Flags_ForbiddenWord_NoSuggest.Union(config.SubStandard);

            config.Warnings = [.. Warnings];

            return config;
        }

        /// <summary>
        /// Enables the given <paramref name="options"/> bits.
        /// </summary>
        /// <param name="options">Various bit options to enable.</param>
        public void EnableOptions(AffixConfigOptions options)
        {
            Options |= options;
        }

        public void LogWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}

