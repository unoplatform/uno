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

internal ref struct SpanSeparatorSplitEnumerator<T> where T : IEquatable<T>
{
    public delegate int FindNextSeparator(ReadOnlySpan<T> text);

    public SpanSeparatorSplitEnumerator(ReadOnlySpan<T> span, StringSplitOptions options, FindNextSeparator findNextSeparator)
    {
#if DEBUG
        if (options is not (StringSplitOptions.None or StringSplitOptions.RemoveEmptyEntries))
        {
            ExceptionEx.ThrowArgumentOutOfRange(nameof(options));
        }
#endif

        _findNextSeparator = findNextSeparator;
        _options = options;
        _span = span;
        _done = false;
    }

    private readonly FindNextSeparator _findNextSeparator;
    private readonly StringSplitOptions _options;
    private ReadOnlySpan<T> _span;
    private ReadOnlySpan<T> _current;
    private bool _done;

    public readonly ReadOnlySpan<T> Current => _current;

    public readonly SpanSeparatorSplitEnumerator<T> GetEnumerator() => this;

    public bool MoveNext()
    {
        if (_options == StringSplitOptions.RemoveEmptyEntries)
        {
            return MoveNextPartSkippingEmpty();
        }

        return MoveNextPart();
    }

    private bool MoveNextPartSkippingEmpty()
    {
        while (MoveNextPart())
        {
            if (_current.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool MoveNextPart()
    {
        if (_done)
        {
            return false;
        }

        var separatorIndex = _findNextSeparator(_span);
        if (separatorIndex >= 0)
        {
            _current = _span.Slice(0, separatorIndex);

            var nextStartIndex = separatorIndex + 1;
            _span = _span.Length > nextStartIndex ? _span.Slice(nextStartIndex) : [];
        }
        else
        {
            _current = _span;
            _done = true;
        }

        return true;
    }
}

