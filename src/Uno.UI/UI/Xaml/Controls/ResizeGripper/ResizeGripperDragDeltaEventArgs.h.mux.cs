// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ResizeGripper\ResizeGripperDragDeltaEventArgs.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Private.Controls;

partial class ResizeGripperDragDeltaEventArgs
{
	// ResizeGripperDragDeltaEventArgs(double delta, double totalDelta);

	// double Delta();
	// double TotalDelta();

	// private:
	private double m_delta = 0.0;
	private double m_totalDelta = 0.0;
}
