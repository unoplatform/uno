// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Uno-specific additions to VirtualizingLayout.

using System;
using Windows.Foundation;
using Uno;

namespace Microsoft.UI.Xaml.Controls;

partial class VirtualizingLayout
{
	/// <summary>
	/// Determines if the difference between 2 viewports is large enough to cause a layout pass.
	/// </summary>
	/// <remarks>
	/// This is Uno-specific. It has been introduced to reduce the number of layout passes for
	/// performance considerations. However a too high threshold would cause longer passes (as more
	/// items would be added/removed at once).
	/// Note: this is used only for viewport; if the size of the ItemsRepeater changes it will
	/// still be re-layouted.
	/// </remarks>
	/// <param name="state">The layout state</param>
	/// <param name="oldViewport">Previous viewport</param>
	/// <param name="newViewport">Updated viewport</param>
	[UnoOnly]
	protected internal virtual bool IsSignificantViewportChange(object state, Rect oldViewport, Rect newViewport)
	{
		const double delta = 50;
		return Math.Abs(oldViewport.Width - newViewport.Width) > delta
			|| Math.Abs(oldViewport.Height - newViewport.Height) > delta
			|| Math.Abs(oldViewport.Top - newViewport.Top) > delta
			|| Math.Abs(oldViewport.Left - newViewport.Left) > delta;
	}
}
