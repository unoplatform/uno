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

[DebuggerDisplay("Options = {Options}, Flags = {Flags}, Morphs = {Morphs}")]
public readonly struct WordEntryDetail : IEquatable<WordEntryDetail>
{
    public static bool operator ==(WordEntryDetail a, WordEntryDetail b) => a.Equals(b);

    public static bool operator !=(WordEntryDetail a, WordEntryDetail b) => !a.Equals(b);

    public static WordEntryDetail Default { get; } = new WordEntryDetail(FlagSet.Empty, MorphSet.Empty, WordEntryOptions.None);

    public WordEntryDetail(FlagSet flags, MorphSet morphs, WordEntryOptions options)
    {
        Morphs = morphs;
        Flags = flags;
        Options = options;
    }

    public MorphSet Morphs { get; }

    public FlagSet Flags { get; }

    public WordEntryOptions Options { get; }

    public bool ContainsFlag(FlagValue flag) => Flags.Contains(flag);

    public bool ContainsAnyFlags(FlagSet flags) => Flags.ContainsAny(flags);

    public bool DoesNotContainFlag(FlagValue flag) => Flags.DoesNotContain(flag);

    public bool DoesNotContainAnyFlags(FlagSet flags) => Flags.DoesNotContainAny(flags);

    public bool Equals(WordEntryDetail other) =>
        other.Options == Options
        && other.Flags.Equals(Flags)
        && other.Morphs.Equals(Morphs);

    public override bool Equals(object? obj) => obj is WordEntryDetail entry && Equals(entry);

    public override int GetHashCode() => HashCode.Combine(Options, Flags, Morphs);

    internal WordEntry ToEntry(string word) => new(word, this);
}

