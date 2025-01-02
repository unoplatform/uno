// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationViewItemPresenterTemplateSettings.properties.cpp, commit 465f0d7

using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;

public partial class NavigationViewItemPresenterTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets the width of the icon.
	/// </summary>
	public double IconWidth
	{
		get => (double)GetValue(IconWidthProperty);
		internal set => SetValue(IconWidthProperty, value);
	}

	/// <summary>
	/// Identifies the IconWidth dependency property.
	/// </summary>
	public static DependencyProperty IconWidthProperty { get; } =
		DependencyProperty.Register(nameof(IconWidth), typeof(double), typeof(NavigationViewItemPresenterTemplateSettings), new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the width of the smaller icon.
	/// </summary>
	public double SmallerIconWidth
	{
		get => (double)GetValue(SmallerIconWidthProperty);
		internal set => SetValue(SmallerIconWidthProperty, value);
	}

	/// <summary>
	/// Identifies the SmallerIconWidth dependency property.
	/// </summary>
	public static DependencyProperty SmallerIconWidthProperty { get; } =
		DependencyProperty.Register(nameof(SmallerIconWidth), typeof(double), typeof(NavigationViewItemPresenterTemplateSettings), new FrameworkPropertyMetadata(0.0));
}
