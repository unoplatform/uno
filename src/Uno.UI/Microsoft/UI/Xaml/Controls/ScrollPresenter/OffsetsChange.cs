// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Windows.UI.Xaml.Controls;

internal partial class OffsetsChange : ViewChange
{
	public double ZoomedHorizontalOffset { get; set; }
	public double ZoomedVerticalOffset { get; set; }

	public OffsetsChange(
		double zoomedHorizontalOffset,
		double zoomedVerticalOffset,
		ScrollPresenterViewKind offsetsKind,
		object options) : base(offsetsKind, options)
	{
		ZoomedHorizontalOffset = zoomedHorizontalOffset;
		ZoomedVerticalOffset = zoomedVerticalOffset;

		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_DBL_DBL, METH_NAME, this,
		// 	zoomedHorizontalOffset, zoomedVerticalOffset);
	}

	// ~OffsetsChange()
	// {
	// 	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }
}
