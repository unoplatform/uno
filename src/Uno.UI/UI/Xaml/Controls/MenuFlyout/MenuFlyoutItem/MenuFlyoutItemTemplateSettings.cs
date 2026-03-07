// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\winrtgeneratedclasses\MenuFlyoutItemTemplateSettings.g.cpp, tag winui3/release/1.8.1, commit cd3b7ad0eca

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a MenuFlyoutPresenter control.
/// Not intended for general use.
/// </summary>
public partial class MenuFlyoutItemTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets the minimum width allocated for the accelerator key tip of an MenuFlyout.
	/// </summary>
	public double KeyboardAcceleratorTextMinWidth
	{
		get => (double)GetValue(KeyboardAcceleratorTextMinWidthProperty);
		internal set => SetValue(KeyboardAcceleratorTextMinWidthProperty, value);
	}

	internal static DependencyProperty KeyboardAcceleratorTextMinWidthProperty { get; } =
		DependencyProperty.Register(
			nameof(KeyboardAcceleratorTextMinWidth),
			typeof(double),
			typeof(MenuFlyoutItemTemplateSettings),
			new FrameworkPropertyMetadata(0.0));
}
