// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.UI.Xaml.Controls;

public sealed partial class ScrollingBringingIntoViewEventArgs
{
	public ScrollingSnapPointsMode SnapPointsMode { get; set; }

	public BringIntoViewRequestedEventArgs RequestEventArgs { get; internal set; }

	public double TargetHorizontalOffset { get; private set; }

	public double TargetVerticalOffset { get; private set; }

	public int CorrelationId { get; internal set; } = -1;

	public bool Cancel { get; set; }

	internal void TargetOffsets(double targetHorizontalOffset, double targetVerticalOffset)
	{
		TargetHorizontalOffset = targetHorizontalOffset;
		TargetVerticalOffset = targetVerticalOffset;
	}
}
