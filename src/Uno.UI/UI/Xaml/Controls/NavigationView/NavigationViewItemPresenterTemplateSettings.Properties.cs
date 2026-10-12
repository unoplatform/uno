// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationViewItemPresenterTemplateSettings.properties.cpp, commit bac7a9c33

using Microsoft.UI.Xaml;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class NavigationViewItemPresenterTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets the width of the icon.
	/// </summary>
	public double IconWidth
	{
		get => (double)GetValue(IconWidthProperty);
		internal set => SetValue(IconWidthProperty, Boxer.Box(value));
	}

	/// <summary>
	/// Identifies the IconWidth dependency property.
	/// </summary>
	public static DependencyProperty IconWidthProperty { get; } =
		DependencyProperty.Register(nameof(IconWidth), typeof(double), typeof(NavigationViewItemPresenterTemplateSettings), new FrameworkPropertyMetadata(DoubleBoxes.Zero));

	/// <summary>
	/// Gets the width of the smaller icon.
	/// </summary>
	public double SmallerIconWidth
	{
		get => (double)GetValue(SmallerIconWidthProperty);
		internal set => SetValue(SmallerIconWidthProperty, Boxer.Box(value));
	}

	/// <summary>
	/// Identifies the SmallerIconWidth dependency property.
	/// </summary>
	public static DependencyProperty SmallerIconWidthProperty { get; } =
		DependencyProperty.Register(nameof(SmallerIconWidth), typeof(double), typeof(NavigationViewItemPresenterTemplateSettings), new FrameworkPropertyMetadata(DoubleBoxes.Zero));
}
