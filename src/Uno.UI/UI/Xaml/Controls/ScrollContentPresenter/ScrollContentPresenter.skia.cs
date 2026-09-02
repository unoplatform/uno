// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Microsoft.UI.Xaml.Controls.cs, tag winui3/release/1.8.4, commit dc46907e92

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
