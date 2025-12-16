// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\ToggleMenuFlyoutItem_Partial.cpp, tag winui3/release/1.8.1, commit cd3b7ad0eca

namespace Microsoft.UI.Xaml.Controls;

partial class ToggleMenuFlyoutItem
{
	/// <summary>
	/// Gets or sets whether the ToggleMenuFlyoutItem is checked.
	/// </summary>
	public bool IsChecked
	{
		get => (bool)GetValue(IsCheckedProperty);
		set => SetValue(IsCheckedProperty, value);
	}

	/// <summary>
	/// Identifies the IsChecked dependency property.
	/// </summary>
	public static DependencyProperty IsCheckedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsChecked),
			typeof(bool),
			typeof(ToggleMenuFlyoutItem),
			new FrameworkPropertyMetadata(false));
}
