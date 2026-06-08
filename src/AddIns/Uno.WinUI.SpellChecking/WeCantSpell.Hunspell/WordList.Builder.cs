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

using System.Collections.Generic;
using System.Linq;

namespace WeCantSpell.Hunspell;

public partial class WordList
{
    public sealed class Builder
    {
        public Builder() : this(new AffixConfig.Builder().Extract())
        {
        }

        public Builder(AffixConfig affix)
        {
            Affix = affix;
            _entriesByRoot = [];
        }

        internal TextDictionary<WordEntryDetail[]> _entriesByRoot;

        public readonly AffixConfig Affix;

        /// <summary>
        /// Spelling replacement suggestions based on phonetics.
        /// </summary>
        public IList<SingleReplacement> PhoneticReplacements => _phoneticReplacements;

        internal ArrayBuilder<SingleReplacement> _phoneticReplacements { get; } = [];

        /// <summary>
        /// Adds a root word to this builder.
        /// </summary>
        /// <param name="word">The root word to add.</param>
        /// <returns><c>true</c> when a root is added, <c>false</c> otherwise.</returns>
        public bool Add(string word)
        {
            return Add(word, FlagSet.Empty, MorphSet.Empty, WordEntryOptions.None);
        }

        /// <summary>
        /// Adds a root word to this builder.
        /// </summary>
        /// <param name="word">The root word to add.</param>
        /// <param name="flags">The flags associated with the root <paramref name="word"/> detail entry.</param>
        /// <param name="morphs">The morphs associated with the root <paramref name="word"/> detail entry.</param>
        /// <param name="options">The options associated with the root <paramref name="word"/> detail entry.</param>
        /// <returns><c>true</c> when a root is added, <c>false</c> otherwise.</returns>
        public bool Add(string word, FlagSet flags, IEnumerable<string> morphs, WordEntryOptions options)
        {
            return Add(word, new WordEntryDetail(flags, MorphSet.Create(morphs), options));
        }

        /// <summary>
        /// Adds a root word to this builder.
        /// </summary>
        /// <param name="word">The root word to add.</param>
        /// <param name="flags">The flags associated with the root <paramref name="word"/> detail entry.</param>
        /// <param name="morphs">The morphs associated with the root <paramref name="word"/> detail entry.</param>
        /// <param name="options">The options associated with the root <paramref name="word"/> detail entry.</param>
        /// <returns><c>true</c> when a root is added, <c>false</c> otherwise.</returns>
        public bool Add(string word, FlagSet flags, MorphSet morphs, WordEntryOptions options)
        {
            return Add(word, new WordEntryDetail(flags, morphs, options));
        }

        /// <summary>
        /// Adds a root word to this builder.
        /// </summary>
        /// <param name="word">The root word to add details for.</param>
        /// <param name="detail">The details to associate with the root <paramref name="word"/>.</param>
        /// <returns><c>true</c> when a root is added, <c>false</c> otherwise.</returns>
        public bool Add(string word, WordEntryDetail detail)
        {
            return WordList.Add(_entriesByRoot, Affix, word, detail);
        }

        /// <summary>
        /// Removes all detail entries for the given root <paramref name="word"/>.
        /// </summary>
        /// <param name="word">The root to delete all entries for.</param>
        /// <returns>The count of entries removed.</returns>
        public int Remove(string word)
        {
            return WordList.Remove(_entriesByRoot, Affix, word);
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
            return WordList.Remove(_entriesByRoot, Affix, word, detail);
        }

        /// <summary>
        /// Builds a new <see cref="WordList"/> based on the values in this builder.
        /// </summary>
        /// <returns>A new <see cref="WordList"/>.</returns>
        public WordList Build() => BuildOrExtract(extract: false);

        /// <summary>
        /// Builds a new <see cref="WordList"/> based on the values in this builder.
        /// </summary>
        /// <remarks>
        /// This method can leave the builder in an invalid state
        /// but provides better performance for file reads.
        /// </remarks>
        /// <returns>A new <see cref="WordList"/>.</returns>
        public WordList Extract() => BuildOrExtract(extract: true);

        private WordList BuildOrExtract(bool extract)
        {
            TextDictionary<WordEntryDetail[]> entriesByRoot;
            if (extract)
            {
                entriesByRoot = _entriesByRoot;
                _entriesByRoot = [];
            }
            else
            {
                entriesByRoot = TextDictionary<WordEntryDetail[]>.Clone(_entriesByRoot, static v => [.. v]);
            }

            var allReplacements = Affix.Replacements;
            if (_phoneticReplacements is { Count: > 0 })
            {
                // store ph: field of a morphological description in reptable
                if (allReplacements.IsEmpty)
                {
                    allReplacements = new(_phoneticReplacements.MakeOrExtractArray(extract));
                }
                else if (extract)
                {
                    _phoneticReplacements.AddRange(allReplacements.RawArray);
                    allReplacements = new(_phoneticReplacements.Extract());
                }
                else
                {
                    allReplacements = SingleReplacementSet.Create(_phoneticReplacements.Concat(allReplacements));
                }
            }

            var nGramRestrictedFlags = FlagSet.Create(
            [
                Affix.ForbiddenWord,
                Affix.NoSuggest,
                Affix.NoNgramSuggest,
                Affix.OnlyInCompound,
                SpecialFlags.OnlyUpcaseFlag
            ]);

            return new WordList(
                Affix,
                entriesByRoot: entriesByRoot,
                nGramRestrictedFlags: nGramRestrictedFlags,
                allReplacements);
        }

        public void InitializeEntriesByRoot(int expectedSize)
        {
            if (expectedSize > 0)
            {
                // PERF: because we add more entries than we are told about, we add a bit more to the expected size
                var expectedCapacity = (expectedSize / 100) + expectedSize;
                _entriesByRoot.EnsureCapacity(expectedCapacity);
            }
        }
    }
}

