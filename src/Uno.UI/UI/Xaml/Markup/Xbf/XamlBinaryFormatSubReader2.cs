// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference xamlbinaryformatsubreader2.cpp, commit fee1e8acc

#nullable enable

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Uno.UI.Xaml.Xbf;

// A client of an XBF node stream. This type is responsible for maintaining a small
// amount of state related to its current position in the node stream.
// It does not own the byte buffer passed into it.
//
// This type communicates errors that indicate XBF corruption using exceptions,
// while expected endOfStream conditions are returned using bools.
internal ref struct XamlBinaryFormatSubReader2
{
	private readonly ReadOnlySpan<byte> m_nodeStream;
	private int m_currentNodeStreamOffset;

	public XamlBinaryFormatSubReader2(ReadOnlySpan<byte> nodeStream, int nodeStreamOffset)
	{
		m_nodeStream = nodeStream;
		m_currentNodeStreamOffset = nodeStreamOffset;
	}

	public int NextIndex
	{
		readonly get => m_currentNodeStreamOffset;
		set => m_currentNodeStreamOffset = value;
	}

	// ReadNodeType is a 'try' method. Clients of this method that are using this
	// stream to create runtime object instances will often read to endOfStream.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadNodeType(int endOffset, out ObjectWriterNodeType nodeType)
	{
		if (m_currentNodeStreamOffset >= endOffset)
		{
			nodeType = ObjectWriterNodeType.EndOfStream;
			return false;
		}

		nodeType = (ObjectWriterNodeType)m_nodeStream[m_currentNodeStreamOffset++];
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte ReadByte() => m_nodeStream[m_currentNodeStreamOffset++];

	// Decodes a 7-bit encoded uint32: the high bit of each byte signals that more
	// bytes follow, the low 7 bits carry the payload.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ReadUInt()
	{
		var first = m_nodeStream[m_currentNodeStreamOffset];
		if (first < 0x80)
		{
			m_currentNodeStreamOffset++;
			return first;
		}

		return Read7BitEncodedIntSlow();
	}

	private int Read7BitEncodedIntSlow()
	{
		const int ContinuationBit = 0x80;
		const int PayloadMask = 0x7F;
		const int BitsPerEncodedByte = 7;
		// A uint32 (32 bits) requires at most 5 bytes.
		const int MaxEncodedBytes = 5;

		uint value = 0;
		var shift = 0;

		for (var bytesRead = 0; bytesRead < MaxEncodedBytes; ++bytesRead)
		{
			var currentByte = m_nodeStream[m_currentNodeStreamOffset++];
			value |= (uint)(currentByte & PayloadMask) << shift;
			shift += BitsPerEncodedByte;

			if ((currentByte & ContinuationBit) == 0)
			{
				return (int)value;
			}
		}

		// Overlong encoding: more continuation bytes than a uint32 can represent.
		throw new InvalidOperationException("The XBF node stream is corrupted (7-bit encoded integer).");
	}

	public PersistedConstantType ReadConstantNodeType() => (PersistedConstantType)ReadByte();

	public int ReadInt32()
	{
		var value = BinaryPrimitives.ReadInt32LittleEndian(m_nodeStream.Slice(m_currentNodeStreamOffset, 4));
		m_currentNodeStreamOffset += 4;
		return value;
	}

	public long ReadInt64()
	{
		var value = BinaryPrimitives.ReadInt64LittleEndian(m_nodeStream.Slice(m_currentNodeStreamOffset, 8));
		m_currentNodeStreamOffset += 8;
		return value;
	}

	public float ReadSingle() => BitConverter.Int32BitsToSingle(ReadInt32());

	public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

	public ReadOnlySpan<byte> ReadBytes(int length)
	{
		var bytes = m_nodeStream.Slice(m_currentNodeStreamOffset, length);
		m_currentNodeStreamOffset += length;
		return bytes;
	}
}
