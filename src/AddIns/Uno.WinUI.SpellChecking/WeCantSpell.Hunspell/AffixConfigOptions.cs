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

namespace WeCantSpell.Hunspell;

/// <summary>
/// Various options that can be enabled as part of an <seealso cref="AffixConfig"/>.
/// </summary>
/// <seealso cref="AffixConfig"/>
[Flags]
public enum AffixConfigOptions : short
{
    /// <summary>
    /// Indicates that no options are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates agglutinative languages with right-to-left writing system.
    /// </summary>
    /// <remarks>
    /// Set twofold prefix stripping (but single suffix stripping) eg. for morphologically complex
    /// languages with right-to-left writing system.
    /// </remarks>
    ComplexPrefixes = 1 << 0,

    /// <summary>
    /// Allow twofold suffixes within compounds.
    /// </summary>
    CompoundMoreSuffixes = 1 << 1,

    /// <summary>
    /// Forbid word duplication in compounds (e.g. foofoo).
    /// </summary>
    CheckCompoundDup = 1 << 2,

    /// <summary>
    /// Forbid compounding if the compound word may be a non compound word with a REP fault.
    /// </summary>
    /// <remarks>
    /// Forbid compounding, if the (usually bad) compound word may be
    /// a non compound word with a REP fault. Useful for languages with
    /// 'compound friendly' orthography.
    /// </remarks>
    CheckCompoundRep = 1 << 3,

    /// <summary>
    /// Forbid compounding if the compound word contains triple repeating letters.
    /// </summary>
    /// <remarks>
    /// Forbid compounding, if compound word contains triple repeating letters
    /// (e.g.foo|ox or xo|oof). Bug: missing multi-byte character support
    /// in UTF-8 encoding(works only for 7-bit ASCII characters).
    /// </remarks>
    CheckCompoundTriple = 1 << 4,

    /// <summary>
    /// Allow simplified 2-letter forms of the compounds forbidden by <see cref="CheckCompoundTriple"/>.
    /// </summary>
    /// <remarks>
    /// It's useful for Swedish and Norwegian (and for
    /// the old German orthography: Schiff|fahrt -> Schiffahrt).
    /// </remarks>
    SimplifiedTriple = 1 << 5,

    /// <summary>
    /// Forbid upper case characters at word boundaries in compounds.
    /// </summary>
    CheckCompoundCase = 1 << 6,

    /// <summary>
    /// A flag used by the controlled compound words.
    /// </summary>
    CheckNum = 1 << 7,

    /// <summary>
    /// Remove all bad n-gram suggestions (default mode keeps one).
    /// </summary>
    /// <seealso cref="AffixConfig.MaxDifferency"/>
    OnlyMaxDiff = 1 << 8,

    /// <summary>
    /// Disable word suggestions with spaces.
    /// </summary>
    NoSplitSuggestions = 1 << 9,

    /// <summary>
    /// Indicates that affix rules can strip full words.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When active, affix rules can strip full words, not only one less characters, before
    /// adding the affixes, see fullstrip.* test files in the source distribution).
    /// </para>
    /// <para>
    /// Note: conditions may be word length without <see cref="FullStrip"/>, too.
    /// </para>
    /// </remarks>
    FullStrip = 1 << 10,

    /// <summary>
    /// Add dot(s) to suggestions, if input word terminates in dot(s).
    /// </summary>
    /// <remarks>
    /// Not for LibreOffice dictionaries, because LibreOffice
    /// has an automatic dot expansion mechanism.
    /// </remarks>
    SuggestWithDots = 1 << 11,

    /// <summary>
    /// When active, words marked with the <see cref="AffixConfig.Warn"/> flag aren't accepted by the spell checker.
    /// </summary>
    /// <remarks>
    /// Words with flag <see cref="AffixConfig.Warn"/> aren't accepted by the spell checker using this parameter.
    /// </remarks>
    ForbidWarn = 1 << 12,

    /// <summary>
    /// Indicates SS letter pair in uppercased (German) words may be upper case sharp s (ß).
    /// </summary>
    /// <remarks>
    /// SS letter pair in uppercased (German) words may be upper case sharp s (ß).
    /// Hunspell can handle this special casing with the CHECKSHARPS
    /// declaration (see also <see cref="AffixConfig.KeepCase"/> flag and tests/germancompounding example)
    /// in both spelling and suggestion.
    /// </remarks>
    /// <seealso cref="AffixConfig.KeepCase"/>
    CheckSharps = 1 << 13,

    SimplifiedCompound = 1 << 14
}

