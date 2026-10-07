// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ResizeGripper\ResizeGripperDragDeltaEventArgs.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Private.Controls;

partial class ResizeGripperDragDeltaEventArgs
{
	internal ResizeGripperDragDeltaEventArgs(double delta, double totalDelta)
	{
		m_delta = delta;
		m_totalDelta = totalDelta;
	}

	public double Delta => m_delta;

	public double TotalDelta => m_totalDelta;
}
