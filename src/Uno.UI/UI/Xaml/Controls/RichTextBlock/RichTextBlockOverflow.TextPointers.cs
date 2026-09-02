// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference RichTextBlockOverflow.cpp (GetTextPositionFromPoint),
// RichTextBlockOverflow_Partial.cpp (ContentStart/End, GetPositionFromPoint),
// tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Documents;
#if __SKIA__
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents.BlockLayout;
#endif

namespace Microsoft.UI.Xaml.Controls;

// The overflow's position model lives on the master's TextContainer, queried through this element's
// own standalone view. On non-Skia targets there is no view, so these return null - the same as
// WinUI before layout has run.
partial class RichTextBlockOverflow
{
	/// <summary>
	/// Gets a TextPointer that represents the start of the content overflowed into this element.
	/// </summary>
	public TextPointer? ContentStart =>
#if __SKIA__
		GetContentStart();
#else
		null;
#endif

	/// <summary>
	/// Gets a TextPointer that represents the end of the content overflowed into this element.
	/// </summary>
	public TextPointer? ContentEnd =>
#if __SKIA__
		GetContentEnd();
#else
		null;
#endif

	/// <summary>
	/// Returns a TextPointer that corresponds to a Point in the coordinate space of this element.
	/// </summary>
	public TextPointer? GetPositionFromPoint(Point point) => GetTextPositionFromPoint(point);

#if __SKIA__
	// CRichTextBlockOverflow::GetTextPositionFromPoint
	private TextPointer? GetTextPositionFromPoint(Point point)
	{
		// Use this element's standalone view to query the pixel position. No coordinate
		// transformation is necessary - this API is called with element-relative coordinates.
		if (_pMaster is not null && _pTextView is not null)
		{
			// Recognise hits after newline = false.
			var position = _pTextView.PixelPositionToTextPosition(point, false, out var gravity);

			if (_pMaster.Blocks.GetTextContainer() is { } container)
			{
				var textPosition = new PlainTextPosition(container, position, gravity);
				return TextPointer.CreateInstanceWithInternalPointer(textPosition);
			}
		}

		return null;
	}
#else
	private TextPointer? GetTextPositionFromPoint(Point point) => null;
#endif
}
