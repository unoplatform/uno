// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBarPanel.properties.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class InfoBarPanel
{
	/// <summary>
	/// Gets the HorizontalOrientationMargin from an object.
	/// </summary>
	/// <param name="obj">The object that has an HorizontalOrientationMargin.</param>
	/// <returns>The HorizontalOrientationMargin thickness.</returns>
	public static Thickness GetHorizontalOrientationMargin(DependencyObject @object) => (Thickness)@object.GetValue(HorizontalOrientationMarginProperty);

	/// <summary>
	/// Sets the HorizontalOrientationMargin to an object.
	/// </summary>
	/// <param name="obj">The object that the HorizontalOrientationMargin value will be set to.</param>
	/// <param name="value">The thickness of the HorizontalOrientationMargin.</param>
	public static void SetHorizontalOrientationMargin(DependencyObject @object, Thickness value) => @object.SetValue(HorizontalOrientationMarginProperty, value);

	/// <summary>
	/// Gets the identifier for the HorizontalOrientationMargin dependency property.
	/// </summary>
	public static DependencyProperty HorizontalOrientationMarginProperty { get; } =
		DependencyProperty.RegisterAttached("HorizontalOrientationMargin", typeof(Thickness), typeof(InfoBarPanel), new FrameworkPropertyMetadata(default(Thickness)));

	/// <summary>
	/// Gets and sets the distance between the edges of the InfoBarPanel
	/// and its children when the panel is oriented horizontally.
	/// </summary>
	public Thickness HorizontalOrientationPadding
	{
		get => (Thickness)GetValue(HorizontalOrientationPaddingProperty);
		set => SetValue(HorizontalOrientationPaddingProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the HorizontalOrientationPadding dependency property.
	/// </summary>
	public static DependencyProperty HorizontalOrientationPaddingProperty { get; } =
		DependencyProperty.Register(nameof(HorizontalOrientationPadding), typeof(Thickness), typeof(InfoBarPanel), new FrameworkPropertyMetadata(default(Thickness)));

	/// <summary>
	/// Gets the VerticalOrientationMargin from an object.
	/// </summary>
	/// <param name="obj">The object that has a VerticalOrientationMargin.</param>
	/// <returns>The VerticalOrientationMargin thickness.</returns>
	public static Thickness GetVerticalOrientationMargin(DependencyObject @object) => (Thickness)@object.GetValue(VerticalOrientationMarginProperty);

	/// <summary>
	/// Sets the VerticalOrientationMargin to an object.
	/// </summary>
	/// <param name="obj">The object that the VerticalOrientationMargin value will be set to.</param>
	/// <param name="value">The thickness of the VerticalOrientationMargin.</param>
	public static void SetVerticalOrientationMargin(DependencyObject @object, Thickness value) => @object.SetValue(VerticalOrientationMarginProperty, value);

	/// <summary>
	/// Gets the identifier for the VerticalOrientationMargin dependency property.
	/// </summary>
	public static DependencyProperty VerticalOrientationMarginProperty { get; } =
		DependencyProperty.RegisterAttached("VerticalOrientationMargin", typeof(Thickness), typeof(InfoBarPanel), new FrameworkPropertyMetadata(default(Thickness)));

	/// <summary>
	/// Gets and sets the distance between the edges of the InfoBarPanel
	/// and its children when the panel is oriented vertically.
	/// </summary>
	public Thickness VerticalOrientationPadding
	{
		get => (Thickness)GetValue(VerticalOrientationPaddingProperty);
		set => SetValue(VerticalOrientationPaddingProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the VerticalOrientationPadding dependency property.
	/// </summary>
	public static DependencyProperty VerticalOrientationPaddingProperty { get; } =
		DependencyProperty.Register(nameof(VerticalOrientationPadding), typeof(Thickness), typeof(InfoBarPanel), new FrameworkPropertyMetadata(default(Thickness)));
}
