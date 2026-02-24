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
using System.Diagnostics;

namespace WeCantSpell.Hunspell;

[DebuggerDisplay("Key = {Key}, Conditions = {Conditions}")]
public abstract class AffixEntry
{
    protected AffixEntry(
        string strip,
        string affixText,
        CharacterConditionGroup conditions,
        MorphSet morph,
        FlagSet contClass,
        FlagValue aFlag,
        AffixEntryOptions options)
    {
        Strip = strip ?? string.Empty;
        Append = affixText ?? string.Empty;
        Conditions = conditions;
        MorphCode = morph;
        ContClass = contClass;
        AFlag = aFlag;
        Options = options;
    }

    /// <summary>
    /// Optional morphological fields separated by spaces or tabulators.
    /// </summary>
    public MorphSet MorphCode { get; }

    /// <summary>
    /// Text matching conditions that are to be met.
    /// </summary>
    public CharacterConditionGroup Conditions { get; }

    public FlagSet ContClass { get; }

    public FlagValue AFlag { get; }

    public AffixEntryOptions Options { get; }

    /// <summary>
    /// The affix string to add.
    /// </summary>
    /// <remarks>
    /// Affix (optionally with flags of continuation classes, separated by a slash).
    /// </remarks>
    public string Append { get; }

    /// <summary>
    /// String to strip before adding affix.
    /// </summary>
    /// <remarks>
    /// Stripping characters from beginning (at prefix rules) or
    /// end(at suffix rules) of the word.
    /// </remarks>
    public string Strip { get; }

    public abstract string Key { get; }

    public abstract bool IsKeySubset(string s2);

    public abstract bool IsKeySubset(ReadOnlySpan<char> s2);

    public abstract bool IsWordSubset(string word);

    public abstract bool IsWordSubset(ReadOnlySpan<char> word);

    internal abstract bool TestCondition(ReadOnlySpan<char> word);

    public bool ContainsContClass(FlagValue flag) => ContClass.Contains(flag);

    public bool ContainsAnyContClass(FlagSet flags) => ContClass.ContainsAny(flags);
}

[DebuggerDisplay("Key = {Key}, Conditions = {Conditions}")]
public sealed class PrefixEntry : AffixEntry
{
    public PrefixEntry(
        string strip,
        string affixText,
        CharacterConditionGroup conditions,
        MorphSet morph,
        FlagSet contClass,
        FlagValue aFlag,
        AffixEntryOptions options)
        : base (strip, affixText, conditions, morph, contClass, aFlag, options)
    {
    }

    public override string Key => Append;

    public override bool IsKeySubset(string s2) => StringEx.IsSubset(Append, s2);

    public override bool IsKeySubset(ReadOnlySpan<char> s2) => StringEx.IsSubset(Append, s2);

    public override bool IsWordSubset(string word) => StringEx.IsSubset(Append, word);

    public override bool IsWordSubset(ReadOnlySpan<char> word) => StringEx.IsSubset(Append, word);

    internal override bool TestCondition(ReadOnlySpan<char> word) => Conditions.IsStartingMatch(word);
}

[DebuggerDisplay("Key = {Key}, Conditions = {Conditions}")]
public sealed class SuffixEntry : AffixEntry
{
    public SuffixEntry(
        string strip,
        string affixText,
        CharacterConditionGroup conditions,
        MorphSet morph,
        FlagSet contClass,
        FlagValue aFlag,
        AffixEntryOptions options)
        : base(strip, affixText, conditions, morph, contClass, aFlag, options)
    {
        _key = Append.GetReversed();
    }

    private readonly string _key;

    public override string Key => _key;

    public override bool IsKeySubset(string s2) => s2 is not null && StringEx.IsSubset(_key, s2);

    public override bool IsKeySubset(ReadOnlySpan<char> s2) => StringEx.IsSubset(_key, s2);

    public override bool IsWordSubset(string word)
    {
        return word is not null
            && Append.Length <= word.Length
            && StringEx.IsSubset(Append, word.AsSpan(word.Length - Append.Length));
    }

    public override bool IsWordSubset(ReadOnlySpan<char> word)
    {
        return Append.Length <= word.Length
            && StringEx.IsSubset(Append, word.Slice(word.Length - Append.Length));
    }

    internal override bool TestCondition(ReadOnlySpan<char> word) => Conditions.IsEndingMatch(word);
}

