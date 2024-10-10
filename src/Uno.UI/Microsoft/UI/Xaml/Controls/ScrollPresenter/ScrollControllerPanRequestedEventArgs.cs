// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ScrollControllerPanRequestedEventArgs.cpp, ScrollControllerPanRequestedEventArgs.h, tag winui3/release/1.4.2

using Windows.UI.Input;

namespace Windows.UI.Xaml.Controls.Primitives;

public sealed partial class ScrollControllerPanRequestedEventArgs
{
	//~ScrollControllerPanRequestedEventArgs()
	//{
	//	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	//}

	public PointerPoint PointerPoint { get; }
	public bool Handled { get; set; }

	public ScrollControllerPanRequestedEventArgs(PointerPoint pointerPoint)
	{
		//SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR, METH_NAME, this, TypeLogging::PointerPointToString(pointerPoint).c_str());

		PointerPoint = pointerPoint;
	}
}
