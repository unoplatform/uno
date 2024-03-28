// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml;

namespace Microsoft.UI.Private.Controls;

internal partial class ScrollPresenterTestHooksAnchorEvaluatedEventArgs
{
	private WeakReference<UIElement> m_anchorElement;

	public ScrollPresenterTestHooksAnchorEvaluatedEventArgs(
		UIElement anchorElement,
		double viewportAnchorPointHorizontalOffset,
		double viewportAnchorPointVerticalOffset)
	{
		if (anchorElement is not null)
		{
			m_anchorElement = new WeakReference<UIElement>(anchorElement);
		}
		ViewportAnchorPointHorizontalOffset = viewportAnchorPointHorizontalOffset;
		ViewportAnchorPointVerticalOffset = viewportAnchorPointVerticalOffset;
	}

	#region IScrollPresenterTestHooksAnchorEvaluatedEventArgs

	public double ViewportAnchorPointHorizontalOffset { get; }
	public double ViewportAnchorPointVerticalOffset { get; }
	public UIElement AnchorElement => m_anchorElement is not null && m_anchorElement.TryGetTarget(out var anchorElement) ? anchorElement : null;

	#endregion
}
