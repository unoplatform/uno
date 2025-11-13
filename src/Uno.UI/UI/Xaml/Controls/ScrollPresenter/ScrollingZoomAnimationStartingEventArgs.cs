// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Numerics;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Controls;

public sealed partial class ScrollingZoomAnimationStartingEventArgs
{
	internal ScrollingZoomAnimationStartingEventArgs()
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	}

	// ~ScrollingZoomAnimationStartingEventArgs()
	// {
	//     SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }

	public Vector2 CenterPoint { get; internal set; }

	public float StartZoomFactor { get; internal set; } = 1;

	public float EndZoomFactor { get; internal set; } = 1;

	// UNO TODO: WinUI checks for null in setter and throws.
	public CompositionAnimation Animation { get; set; }

	public int CorrelationId { get; internal set; } = -1;
}
