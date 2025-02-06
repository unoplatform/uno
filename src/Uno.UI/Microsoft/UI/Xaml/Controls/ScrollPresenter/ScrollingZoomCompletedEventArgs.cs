// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Private.Controls;

namespace Windows.UI.Xaml.Controls;

public sealed partial class ScrollingZoomCompletedEventArgs
{
	internal ScrollingZoomCompletedEventArgs()
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	}

	// ~ScrollingZoomCompletedEventArgs()
	// {
	//     SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }

	public int CorrelationId { get; internal set; } = -1;

	internal ScrollPresenterViewChangeResult Result { get; set; } = ScrollPresenterViewChangeResult.Completed;
}
