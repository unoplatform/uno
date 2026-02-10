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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeCantSpell.Hunspell;

internal sealed class LineReader : IDisposable
{
    private const char CharacterCr = '\r';
    private const char CharacterLf = '\n';
    private const int DefaultBufferSize = 4096 * 2;
    private static readonly Encoding[] PreambleEncodings;

    static LineReader()
    {
        // NOTE: the order of these encodings must be preserved
        PreambleEncodings =
        [
            new UnicodeEncoding(true, true),
            new UnicodeEncoding(false, true),
            new UTF32Encoding(false, true),
            Encoding.UTF8,
            new UTF32Encoding(true, true)
        ];
    }

    internal LineReader(Stream stream, Encoding encoding, bool allowEncodingChanges = false, bool ownsStream = false)
    {
        _stream = stream;
        _encoding = encoding;
        _decoder = encoding.GetDecoder();
        _allowEncodingChanges = allowEncodingChanges;
        _reusableFileReadBuffer = new byte[DefaultBufferSize];
        _buffers = new(2);
        _ownsStream = ownsStream;
    }

    private readonly Stream _stream;
    private readonly byte[] _reusableFileReadBuffer;
    private readonly List<TextBufferLine> _buffers;
    private Encoding _encoding;
    private Decoder _decoder;
    private IMemoryOwner<char>? _tempJoinBuffer = null;
    private ReadOnlyMemory<char> _current = ReadOnlyMemory<char>.Empty;
    private BufferPosition _position = default;
    private bool _allowEncodingChanges;
    private bool _hasReadPreamble = false;
    private readonly bool _ownsStream;

    public ReadOnlySpan<char> CurrentSpan => _current.Span;

    public bool ReadNext()
    {
        var lineTerminalPosition = ScanForwardToNextLineBreak();

        while (!lineTerminalPosition.HasValue)
        {
            var newBufferLines = ReadNewTextBufferLines();
            if (newBufferLines == 0)
            {
                break;
            }

            lineTerminalPosition = ScanForwardToNextLineBreak(_buffers.Count - newBufferLines, 0);
        }

        return UpdateAfterRead(lineTerminalPosition ?? GetTerminalBufferPosition());
    }

#if NO_VALUETASK
    public Task<bool> ReadNextAsync(CancellationToken ct)
    {
        if (ScanForwardToNextLineBreak() is { } lineTerminalPosition)
        {
            return Task.FromResult(UpdateAfterRead(lineTerminalPosition));
        }
        else
        {
            return ReadNextAsyncWithReads(ct);
        }
    }
#else
    public ValueTask<bool> ReadNextAsync(CancellationToken ct)
    {
        if (ScanForwardToNextLineBreak() is { } lineTerminalPosition)
        {
            return ValueTask.FromResult(UpdateAfterRead(lineTerminalPosition));
        }
        else
        {
            return ReadNextAsyncWithReads(ct);
        }
    }
#endif

#if NO_VALUETASK
    private async Task<bool> ReadNextAsyncWithReads(CancellationToken ct)
#else
    private async ValueTask<bool> ReadNextAsyncWithReads(CancellationToken ct)
#endif
    {
        BufferPosition? lineTerminalPosition = null;
        do
        {
            var newBufferLines = await ReadNewTextBufferLinesAsync(ct).ConfigureAwait(false);
            if (newBufferLines == 0)
            {
                break;
            }

            lineTerminalPosition = ScanForwardToNextLineBreak(_buffers.Count - newBufferLines, 0);
        }
        while (!lineTerminalPosition.HasValue);

        return UpdateAfterRead(lineTerminalPosition ?? GetTerminalBufferPosition());
    }

    private bool UpdateAfterRead(BufferPosition lineTerminalPosition)
    {
        if (_position.BufferIndex >= _buffers.Count)
        {
            return false;
        }

        _tempJoinBuffer?.Dispose();
        _tempJoinBuffer = null;

        TextBufferLine buffer;
        if (_position.BufferIndex == lineTerminalPosition.BufferIndex)
        {
            buffer = _buffers[_position.BufferIndex];
            _current = buffer.AsMemory(_position.SubIndex, lineTerminalPosition.SubIndex - _position.SubIndex);
        }
        else
        {
            buffer = UpdateAfterReadWithJoinInternal(lineTerminalPosition);
        }

        if (_current.EndsWith(CharacterCr))
        {
            _current = _current.Slice(0, _current.Length - 1);
        }

        _position = lineTerminalPosition;
        _position.SubIndex++;

        if (_position.SubIndex >= buffer.Length)
        {
            _position.BufferIndex++;
            _position.SubIndex = 0;
        }

        if (_allowEncodingChanges)
        {
            UpdateAfterRead_TryChangeEncoding();
        }

        return true;
    }

    private void UpdateAfterRead_TryChangeEncoding()
    {
        var encodingSpan = readSetEncoding(_current.Span);
        if (!encodingSpan.IsEmpty)
        {
            ChangeEncoding(encodingSpan);
            _allowEncodingChanges = false; // Only expect one encoding switch per file
        }

        static ReadOnlySpan<char> readSetEncoding(ReadOnlySpan<char> span)
        {
            var startIndex = 0;
            for (; startIndex < span.Length && span[startIndex].IsTabOrSpace(); startIndex++) ;

            if (startIndex > 0)
            {
                span = span.Slice(startIndex);
            }

            if (span.Length >= 5
                && span[0] is 'S' or 's'
                && span[1] is 'E' or 'e'
                && span[2] is 'T' or 't'
                && span[3].IsTabOrSpace())
            {
                for (startIndex = 4; startIndex < span.Length && span[startIndex].IsTabOrSpace(); startIndex++) ;

                var endIndex = span.Length - 1;
                for (; endIndex > startIndex && span[endIndex].IsTabOrSpace(); endIndex--) ;

                return span.Slice(startIndex, endIndex - startIndex + 1);
            }

            return [];
        }

    }

    private TextBufferLine UpdateAfterReadWithJoinInternal(BufferPosition lineTerminalPosition)
    {
        int i;
        var requiredBufferSize = _buffers[_position.BufferIndex].Length - _position.SubIndex + lineTerminalPosition.SubIndex;
        for (i = _position.BufferIndex + 1; i < lineTerminalPosition.BufferIndex; i++)
        {
            requiredBufferSize += _buffers[i].Length;
        }

        _tempJoinBuffer = MemoryPool<char>.Shared.Rent(requiredBufferSize);

        var joinBufferMemory = _tempJoinBuffer.Memory.Slice(0, requiredBufferSize);
        var joinWriteSpan = joinBufferMemory.Span;

        var buffer = _buffers[_position.BufferIndex];
        buffer.AsSpan(_position.SubIndex).CopyTo(joinWriteSpan);
        joinWriteSpan = joinWriteSpan.Slice(buffer.Length - _position.SubIndex);

        for (i = _position.BufferIndex + 1; i < lineTerminalPosition.BufferIndex; i++)
        {
            buffer = _buffers[i];
            buffer.AsSpan().CopyTo(joinWriteSpan);
            joinWriteSpan = joinWriteSpan.Slice(buffer.Length);
        }

        buffer = _buffers[lineTerminalPosition.BufferIndex];
        buffer.AsSpan(0, lineTerminalPosition.SubIndex).CopyTo(joinWriteSpan);

        _current = joinBufferMemory;

        return buffer;
    }

    private BufferPosition GetTerminalBufferPosition()
    {
        var lastBufferIndex = _buffers.Count - 1;
        return new(lastBufferIndex, lastBufferIndex >= 0 ? _buffers[lastBufferIndex].Length : 0);
    }

    private BufferPosition? ScanForwardToNextLineBreak() => ScanForwardToNextLineBreak(_position.BufferIndex, _position.SubIndex);

    private BufferPosition? ScanForwardToNextLineBreak(int bufferIndex, int subIndex)
    {
        for (; bufferIndex < _buffers.Count; bufferIndex++)
        {
            subIndex = _buffers[bufferIndex].FindLineBreak(subIndex);

            if (subIndex >= 0)
            {
                return new(bufferIndex, subIndex);
            }

            subIndex = 0;
        }

        return null;
    }

#if NO_STREAM_SYSMEM
    private int ReadNewTextBufferLines()
    {
        var fileBytesRead = _stream.Read(_reusableFileReadBuffer, 0, _reusableFileReadBuffer.Length);
        if (fileBytesRead == 0)
        {
            return 0;
        }

        return DecodeIntoBufferLines(_reusableFileReadBuffer.AsSpan(0, fileBytesRead));
    }
#else
    private int ReadNewTextBufferLines()
    {
        var fileReadBuffer = _reusableFileReadBuffer.AsSpan();
        var fileBytesRead = _stream.Read(fileReadBuffer);
        if (fileBytesRead == 0)
        {
            return 0;
        }

        return DecodeIntoBufferLines(fileReadBuffer.Slice(0, fileBytesRead));
    }
#endif

#if NO_VALUETASK || NO_STREAM_SYSMEM
    private async Task<int> ReadNewTextBufferLinesAsync(CancellationToken ct)
    {
        var fileBytesRead = await _stream.ReadAsync(_reusableFileReadBuffer, 0, _reusableFileReadBuffer.Length, ct).ConfigureAwait(false);
        if (fileBytesRead == 0)
        {
            return 0;
        }

        return DecodeIntoBufferLines(_reusableFileReadBuffer.AsSpan(0, fileBytesRead));
    }
#else
    private async ValueTask<int> ReadNewTextBufferLinesAsync(CancellationToken ct)
    {
        var fileReadBuffer = _reusableFileReadBuffer.AsMemory();
        var fileBytesRead = await _stream.ReadAsync(fileReadBuffer, ct).ConfigureAwait(false);
        if (fileBytesRead == 0)
        {
            return 0;
        }

        return DecodeIntoBufferLines(fileReadBuffer.Span.Slice(0, fileBytesRead));
    }
#endif

    private int DecodeIntoBufferLines(ReadOnlySpan<byte> fileReadByteBuffer)
    {
        var linesAdded = 0;

        if (!_hasReadPreamble)
        {
            ReadPreamble(ref fileReadByteBuffer);
        }

        while (fileReadByteBuffer.Length > 0)
        {
            var textBuffer = AllocateBufferForNewWrites();

            _decoder.Convert(
                fileReadByteBuffer,
                textBuffer.GetFullWriteSpan(),
                flush: false,
                out var bytesConsumed,
                out var charsProduced,
                out _);

#if DEBUG
            if (bytesConsumed == 0) ExceptionEx.ThrowInvalidOperation();
#endif

            fileReadByteBuffer = fileReadByteBuffer.Slice(bytesConsumed);

            if (charsProduced == 0)
            {
                continue;
            }

            textBuffer.PrepareMemoryForUse(charsProduced);

            _buffers.Add(textBuffer);
            linesAdded++;
        }

        return linesAdded;
    }

    private TextBufferLine AllocateBufferForNewWrites()
    {
        TextBufferLine buffer;

        while (_position.BufferIndex > 0 && _buffers.Count > 0)
        {
            buffer = _buffers[0];

            _buffers.RemoveAt(0);
            _position.BufferIndex--;

            if (!buffer.PreventRecycle)
            {
                buffer.ResetMemory();
                return buffer;
            }
        }

        return new(DefaultBufferSize);
    }

    private void ReadPreamble(ref ReadOnlySpan<byte> fileBytes)
    {
        if (fileBytes.Length > 0)
        {
            _hasReadPreamble = true;

            foreach (var candidateEncoding in PreambleEncodings)
            {
                if (
                    candidateEncoding.GetPreamble() is { Length: > 0 } encodingPreamble
                    && fileBytes.StartsWith(encodingPreamble)
                )
                {
                    ChangeEncoding(candidateEncoding);
                    fileBytes = fileBytes.Slice(encodingPreamble.Length);
                }
            }
        }
    }

    private void ChangeEncoding(ReadOnlySpan<char> encodingName)
    {
        if (EncodingEx.GetEncodingByName(encodingName) is { } newEncoding)
        {
            ChangeEncoding(newEncoding);
        }
    }

    private void ChangeEncoding(Encoding newEncoding)
    {
        if (_encoding is not null && (ReferenceEquals(newEncoding, _encoding) || _encoding.Equals(newEncoding)))
        {
            return;
        }

        var oldEncoding = _encoding ?? Encoding.UTF8;

        _encoding = newEncoding;
        _decoder = newEncoding.GetDecoder();

        if (_position.BufferIndex < _buffers.Count)
        {
            // The characters that were loaded after the encoding change need to be re-decoded
            for (var bufferIndex = _position.BufferIndex; bufferIndex < _buffers.Count; bufferIndex++)
            {
                var offset = _position.BufferIndex == bufferIndex ? _position.SubIndex : 0;
                var newCharacters = _buffers[bufferIndex].ReEncode(offset, oldEncoding, newEncoding);
                _buffers[bufferIndex] = new TextBufferLine(newCharacters, preventRecycle: true);
            }

            _position.SubIndex = 0;
        }
    }

    public void Dispose()
    {
        _tempJoinBuffer?.Dispose();

        if (_ownsStream)
        {
            _stream.Dispose();
        }
    }

    private struct TextBufferLine
    {
        public TextBufferLine(int rawBufferSize)
        {
            _raw = new char[rawBufferSize];
            _length = 0;
            _preventRecycle = false;
        }

        public TextBufferLine(char[] raw, bool preventRecycle)
        {
            _raw = raw;
            _length = raw.Length;
            _preventRecycle = preventRecycle;
        }

        private readonly char[] _raw;
        private int _length;
        private readonly bool _preventRecycle;

        public readonly int Length => _length;

        public readonly bool PreventRecycle => _preventRecycle;

        public readonly int FindLineBreak(int startIndex) => Array.IndexOf(_raw, CharacterLf, startIndex, _length - startIndex);

        public readonly ReadOnlySpan<char> AsSpan() => _raw.AsSpan(0, _length);

        public readonly ReadOnlySpan<char> AsSpan(int offset) => _raw.AsSpan(offset, _length - offset);

        public readonly ReadOnlySpan<char> AsSpan(int offset, int length)
        {
#if DEBUG
            ExceptionEx.ThrowIfArgumentGreaterThan(offset, _length, nameof(offset));
            ExceptionEx.ThrowIfArgumentGreaterThan(offset + length, _length, nameof(length));
#endif
            return _raw.AsSpan(offset, length);
        }

        public readonly Span<char> GetFullWriteSpan() => _raw.AsSpan();

        public readonly ReadOnlyMemory<char> AsMemory(int offset, int length)
        {
#if DEBUG
            ExceptionEx.ThrowIfArgumentGreaterThan(offset, _length, nameof(offset));
            ExceptionEx.ThrowIfArgumentGreaterThan(offset + length, _length, nameof(length));
#endif

            return _raw.AsMemory(offset, length);
        }

        public void PrepareMemoryForUse(int valueSize)
        {
            _length = valueSize;
        }

        public void ResetMemory()
        {
            _length = 0;
        }

        internal readonly char[] ReEncode(int offset, Encoding oldEncoding, Encoding newEncoding) =>
            newEncoding.GetChars(oldEncoding.GetBytes(_raw, offset, _length - offset));
    }

    private struct BufferPosition
    {
        public BufferPosition(int bufferIndex, int subIndex)
        {
            BufferIndex = bufferIndex;
            SubIndex = subIndex;
        }

        public int BufferIndex;
        public int SubIndex;
    }
}

