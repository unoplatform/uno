// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollingScrollStartingEventArgs.cpp, commit b8cfb8490

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the <see cref="ScrollView.ScrollStarting"/> and
/// Microsoft.UI.Xaml.Controls.Primitives.ScrollPresenter.ScrollStarting events.
/// </summary>
public sealed partial class ScrollingScrollStartingEventArgs
{
	internal ScrollingScrollStartingEventArgs()
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	}

	// ~ScrollingScrollStartingEventArgs()
	// {
	//     SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }

	/// <summary>
	/// Gets the correlation identifier associated with the imminent scroll offsets change.
	/// </summary>
	public int CorrelationId { get; internal set; } = -1;

	/// <summary>
	/// Gets the anticipated horizontal offset once all queued offset changes are completed.
	/// </summary>
	public double HorizontalOffset { get; internal set; }

	/// <summary>
	/// Gets the anticipated vertical offset once all queued offset changes are completed.
	/// </summary>
	public double VerticalOffset { get; internal set; }

	/// <summary>
	/// Gets the anticipated zoom factor once all queued view changes are completed.
	/// </summary>
	public float ZoomFactor { get; internal set; }
}
