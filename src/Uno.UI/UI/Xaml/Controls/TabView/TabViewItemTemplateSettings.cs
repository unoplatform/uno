// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabViewItemTemplateSettings.properties.cpp, commit 65718e2813

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Gets an object that provides calculated values that can be referenced as {TemplateBinding}
/// markup extension sources when defining templates for a TabViewItem control.
/// </summary>
public partial class TabViewItemTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as {TemplateBinding}
	/// markup extension sources when defining templates for a TabViewItem control.
	/// </summary>
	public IconElement IconElement
	{
		get => (IconElement)GetValue(IconElementProperty);
		set => SetValue(IconElementProperty, value);
	}

	/// <summary>
	/// Identifies the IconElement dependency property.
	/// </summary>
	public static DependencyProperty IconElementProperty { get; } =
		DependencyProperty.Register(nameof(IconElement), typeof(IconElement), typeof(TabViewItemTemplateSettings), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets tab geometry.
	/// </summary>
	public Geometry TabGeometry
	{
		get => (Geometry)GetValue(TabGeometryProperty);
		set => SetValue(TabGeometryProperty, value);
	}

	/// <summary>
	/// Identifies the TabGeometry dependency property.
	/// </summary>
	public static DependencyProperty TabGeometryProperty { get; } =
		DependencyProperty.Register(nameof(TabGeometry), typeof(Geometry), typeof(TabViewItemTemplateSettings), new FrameworkPropertyMetadata(null));
}
