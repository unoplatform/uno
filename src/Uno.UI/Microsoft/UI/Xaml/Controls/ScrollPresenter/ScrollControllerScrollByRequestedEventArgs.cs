// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ScrollControllerScrollByRequestedEventArgs.cpp, ScrollControllerScrollByRequestedEventArgs.h, tag winui3/release/1.4.2

namespace Windows.UI.Xaml.Controls.Primitives;

public sealed partial class ScrollControllerScrollByRequestedEventArgs
{
	//~ScrollControllerScrollByRequestedEventArgs()
	//{
	//	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	//}

	public ScrollControllerScrollByRequestedEventArgs(
		double offsetDelta,
		ScrollingScrollOptions options)
	{
		//SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_DBL, METH_NAME, this,
		//	TypeLogging::ScrollOptionsToString(options).c_str(), offsetDelta);

		OffsetDelta = offsetDelta;
		Options = options;
	}

	public double OffsetDelta { get; }
	public ScrollingScrollOptions Options { get; }
	public int CorrelationId { get; set; } = -1;
}
