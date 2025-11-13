// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Numerics;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Controls;

public sealed partial class ScrollingScrollAnimationStartingEventArgs
{
	internal ScrollingScrollAnimationStartingEventArgs()
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	}

	// ~ScrollingScrollAnimationStartingEventArgs()
	// {
	//     SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }

	// UNO TODO:
	// WinUI setter throws on null value.
	public CompositionAnimation Animation { get; set; }

	public int CorrelationId { get; internal set; } = -1;

	public Vector2 StartPosition { get; internal set; }

	public Vector2 EndPosition { get; internal set; }
}
