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

public sealed class QueryOptions
{
    /// <summary>
    /// The maximum depth of ß sharps to check.
    /// </summary>
    /// <remarks>
    /// The actual results returned may or may not actually be limited by this value.
    /// </remarks>
    public int MaxSharps { get; set; } = 5;

    /// <summary>
    /// The maximum number of compound suggestions to suggest.
    /// </summary>
    /// <remarks>
    /// The actual results returned may or may not actually be limited by this value.
    /// </remarks>
    public int MaxCompoundSuggestions { get; set; } = 3;

    /// <summary>
    /// The maximum number of suggestions to produce.
    /// </summary>
    /// <remarks>
    /// The actual results returned may or may not actually be limited by this value.
    /// </remarks>
    public int MaxSuggestions { get; set; } = 15;

    /// <remarks>
    /// The actual results returned may or may not actually be limited by this value.
    /// </remarks>
    public int MaxRoots { get; set; } = 100;

    /// <remarks>
    /// The actual results returned may or may not actually be limited by this value.
    /// </remarks>
    public int MaxWords { get; set; } = 100;

    /// <remarks>
    /// The actual results returned may or may not actually be limited by this value.
    /// </remarks>
    public int MaxGuess { get; set; } = 200;

    /// <remarks>
    /// The actual results returned may or may not actually be limited by this value.
    /// </remarks>
    public int MaxPhoneticSuggestions { get; set; } = 2;

    /// <remarks>
    /// The actual results returned may or may not actually be limited by this value.
    /// </remarks>
    public int MaxCharDistance { get; set; } = 4;

    /// <summary>
    /// This is the number of checks to be performed before considering time based cancellation.
    /// </summary>
    /// <remarks>
    /// I think the purpose of this is to ensure a minimum number of results in slower environments.
    /// </remarks>
    public int MinTimer { get; set; } = 100;

    public int MaxWordLen { get; set; } = 100;

    public int MaxPhoneTLen { get; set; } = 256;

    public int RecursiveDepthLimit { get; set; } = 0x3F00;

    /// <summary>
    /// The time limit for the individual steps during suggestion generation.
    /// </summary>
    /// <remarks>
    /// The default value is sourced from the TIMELIMIT variable in origin.
    /// </remarks>
    public TimeSpan TimeLimitSuggestStep { get; set; } = TimeSpan.FromMilliseconds(1000 / 20);

    /// <summary>
    /// The time limit for each compound suggestion iteration.
    /// </summary>
    /// <remarks>
    /// The default value is sourced from the TIMELIMIT_SUGGESTION variable in origin.
    /// </remarks>
    public TimeSpan TimeLimitCompoundSuggest { get; set; } = TimeSpan.FromMilliseconds(1000 / 10);

    /// <summary>
    /// The time limit for each compound word check operation.
    /// </summary>
    /// <remarks>
    /// The default value is sourced from the TIMELIMIT variable in origin.
    /// </remarks>
    public TimeSpan TimeLimitCompoundCheck { get; set; } = TimeSpan.FromMilliseconds(1000 / 20);

    /// <summary>
    /// A somewhat overall time limit for the suggestion algorithm.
    /// </summary>
    /// <remarks>
    /// The default value is sourced from the TIMELIMIT_GLOBAL variable in origin.
    /// </remarks>
    public TimeSpan TimeLimitSuggestGlobal { get; set; } = TimeSpan.FromMilliseconds(1000 / 4);
}

