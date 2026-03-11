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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace WeCantSpell.Hunspell;

[DebuggerDisplay("Length = {Length}, Text = {ToString()}")]
internal ref struct StringBuilderSpan
{
    public StringBuilderSpan(int capacity)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentLessThan(capacity, 0, nameof(capacity));
#endif

        _bufferRental = ArrayPool<char>.Shared.Rent(capacity);
        _bufferSpan = _bufferRental.AsSpan();
        _length = 0;
    }

    public StringBuilderSpan(scoped ReadOnlySpan<char> text)
    {
        _bufferRental = ArrayPool<char>.Shared.Rent(text.Length);
        _bufferSpan = _bufferRental.AsSpan();
        _length = text.Length;
        text.CopyTo(_bufferSpan);
    }

#if NO_STRING_SPAN

    public StringBuilderSpan(string text) : this(text is null ? [] : text.AsSpan())
    {
    }

#else

    public StringBuilderSpan(string text)
    {
        if (text is not { Length: > 0 })
        {
            _bufferRental = [];
            _bufferSpan = [];
            _length = 0;
        }
        else
        {
            _bufferRental = ArrayPool<char>.Shared.Rent(text.Length);
            _bufferSpan = _bufferRental.AsSpan();
            _length = text.Length;
            text.CopyTo(_bufferSpan);
        }
    }

#endif

    private char[] _bufferRental;
    private Span<char> _bufferSpan;
    private int _length;

    public readonly int Length => _length;

    public readonly char this[int index]
    {
        get
        {
#if DEBUG
            ExceptionEx.ThrowIfArgumentLessThan(index, 0, nameof(index));
            ExceptionEx.ThrowIfArgumentGreaterThanOrEqual(index, _length, nameof(index));
#endif

            return _bufferSpan[index];
        }

        set
        {
#if DEBUG
            ExceptionEx.ThrowIfArgumentLessThan(index, 0, nameof(index));
            ExceptionEx.ThrowIfArgumentGreaterThanOrEqual(index, _length, nameof(index));
#endif

            _bufferSpan[index] = value;
        }
    }

    internal readonly Span<char> CurrentSpan => _bufferSpan.Slice(0, _length);

#if NO_STRING_SPAN

    public void Set(string value)
    {
        if (value is { Length: > 0 })
        {
            Set(value.AsSpan());
        }
        else
        {
            _length = 0;
        }
    }

#else

    public void Set(string value)
    {
        if (value is { Length: > 0 })
        {
            if (_bufferSpan.Length < value.Length)
            {
                ResetAndReallocateBuffer(value.Length);
            }

            value.CopyTo(_bufferSpan);
            _length = value.Length;
        }
        else
        {
            _length = 0;
        }
    }

#endif

    public void Set(scoped ReadOnlySpan<char> value)
    {
        if (value.Length > 0)
        {
            if (_bufferSpan.Length < value.Length)
            {
                ResetAndReallocateBuffer(value.Length);
            }

            value.CopyTo(_bufferSpan);
            _length = value.Length;
        }
        else
        {
            _length = 0;
        }
    }

    public void Append(string value)
    {
        if (value is { Length: > 0 })
        {
#if NO_STRING_SPAN
            Append(value.AsSpan());
#else
            if (value.Length + _length > _bufferSpan.Length)
            {
                GrowBufferToCapacity(value.Length + _length);
            }

            value.CopyTo(_bufferSpan.Slice(_length));
            _length += value.Length;
#endif
        }
    }

    public void Append(scoped ReadOnlySpan<char> value)
    {
        if (value.Length > 0)
        {
            if (value.Length + _length > _bufferSpan.Length)
            {
                GrowBufferToCapacity(value.Length + _length);
            }

            value.CopyTo(_bufferSpan.Slice(_length));
            _length += value.Length;
        }
    }

    public void Append(IEnumerable<char> values)
    {
        var expectedCount = values.GetNonEnumeratedCountOrDefault();
        if (_length + expectedCount > _bufferSpan.Length)
        {
            GrowBufferToCapacity(_length + expectedCount);
        }

        foreach (var value in values)
        {
            Append(value);
        }
    }

    public void Append(char value)
    {
        if (_length + 1 > _bufferSpan.Length)
        {
            GrowBufferToCapacity(_length + 1);
        }

        _bufferSpan[_length] = value;
        _length++;
    }

    public void AppendIfNotNull(char value)
    {
        if (value != default)
        {
            Append(value);
        }
    }

    public void AppendLower(scoped ReadOnlySpan<char> value, CultureInfo cultureInfo)
    {
        var space = AppendSpaceForImmediateWrite(value.Length);
        var written = value.ToLower(space, cultureInfo);
        if (written < 0)
        {
            Append(value.ToString().ToLower(cultureInfo));
        }
        else if (written != space.Length)
        {
            ExceptionEx.ThrowInvalidOperation("Failed to change case safely");
        }
    }

    public void AppendReversed(scoped ReadOnlySpan<char> value)
    {
        var space = AppendSpaceForImmediateWrite(value.Length);
        value.CopyToReversed(space);
    }

    public readonly void Replace(char oldChar, char newChar)
    {
        Replace(oldChar, newChar, 0, _length);
    }

    public readonly void Replace(char oldChar, char newChar, int startIndex)
    {
        Replace(oldChar, newChar, startIndex, _length - startIndex);
    }

    public readonly void Replace(char oldChar, char newChar, int startIndex, int count)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentGreaterThanOrEqual(startIndex, _length, nameof(startIndex));
        ExceptionEx.ThrowIfArgumentGreaterThan(startIndex + count, _length, nameof(count));
#endif

        _bufferSpan.Slice(startIndex, count).Replace(oldChar, newChar);
    }

    public void Replace(scoped ReadOnlySpan<char> oldText, scoped ReadOnlySpan<char> newText)
    {
        Replace(oldText, newText, 0, _length);
    }

    public void Replace(scoped ReadOnlySpan<char> oldText, scoped ReadOnlySpan<char> newText, int startIndex)
    {
        Replace(oldText, newText, startIndex, _length - startIndex);
    }

    public void Replace(scoped ReadOnlySpan<char> oldText, scoped ReadOnlySpan<char> newText, int startIndex, int count)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentGreaterThanOrEqual(startIndex, _length, nameof(startIndex));
        ExceptionEx.ThrowIfArgumentGreaterThan(startIndex + count, _length, nameof(count));
#endif

        if (_length != 0 && count != 0 && !oldText.IsEmpty)
        {
            if (oldText.Length == newText.Length)
            {
                ReplaceEqualSizeInternal(oldText, newText, startIndex, count);
            }
            else if (oldText.Length < newText.Length)
            {
                ReplaceIncreasingSizeInternal(oldText, newText, startIndex, count);
            }
            else
            {
                ReplaceDecreasingSizeInternal(oldText, newText, startIndex, count);
            }
        }
    }

    private readonly void ReplaceEqualSizeInternal(scoped ReadOnlySpan<char> oldText, scoped ReadOnlySpan<char> newText, int startIndex, int count)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentEqual(count, 0, nameof(count));
        ExceptionEx.ThrowIfArgumentEmpty(oldText, nameof(oldText));
        ExceptionEx.ThrowIfArgumentNotEqual(newText.Length, oldText.Length, nameof(newText));
        if (_length == 0) ExceptionEx.ThrowInvalidOperation();
#endif

        var editableArea = _bufferSpan.Slice(startIndex, count);

        do
        {
            if (editableArea.IsEmpty)
            {
                return;
            }

            var searchIndex = editableArea.IndexOf(oldText);
            if (searchIndex < 0)
            {
                return;
            }

            if (searchIndex > 0)
            {
                editableArea = editableArea.Slice(searchIndex);
            }

            newText.CopyTo(editableArea);

            editableArea = editableArea.Slice(newText.Length);
        }
        while (true);
    }

    private void ReplaceIncreasingSizeInternal(scoped ReadOnlySpan<char> oldText, scoped ReadOnlySpan<char> newText, int startIndex, int count)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentEqual(count, 0, nameof(count));
        ExceptionEx.ThrowIfArgumentEmpty(oldText, nameof(oldText));
        ExceptionEx.ThrowIfArgumentLessThanOrEqual(newText.Length, oldText.Length, nameof(newText));
        if (_length == 0) ExceptionEx.ThrowInvalidOperation();
#endif

        var searchIgnoreSize = _length - startIndex - count;
        var replacementGrowthSize = newText.Length - oldText.Length;

        do
        {
            if (_length - searchIgnoreSize <= startIndex)
            {
                return;
            }

            var searchIndex = _bufferSpan.Slice(startIndex, _length - startIndex - searchIgnoreSize).IndexOf(oldText);
            if (searchIndex < 0)
            {
                return;
            }

            if (_length + replacementGrowthSize > _bufferSpan.Length)
            {
                GrowBufferToCapacity(_length + replacementGrowthSize);
            }

            var editableArea = _bufferSpan.Slice(startIndex + searchIndex);

            editableArea.Slice(oldText.Length, _length - startIndex - searchIndex - oldText.Length).CopyTo(editableArea.Slice(newText.Length));
            newText.CopyTo(editableArea);

            _length += replacementGrowthSize;

            startIndex += searchIndex + newText.Length;
        }
        while (true);
    }

    private void ReplaceDecreasingSizeInternal(scoped ReadOnlySpan<char> oldText, scoped ReadOnlySpan<char> newText, int startIndex, int count)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentEqual(count, 0, nameof(count));
        ExceptionEx.ThrowIfArgumentEmpty(oldText, nameof(oldText));
        ExceptionEx.ThrowIfArgumentGreaterThanOrEqual(newText.Length, oldText.Length, nameof(newText));
        if (_length == 0) ExceptionEx.ThrowInvalidOperation();
#endif

        var searchIgnoreSize = _length - startIndex - count;

        do
        {
            if (_length - searchIgnoreSize <= startIndex)
            {
                return;
            }

            var editableArea = _bufferSpan.Slice(startIndex, _length - startIndex);

            var searchIndex = editableArea.Slice(0, editableArea.Length - searchIgnoreSize).IndexOf(oldText);
            if (searchIndex < 0)
            {
                return;
            }

            if (searchIndex > 0)
            {
                editableArea = editableArea.Slice(searchIndex);
            }

            newText.CopyTo(editableArea);
            editableArea.Slice(oldText.Length).CopyTo(editableArea.Slice(newText.Length));

            _length -= oldText.Length - newText.Length;
            startIndex += searchIndex + newText.Length;
        }
        while (true);
    }

    public void RemoveAt(int index)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentLessThan(index, 0, nameof(index));
        ExceptionEx.ThrowIfArgumentGreaterThanOrEqual(index, _length, nameof(index));
#endif

        if ((index + 1) < _length)
        {
            _bufferSpan.Slice(index + 1, _length - index - 1).CopyTo(_bufferSpan.Slice(index));
        }

        _length--;
    }

    public void Remove(int startIndex, int count)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentLessThan(startIndex, 0, nameof(startIndex));
        ExceptionEx.ThrowIfArgumentGreaterThanOrEqual(startIndex, _length, nameof(startIndex));
        ExceptionEx.ThrowIfArgumentLessThan(count, 0, nameof(count));
        ExceptionEx.ThrowIfArgumentGreaterThan(startIndex + count, _length, nameof(count));
#endif

        if (count == 1)
        {
            RemoveAt(startIndex);
        }
        else if (count > 1)
        {
            RemoveRangeInternal(startIndex, count);
        }
    }

    private void RemoveRangeInternal(int startIndex, int count)
    {
        var endIndex = startIndex + count;
        if (_length > endIndex)
        {
            _bufferSpan.Slice(endIndex, _length - endIndex).CopyTo(_bufferSpan.Slice(startIndex));
        }

        _length -= count;
    }

    public void RemoveAll(char value)
    {
        var index = _bufferSpan.Slice(0, _length).IndexOf(value);
        while (index >= 0 && index < _length)
        {
            RemoveAt(index);
            index = ((ReadOnlySpan<char>)_bufferSpan.Slice(0, _length)).IndexOf(value, index);
        }
    }

    public void Insert(int index, char value)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentLessThan(index, 0, nameof(index));
        ExceptionEx.ThrowIfArgumentGreaterThan(index, _length + 1, nameof(index));
#endif

        if (_length + 1 > _bufferSpan.Length)
        {
            GrowBufferToCapacity(_length + 1);
        }

        if (index < _length)
        {
            _bufferSpan.Slice(index, _length - index).CopyTo(_bufferSpan.Slice(index + 1));
        }

        _bufferSpan[index] = value;
        _length++;
    }

    public readonly void Sort()
    {
#if NO_SPAN_SORT
        Array.Sort(_bufferRental, 0, _length);
#else
        CurrentSpan.Sort();
#endif
    }

    public void RemoveAdjacentDuplicates()
    {
        if (_length < 2)
        {
            return;
        }

        var i = 1;
        do
        {
            if (_bufferSpan[i - 1] == _bufferSpan[i])
            {
                _bufferSpan.Slice(i + 1, _length - i - 1).CopyTo(_bufferSpan.Slice(i));
                _length--;
            }
            else
            {
                i++;
            }
        }
        while (i < _length);
    }

    public string ToStringInitCap(TextInfo textInfo)
    {
        if (_length == 0)
        {
            return string.Empty;
        }

        var backup = _bufferSpan[0];
        var initial = textInfo.ToUpper(backup);

        string result;
        if (initial == backup)
        {
            result = ToString();
        }
        else
        {
            _bufferSpan[0] = initial;
            result = ToString();
            _bufferSpan[0] = backup;
        }

        return result;
    }

    public override readonly string ToString() => _length > 0 ? _bufferSpan.Slice(0, _length).ToString() : string.Empty;

    public readonly bool EndsWith(char value) => _length > 0 && _bufferSpan[_length - 1] == value;

    public readonly bool Contains(char value) => CurrentSpan.Contains(value);

    public string GetStringAndDispose()
    {
        var result = ToString();
        Dispose();
        return result;
    }

    public void Dispose()
    {
        var toReturn = _bufferRental;

        this = default;

        if (toReturn is { Length: not 0 })
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    private Span<char> AppendSpaceForImmediateWrite(int size)
    {
        var newSize = _length + size;
        if (_bufferSpan.Length < newSize)
        {
            GrowBufferToCapacity(newSize);
        }

        var newSpace = _bufferSpan.Slice(_length, size);

        _length = newSize;

        return newSpace;
    }

    private void GrowBufferToCapacity(int capacity)
    {
#if DEBUG
        ExceptionEx.ThrowIfArgumentLessThanOrEqual(capacity, _bufferSpan.Length, nameof(capacity));
#endif

        var toReturn = _bufferRental;
        var newBuffer = ArrayPool<char>.Shared.Rent(capacity);
        var newSpan = newBuffer.AsSpan();

        _bufferSpan.Slice(0, _length).CopyTo(newSpan);
        _bufferRental = newBuffer;
        _bufferSpan = newSpan;

        if (toReturn.Length != 0)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    private void ResetAndReallocateBuffer(int capacity)
    {
        if (_bufferRental.Length != 0)
        {
            ArrayPool<char>.Shared.Return(_bufferRental);
        }

        _bufferRental = ArrayPool<char>.Shared.Rent(capacity);
        _bufferSpan = _bufferRental.AsSpan();
        _length = 0;
    }
}

