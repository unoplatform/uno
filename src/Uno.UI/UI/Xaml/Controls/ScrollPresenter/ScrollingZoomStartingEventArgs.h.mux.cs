// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollingZoomStartingEventArgs.h, commit b8cfb8490

namespace Microsoft.UI.Xaml.Controls;

partial class ScrollingZoomStartingEventArgs
{
	internal ScrollingZoomStartingEventArgs()
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	}

#if HAS_UNO
	// TODO Uno: The original C++ destructor only traces destruction, so Uno does not add a finalizer.
	// Original destructor logic (not executed):
	// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
#endif

	private int m_correlationId = -1;
	private double m_horizontalOffset;
	private double m_verticalOffset;
	private float m_zoomFactor;
}
