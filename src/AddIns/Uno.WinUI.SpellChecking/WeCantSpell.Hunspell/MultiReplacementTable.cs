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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WeCantSpell.Hunspell;

[DebuggerDisplay("Count = {Count}")]
public sealed class MultiReplacementTable : IReadOnlyDictionary<string, MultiReplacementEntry>
{
    public static readonly MultiReplacementTable Empty = TakeDictionary([]);

    public static MultiReplacementTable Create(Dictionary<string, MultiReplacementEntry>? replacements) =>
        replacements is null
            ? Empty
            : TakeDictionary(TextDictionary<MultiReplacementEntry>.MapFromDictionary(replacements));

    internal static MultiReplacementTable Create(TextDictionary<MultiReplacementEntry>? replacements) =>
        replacements is null
            ? Empty
            : TakeDictionary(TextDictionary<MultiReplacementEntry>.Clone(replacements));

    internal static MultiReplacementTable TakeDictionary(TextDictionary<MultiReplacementEntry>? replacements) =>
        replacements is null ? Empty : new MultiReplacementTable(replacements);

    private MultiReplacementTable(TextDictionary<MultiReplacementEntry> replacements)
    {
        _replacements = replacements;
    }

    private readonly TextDictionary<MultiReplacementEntry> _replacements;

    public MultiReplacementEntry this[string key] => _replacements[key];

    public int Count => _replacements.Count;

    public bool HasReplacements => _replacements.HasItems;

    public IEnumerable<string> Keys => _replacements.Keys;

    public IEnumerable<MultiReplacementEntry> Values => _replacements.Values;

    public IEnumerator<KeyValuePair<string, MultiReplacementEntry>> GetEnumerator() => _replacements.AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool ContainsKey(string key) => _replacements.ContainsKey(key);

    public bool TryGetValue(
        string key,
#if !NO_EXPOSED_NULLANNOTATIONS
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)]
#endif
        out MultiReplacementEntry value
    ) => _replacements.TryGetValue(key, out value);

    internal bool TryConvert(string text, out string converted)
    {
        if (text.Length > 0 && HasReplacements)
        {
            var appliedConversion = false;
            var convertedBuilder = new StringBuilderSpan(text.Length);

            for (var i = 0; i < text.Length; i++)
            {
                if (
                    FindLargestMatchingConversion(text.AsSpan(i)) is { } replacementEntry
                    && replacementEntry.ExtractReplacementText(text.Length - i, i == 0) is { Length: > 0 } replacementText)
                {
                    convertedBuilder.Append(replacementText);
                    i += replacementEntry.Pattern.Length - 1;
                    appliedConversion = true;
                }
                else
                {
                    convertedBuilder.Append(text[i]);
                }
            }

            if (appliedConversion)
            {
                converted = convertedBuilder.GetStringAndDispose();
                return true;
            }
            else
            {
                convertedBuilder.Dispose();
            }
        }

        converted = string.Empty;
        return false;
    }

    internal bool TryConvert(ReadOnlySpan<char> text, out string converted)
    {
        if (text.Length > 0 && HasReplacements)
        {
            var appliedConversion = false;
            var convertedBuilder = new StringBuilderSpan(text.Length);

            for (var i = 0; i < text.Length; i++)
            {
                if (
                    FindLargestMatchingConversion(text.Slice(i)) is { } replacementEntry
                    && replacementEntry.ExtractReplacementText(text.Length - i, i == 0) is { Length: > 0 } replacementText)
                {
                    convertedBuilder.Append(replacementText);

                    if (replacementEntry.Pattern.Length > 1)
                    {
                        i += replacementEntry.Pattern.Length - 1;
                    }

                    appliedConversion = true;
                }
                else
                {
                    convertedBuilder.Append(text[i]);
                }
            }

            if (appliedConversion)
            {
                converted = convertedBuilder.GetStringAndDispose();
                return true;
            }
            else
            {
                convertedBuilder.Dispose();
            }
        }

        converted = string.Empty;
        return false;
    }

    internal void ConvertAll(List<string> slst)
    {
        if (HasReplacements)
        {
            for (var j = 0; j < slst.Count; j++)
            {
                if (TryConvert(slst[j], out var wspace))
                {
                    slst[j] = wspace;
                }
            }
        }
    }

    /// <summary>
    /// Finds a conversion matching the longest version of the given <paramref name="text"/> from the left.
    /// </summary>
    /// <param name="text">The text to find a matching input conversion for.</param>
    /// <returns>The best matching input conversion.</returns>
    /// <seealso cref="MultiReplacementEntry"/>
    internal MultiReplacementEntry? FindLargestMatchingConversion(ReadOnlySpan<char> text)
    {
        for (var searchLength = text.Length; searchLength > 0; searchLength--)
        {
            if (_replacements.TryGetValue(text.Slice(0, searchLength), out var entry))
            {
                return entry;
            }
        }

        return null;
    }
}

