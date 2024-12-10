// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBadgeTemplateSettings.properties.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for an InfoBadge.
/// </summary>
public partial class InfoBadgeTemplateSettings : DependencyObject
{
	/// <summary>
	/// Initializes a new instance of the InfoBadgeTemplateSettings class.
	/// </summary>
	public InfoBadgeTemplateSettings()
	{
	}

	/// <summary>
	/// Gets or sets the icon element for an InfoBadge.
	/// </summary>
	public IconElement IconElement
	{
		get => (IconElement)GetValue(IconElementProperty);
		set => SetValue(IconElementProperty, value);
	}

	/// <summary>
	/// Identifies the InfoBadgeTemplateSettings.IconElement dependency property.
	/// </summary>
	public static DependencyProperty IconElementProperty { get; } =
		DependencyProperty.Register(nameof(IconElement), typeof(IconElement), typeof(InfoBadgeTemplateSettings), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the corner radius for an InfoBadge.
	/// </summary>
	public CornerRadius InfoBadgeCornerRadius
	{
		get => (CornerRadius)GetValue(InfoBadgeCornerRadiusProperty);
		set => SetValue(InfoBadgeCornerRadiusProperty, value);
	}

	/// <summary>
	/// Identifies the InfoBadgeTemplateSettings.InfoBadgeCornerRadius dependency property.
	/// </summary>
	public static DependencyProperty InfoBadgeCornerRadiusProperty { get; } =
		DependencyProperty.Register(nameof(InfoBadgeCornerRadius), typeof(CornerRadius), typeof(InfoBadgeTemplateSettings), new FrameworkPropertyMetadata(default(CornerRadius)));
}
