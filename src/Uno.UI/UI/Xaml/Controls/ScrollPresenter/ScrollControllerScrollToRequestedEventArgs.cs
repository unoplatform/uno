// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ScrollControllerScrollToRequestedEventArgs.cpp, ScrollControllerScrollToRequestedEventArgs.h, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls.Primitives;

public sealed partial class ScrollControllerScrollToRequestedEventArgs
{
	//~ScrollControllerScrollToRequestedEventArgs()
	//{
	//	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	//}

	public ScrollControllerScrollToRequestedEventArgs(
		double offset,
		ScrollingScrollOptions options)
	{
		Offset = offset;
		Options = options;
	}

	public double Offset { get; }
	public ScrollingScrollOptions Options { get; }
	public int CorrelationId { get; set; } = -1;

}
