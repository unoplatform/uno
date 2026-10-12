// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference RichTextBlockOverflow.cpp (GetTextPositionFromPoint),
// RichTextBlockOverflow_Partial.cpp (ContentStart/End, GetPositionFromPoint),
// tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents.BlockLayout;

namespace Microsoft.UI.Xaml.Controls;

// The overflow's position model lives on the master's TextContainer, queried through this element's
// own standalone view. Before layout has run there is no view, so these return null, as WinUI does.
partial class RichTextBlockOverflow
{
	/// <summary>
	/// Gets a TextPointer that represents the start of the content overflowed into this element.
	/// </summary>
	public TextPointer? ContentStart => GetContentStart();

	/// <summary>
	/// Gets a TextPointer that represents the end of the content overflowed into this element.
	/// </summary>
	public TextPointer? ContentEnd => GetContentEnd();

	/// <summary>
	/// Returns a TextPointer that corresponds to a Point in the coordinate space of this element.
	/// </summary>
	public TextPointer? GetPositionFromPoint(Point point) => GetTextPositionFromPoint(point);

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
}
