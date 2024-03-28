// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBarTemplateSettings.properties.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent
/// sources when defining templates for an InfoBar.
/// </summary>
public partial class InfoBarTemplateSettings : DependencyObject
{
	/// <summary>
	/// Initializes a new instance of the InfoBarTemplateSettings class.
	/// </summary>
	public InfoBarTemplateSettings()
	{
	}

	/// <summary>
	/// Gets the icon element.
	/// </summary>
	public IconElement IconElement
	{
		get => (IconElement)GetValue(IconElementProperty);
		set => SetValue(IconElementProperty, value);
	}

	/// <summary>
	/// Identifies the InfoBarTemplateSettings.IconElement dependency property.
	/// </summary>
	public static DependencyProperty IconElementProperty { get; } =
		DependencyProperty.Register(nameof(IconElement), typeof(IconElement), typeof(InfoBarTemplateSettings), new FrameworkPropertyMetadata(null));
}
