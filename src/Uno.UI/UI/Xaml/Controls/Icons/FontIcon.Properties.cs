// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.h

#nullable enable

using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.Boxes;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Controls;

partial class FontIcon
{
	/// <summary>
	/// Gets or sets the font used to display the icon glyph.
	/// </summary>
	public FontFamily FontFamily
	{
		get => (FontFamily)GetValue(FontFamilyProperty);
		set => SetValue(FontFamilyProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the FontFamily dependency property.
	/// </summary>
	public static DependencyProperty FontFamilyProperty { get; } =
		DependencyProperty.Register(
			nameof(FontFamily),
			typeof(FontFamily),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				// TODO Uno specific: Uno ships its own symbols font instead of GetDefaultFontIconFontFamily.
				new FontFamily(Uno.UI.FeatureConfiguration.Font.SymbolsFont),
				propertyChangedCallback: (s, e) => ((FontIcon)s).UpdateLinkedChildProperty(e.Property, TextBlock.FontFamilyProperty)));

	/// <summary>
	/// Gets or sets the size of the icon glyph.
	/// </summary>
	public double FontSize
	{
		get => (double)GetValue(FontSizeProperty);
		set => SetValue(FontSizeProperty, Boxer.Box(value));
	}

	/// <summary>
	/// Gets the identifier for the FontSize dependency property.
	/// </summary>
	public static DependencyProperty FontSizeProperty { get; } =
		DependencyProperty.Register(
			nameof(FontSize),
			typeof(double),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				g_ClientCoreFontSize,
				propertyChangedCallback: (s, e) => ((FontIcon)s).UpdateLinkedChildProperty(e.Property, TextBlock.FontSizeProperty)));

	/// <summary>
	/// Gets or sets the font style for the icon glyph.
	/// </summary>
	public FontStyle FontStyle
	{
		get => (FontStyle)GetValue(FontStyleProperty);
		set => SetValue(FontStyleProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the FontStyle dependency property.
	/// </summary>
	public static DependencyProperty FontStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(FontStyle),
			typeof(FontStyle),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				FontStyle.Normal,
				propertyChangedCallback: (s, e) => ((FontIcon)s).UpdateLinkedChildProperty(e.Property, TextBlock.FontStyleProperty)));

	/// <summary>
	/// Gets or sets the thickness of the icon glyph.
	/// </summary>
	public FontWeight FontWeight
	{
		get => (FontWeight)GetValue(FontWeightProperty);
		set => SetValue(FontWeightProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the FontWeight dependency property.
	/// </summary>
	public static DependencyProperty FontWeightProperty { get; } =
		DependencyProperty.Register(
			nameof(FontWeight),
			typeof(FontWeight),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				FontWeights.Normal,
				propertyChangedCallback: (s, e) => ((FontIcon)s).UpdateLinkedChildProperty(e.Property, TextBlock.FontWeightProperty)));

	/// <summary>
	/// Gets or sets the character code that identifies the icon glyph.
	/// </summary>
	public string Glyph
	{
		get => (string)GetValue(GlyphProperty);
		set => SetValue(GlyphProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the Glyph dependency property.
	/// </summary>
	public static DependencyProperty GlyphProperty { get; } =
		DependencyProperty.Register(
			nameof(Glyph),
			typeof(string),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				string.Empty,
				propertyChangedCallback: (s, e) => ((FontIcon)s).UpdateLinkedChildProperty(e.Property, TextBlock.TextProperty)));

	/// <summary>
	/// Gets or sets whether automatic text enlargement, to reflect the system text size setting, is enabled.
	/// </summary>
	public bool IsTextScaleFactorEnabled
	{
		get => (bool)GetValue(IsTextScaleFactorEnabledProperty);
		set => SetValue(IsTextScaleFactorEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsTextScaleFactorEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsTextScaleFactorEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsTextScaleFactorEnabled),
			typeof(bool),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(BoolBoxes.True));

	/// <summary>
	/// Gets or sets a value that indicates whether the icon is mirrored when the FlowDirection is RightToLeft.
	/// </summary>
	public bool MirroredWhenRightToLeft
	{
		get => (bool)GetValue(MirroredWhenRightToLeftProperty);
		set => SetValue(MirroredWhenRightToLeftProperty, value);
	}

	/// <summary>
	/// Identifies the MirroredWhenRightToLeft dependency property.
	/// </summary>
	public static DependencyProperty MirroredWhenRightToLeftProperty { get; } =
		DependencyProperty.Register(
			nameof(MirroredWhenRightToLeft),
			typeof(bool),
			typeof(FontIcon),
			new FrameworkPropertyMetadata(
				BoolBoxes.False,
				propertyChangedCallback: (s, e) => ((FontIcon)s).ApplyScaleTransformForFlowDirection()));
}
