// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextPosition.cpp, TextPosition.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Text.Core;

// Thin wrapper over PlainTextPosition. All members delegate to the underlying plain position.
internal struct TextPosition
{
	private PlainTextPosition m_plainPosition;

	public TextPosition(PlainTextPosition position)
	{
		m_plainPosition = position;
	}

	public bool GetOffset(out uint pOffset) => m_plainPosition.GetOffset(out pOffset);

	public bool IsValid() => m_plainPosition.IsValid();

	public bool Equals(TextPosition other) => m_plainPosition.Equals(other.m_plainPosition);

	public bool LessThan(TextPosition other) => m_plainPosition.LessThan(other.m_plainPosition);

	public bool IsAtInsertionPosition(out bool pIsAtInsertionPosition)
		=> m_plainPosition.IsAtInsertionPosition(out pIsAtInsertionPosition);

	public bool GetNextInsertionPosition(
		out bool pFoundPosition,
		out TextPosition pPosition)
	{
		bool result = m_plainPosition.GetNextInsertionPosition(out pFoundPosition, out var position);
		pPosition = new TextPosition(position);
		return result;
	}

	public bool GetPreviousInsertionPosition(
		out bool pFoundPosition,
		out TextPosition pPosition)
	{
		bool result = m_plainPosition.GetPreviousInsertionPosition(out pFoundPosition, out var position);
		pPosition = new TextPosition(position);
		return result;
	}

	public bool GetBackspacePosition(
		out bool pFoundPosition,
		out TextPosition pPosition)
	{
		bool result = m_plainPosition.GetBackspacePosition(out pFoundPosition, out var position);
		pPosition = new TextPosition(position);
		return result;
	}

	public PlainTextPosition GetPlainPosition() => m_plainPosition;

	public bool IsAfterLineBreak(out bool pIsAfterLineBreak)
	{
		// TODO: May be needed in RichTextBlock/TextBlock selection scenarios.
		pIsAfterLineBreak = false;
		return true;
	}

	public bool IsInsideLineBreak(out bool pIsInsideLineBreak)
		=> m_plainPosition.IsInsideLineBreak(out pIsInsideLineBreak);

	/*
	Overloaded comparison operators for convenience. They're all defined in terms
	of TextPosition::Equals and TextPosition::LessThan.
	*/

	public static bool operator ==(TextPosition lhs, TextPosition rhs) => lhs.Equals(rhs);

	public static bool operator !=(TextPosition lhs, TextPosition rhs) => !(lhs == rhs);

	public static bool operator <(TextPosition lhs, TextPosition rhs) => lhs.LessThan(rhs);

	public static bool operator <=(TextPosition lhs, TextPosition rhs) => lhs < rhs || lhs == rhs;

	public static bool operator >(TextPosition lhs, TextPosition rhs) => !(lhs <= rhs);

	public static bool operator >=(TextPosition lhs, TextPosition rhs) => !(lhs < rhs);

	public override bool Equals(object? obj) => obj is TextPosition other && Equals(other);

	public override int GetHashCode()
	{
		GetOffset(out var offset);
		return (int)offset;
	}
}
