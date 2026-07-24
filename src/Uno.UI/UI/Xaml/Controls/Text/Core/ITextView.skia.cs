// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextView.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.BlockLayout;

namespace Microsoft.UI.Xaml.Controls.Text.Core;

//------------------------------------------------------------------------
//
//  Interface:  ITextView
//
//  Query interface used to access layout results for text display
//  elements.
//
//------------------------------------------------------------------------
internal interface ITextView
{
	Rect[] TextRangeToTextBounds(uint startOffset, uint endOffset);

	Rect[] TextSelectionToTextBounds(IJupiterTextSelection selection);

	bool IsAtInsertionPosition(uint iTextPosition);

	uint PixelPositionToTextPosition(Point pixelCoordinate, bool bIncludeNewline, out TextGravity gravity);

	void TextPositionToPixelPosition(
		uint iTextPosition,
		TextGravity eGravity,
		out float pixelOffset,      // Relative to origin of line
		out float characterTop,     // Relative to TextView top
		out float characterHeight,
		out float lineTop,          // Relative to TextView top
		out float lineHeight,
		out float lineBaseline,
		out float lineOffset);      // Padding and alignment offset

	uint GetContentStartPosition();

	uint GetContentLength();

	int GetAdjustedPosition(int charIndex);

	int GetCharacterIndex(int position);

	FrameworkElement? GetUIScopeForPosition(uint iTextPosition, TextGravity eGravity);

	bool ContainsPosition(uint iTextPosition, TextGravity gravity);
}
