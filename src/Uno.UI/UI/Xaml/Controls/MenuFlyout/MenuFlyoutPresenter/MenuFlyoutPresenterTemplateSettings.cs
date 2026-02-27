// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\winrtgeneratedclasses\MenuFlyoutPresenterTemplateSettings.g.cpp, tag winui3/release/1.8.1, commit cd3b7ad0eca

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a MenuFlyoutPresenter control.
/// Not intended for general use.
/// </summary>
public partial class MenuFlyoutPresenterTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets the minimum width of flyout content.
	/// </summary>
	public double FlyoutContentMinWidth
	{
		get => (double)GetValue(FlyoutContentMinWidthProperty);
		internal set => SetValue(FlyoutContentMinWidthProperty, value);
	}

	internal static DependencyProperty FlyoutContentMinWidthProperty { get; } =
		DependencyProperty.Register(nameof(FlyoutContentMinWidth), typeof(double), typeof(MenuFlyoutPresenterTemplateSettings), new FrameworkPropertyMetadata(0.0));
}
