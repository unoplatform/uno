// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ResizeGripper\ResizeGripperDragCompletedEventArgs.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Private.Controls;

partial class ResizeGripperDragCompletedEventArgs
{
	// ResizeGripperDragCompletedEventArgs(double totalDelta, bool canceled);

	// double TotalDelta();
	// bool Canceled();

	// private:
	private double m_totalDelta = 0.0;
	private bool m_canceled = false;
}
