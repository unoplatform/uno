// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ResizeGripper\ResizeGripperDragCompletedEventArgs.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Private.Controls;

partial class ResizeGripperDragCompletedEventArgs
{
	internal ResizeGripperDragCompletedEventArgs(double totalDelta, bool canceled)
	{
		m_totalDelta = totalDelta;
		m_canceled = canceled;
	}

	public double TotalDelta => m_totalDelta;

	public bool Canceled => m_canceled;
}
