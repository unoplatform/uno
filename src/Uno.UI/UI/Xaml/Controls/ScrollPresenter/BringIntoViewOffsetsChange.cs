// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal sealed partial class BringIntoViewOffsetsChange : OffsetsChange
{
	// UNO TODO: owner is unused
	public BringIntoViewOffsetsChange(
		/*ITrackerHandleManager*/ UIElement owner,
		double zoomedHorizontalOffset,
		double zoomedVerticalOffset,
		ScrollPresenterViewKind offsetsKind,
		object options,
		UIElement element,
		Rect elementRect,
		double horizontalAlignmentRatio,
		double verticalAlignmentRatio,
		double horizontalOffset,
		double verticalOffset) : base(zoomedHorizontalOffset, zoomedVerticalOffset, offsetsKind, options)
	{
		Element = element;
		ElementRect = elementRect;
		HorizontalAlignmentRatio = horizontalAlignmentRatio;
		VerticalAlignmentRatio = verticalAlignmentRatio;
		HorizontalOffset = horizontalOffset;
		VerticalOffset = verticalOffset;
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	}

	// ~BringIntoViewOffsetsChange()
	// {
	// 	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }
}
