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
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WeCantSpell.Hunspell;

[DebuggerDisplay("BufferLength = {BufferLength}, Text = {ToString()}")]
internal struct SimulatedCString
{
    public SimulatedCString(int capacity)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentLessThan(capacity, 0, nameof(capacity));
#endif
        _rawBuffer = ArrayPool<char>.Shared.Rent(capacity);
        _bufferLength = capacity;
        _terminatedLength = 0;
    }

    public SimulatedCString(ReadOnlySpan<char> text)
    {
        _rawBuffer = ArrayPool<char>.Shared.Rent(text.Length + 3); // 3 extra characters seems to be enough to prevent most reallocations
        text.CopyTo(_rawBuffer);
        _bufferLength = text.Length;
        _terminatedLength = -1;
    }

    private char[] _rawBuffer;
    private int _bufferLength;
    private int _terminatedLength;

    public readonly int BufferLength => _bufferLength;

    public char this[int index]
    {
        readonly get
        {
#if DEBUG
            ExceptionEx.ThrowIfArgumentLessThan(index, 0, nameof(index));
            ExceptionEx.ThrowIfArgumentGreaterThan(index, _bufferLength, nameof(index)); // Allow access to the virtual or real null terminator
#endif
            return index < _bufferLength ? _rawBuffer[index] : '\0';
        }

        set
        {
#if DEBUG
            ExceptionEx.ThrowIfArgumentLessThan(index, 0, nameof(index));
            ExceptionEx.ThrowIfArgumentGreaterThanOrEqual(index, _bufferLength, nameof(index));
#endif
            _rawBuffer[index] = value;

            if (value == '\0')
            {
                if (index == 0 || index < _terminatedLength)
                {
                    _terminatedLength = index;
                }
            }
            else if (index == _terminatedLength)
            {
                _terminatedLength = -1;
            }
        }
    }

    public ReadOnlySpan<char> TerminatedSpan
    {
        get
        {
            if (_terminatedLength < 0)
            {
                _terminatedLength = Array.IndexOf(_rawBuffer, '\0', 0, _bufferLength);

                if (_terminatedLength < 0)
                {
                    _terminatedLength = _bufferLength;
                }
            }

            return _rawBuffer.AsSpan(0, _terminatedLength);
        }
    }

    public readonly ReadOnlySpan<char> SliceToTerminatorFromOffset(int startIndex)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentLessThan(startIndex, 0, nameof(startIndex));
        ExceptionEx.ThrowIfArgumentGreaterThan(startIndex, _bufferLength, nameof(startIndex));
#endif

        var result = _rawBuffer.AsSpan(startIndex, _bufferLength - startIndex);
        var index = result.IndexOf('\0');
        return index >= 0 ? result.Slice(0, index) : result;
    }

    public char ExchangeWithNull(int index)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentLessThan(index, 0, nameof(index));
        ExceptionEx.ThrowIfArgumentGreaterThanOrEqual(index, _bufferLength, nameof(index));
#endif

        if (index == 0 || index < _terminatedLength)
        {
            _terminatedLength = index;
        }

        return performExchange(ref _rawBuffer[index]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char performExchange(ref char target)
        {
            var previous = target;
            target = '\0';
            return previous;
        }
    }

    public void WriteChars(ReadOnlySpan<char> text, int destinationIndex)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentLessThan(destinationIndex, 0, nameof(destinationIndex));
        ExceptionEx.ThrowIfArgumentGreaterThan(destinationIndex, _bufferLength, nameof(destinationIndex));
#endif

        EnsureBufferCapacity(text.Length + destinationIndex);

        text.CopyTo(_rawBuffer.AsSpan(destinationIndex));

        if (destinationIndex <= _terminatedLength)
        {
            _terminatedLength = -1;
        }
    }

    public void Assign(ReadOnlySpan<char> text)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentGreaterThan(text.Length, _bufferLength, nameof(text));
#endif

        var buffer = _rawBuffer.AsSpan(0, _bufferLength);
        text.CopyTo(buffer);

        if (text.Length < buffer.Length)
        {
            buffer.Slice(text.Length).Clear();
        }

        _terminatedLength = -1;
    }

    public void RemoveRange(int startIndex, int count)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentLessThan(startIndex, 0, nameof(startIndex));
        ExceptionEx.ThrowIfArgumentGreaterThan(startIndex + count, _bufferLength, nameof(count));
#endif

        if (count > 0)
        {
            if (_terminatedLength >= startIndex)
            {
                _terminatedLength = -1;
            }

            var buffer = _rawBuffer.AsSpan(0, _bufferLength);
            buffer.Slice(startIndex + count).CopyTo(buffer.Slice(startIndex)); // shift the leftovers backwards
            buffer.Slice(buffer.Length - count).Clear(); // zero the freed space at the end
        }
    }

    public override string ToString() => TerminatedSpan.ToString();

    public void Dispose()
    {
        if (_rawBuffer.Length != 0)
        {
            ArrayPool<char>.Shared.Return(_rawBuffer);
            _rawBuffer = [];
        }
    }

    private void EnsureBufferCapacity(int neededLength)
    {
        if (_bufferLength < neededLength)
        {
            if (_rawBuffer.Length < neededLength)
            {
                var newBuffer = ArrayPool<char>.Shared.Rent(neededLength);
                Array.Copy(_rawBuffer, newBuffer, _bufferLength);

                if (_rawBuffer.Length != 0)
                {
                    ArrayPool<char>.Shared.Return(_rawBuffer);
                }

                _rawBuffer = newBuffer;
            }

            _bufferLength = neededLength;
        }
    }
}

