// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference Microsoft.UI.Xaml.Controls.cs, commit 3c9c168844

namespace Microsoft.UI.Xaml.Controls;

public sealed partial class ScrollContentPresenter
{
	/// <summary>
	/// Gets or sets a value that indicates whether scrolled content can render outside the bounds of the ScrollViewer.
	/// </summary>
	public bool CanContentRenderOutsideBounds
	{
		get => (bool)GetValue(CanContentRenderOutsideBoundsProperty);
		set => SetValue(CanContentRenderOutsideBoundsProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="CanContentRenderOutsideBounds"/> dependency property.
	/// </summary>
	public static DependencyProperty CanContentRenderOutsideBoundsProperty { get; } =
		DependencyProperty.Register(
			nameof(CanContentRenderOutsideBounds),
			typeof(bool),
			typeof(ScrollContentPresenter),
			new FrameworkPropertyMetadata(false));
}
