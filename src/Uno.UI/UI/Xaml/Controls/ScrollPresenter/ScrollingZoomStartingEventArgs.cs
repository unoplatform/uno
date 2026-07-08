// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollingZoomStartingEventArgs.cpp, commit b8cfb8490

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the <see cref="ScrollView.ZoomStarting"/> and
/// Microsoft.UI.Xaml.Controls.Primitives.ScrollPresenter.ZoomStarting events.
/// </summary>
public sealed partial class ScrollingZoomStartingEventArgs
{
	internal ScrollingZoomStartingEventArgs()
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	}

	// ~ScrollingZoomStartingEventArgs()
	// {
	//     SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }

	/// <summary>
	/// Gets the correlation identifier associated with the imminent zoom factor change.
	/// </summary>
	public int CorrelationId { get; internal set; } = -1;

	/// <summary>
	/// Gets the anticipated horizontal offset once all queued view changes are completed.
	/// </summary>
	public double HorizontalOffset { get; internal set; }

	/// <summary>
	/// Gets the anticipated vertical offset once all queued view changes are completed.
	/// </summary>
	public double VerticalOffset { get; internal set; }

	/// <summary>
	/// Gets the anticipated zoom factor once all queued zoom factor changes are completed.
	/// </summary>
	public float ZoomFactor { get; internal set; }
}
