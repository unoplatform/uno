// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBadge.properties.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class InfoBadge
{
	/// <summary>
	/// Gets or sets the icon to be used in an InfoBadge.
	/// </summary>
	public IconSource IconSource
	{
		get => (IconSource)GetValue(IconSourceProperty);
		set => SetValue(IconSourceProperty, value);
	}

	/// <summary>
	/// Identifies the InfoBadge.IconSource dependency property.
	/// </summary>
	public static DependencyProperty IconSourceProperty { get; } =
		DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(InfoBadge), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	private static void OnPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (InfoBadge)sender;
		owner.OnPropertyChanged(args);
	}

	/// <summary>
	/// Provides calculated values that can be referenced as TemplatedParent sources when defining 
	/// templates for an InfoBadge. Not intended for general use.
	/// </summary>
	public InfoBadgeTemplateSettings TemplateSettings
	{
		get => (InfoBadgeTemplateSettings)GetValue(TemplateSettingsProperty);
		private set => SetValue(TemplateSettingsProperty, value);
	}

	/// <summary>
	/// Identifies the InfoBadgeTemplateSettings dependency property.
	/// </summary>
	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TemplateSettings), typeof(InfoBadgeTemplateSettings), typeof(InfoBadge), new FrameworkPropertyMetadata(null, OnPropertyChanged));


	/// <summary>
	/// Gets or sets the integer to be displayed in a numeric InfoBadge.
	/// </summary>
	public int Value
	{
		get => (int)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	/// <summary>
	/// Identifies the InfoBadge.Value dependency property.
	/// </summary>
	public static DependencyProperty ValueProperty { get; } =
		DependencyProperty.Register(nameof(Value), typeof(int), typeof(InfoBadge), new FrameworkPropertyMetadata(-1, OnPropertyChanged));
}
