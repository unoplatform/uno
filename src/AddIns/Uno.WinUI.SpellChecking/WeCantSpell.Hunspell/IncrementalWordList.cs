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
using System.Diagnostics;
using System.Threading;

namespace WeCantSpell.Hunspell;

[DebuggerDisplay("WNum = {WNum}")]
internal sealed class IncrementalWordList
{
    private const int MaxCachedCapacity = 32;

    private static IncrementalWordList? PoolCache;

    public static IncrementalWordList GetRoot()
    {
        if (Interlocked.Exchange(ref PoolCache, null) is { } rental)
        {
#if DEBUG
            if (rental.WNum != 0) ExceptionEx.ThrowInvalidOperation();
#endif
            rental._words.Clear();
        }
        else
        {
            rental = new();
        }

        return rental;
    }

    public static void ReturnRoot(ref IncrementalWordList? rental)
    {
        if (rental is { _words.Capacity: > 0 and <= MaxCachedCapacity })
        {
#if DEBUG
            if (rental.WNum != 0) ExceptionEx.ThrowInvalidOperation();
#endif

            Volatile.Write(ref PoolCache, rental);
        }

        rental = null;
    }

    private IncrementalWordList() : this([], 0)
    {
    }

    private IncrementalWordList(List<WordEntryDetail?> words, int wNum)
    {
        _words = words;
        WNum = wNum;
    }

    private readonly List<WordEntryDetail?> _words;

    public readonly int WNum;

    public void SetCurrent(in WordEntryDetail value)
    {
        if (WNum < _words.Count)
        {
            _words[WNum] = value;
        }
        else
        {
            while (WNum > _words.Count)
            {
                _words.Add(null);
            }

            _words.Add(value);
        }
    }

    public void ClearCurrent()
    {
        if (WNum < _words.Count)
        {
            _words[WNum] = null;
        }
    }

    public bool CheckIfCurrentIsNotNull() => CheckIfNotNull(WNum);

    public bool CheckIfNextIsNotNull() => CheckIfNotNull(WNum + 1);

    private bool CheckIfNotNull(int index) => index < _words.Count && _words[index] is not null;

    public bool ContainsFlagAt(int index, FlagValue flag)
    {
        return index < _words.Count
            && _words[index] is { } word
            && word.ContainsFlag(flag);
    }

    public IncrementalWordList CreateIncremented() => new(_words, WNum + 1);
}

