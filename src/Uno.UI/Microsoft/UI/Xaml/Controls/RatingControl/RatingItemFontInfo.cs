// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RatingItemFontInfo.properties.cpp, tag winui3/release/1.5.3, commit 2a60e27c591846556fa9ec4d8f305afdf0f96dc1

using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Represents information about the visual states of font elements that represent a rating.
/// </summary>
public partial class RatingItemFontInfo : RatingItemInfo
{
	/// <summary>
	/// Initializes a new instance of the RatingItemFontInfo class.
	/// </summary>
	public RatingItemFontInfo()
	{
	}

	/// <summary>
	/// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element that is disabled.
	/// </summary>
	public string DisabledGlyph
	{
		get => (string)GetValue(DisabledGlyphProperty);
		set => SetValue(DisabledGlyphProperty, value);
	}

	/// <summary>
	/// Identifies the DisabledGlyph dependency property.
	/// </summary>
	public static DependencyProperty DisabledGlyphProperty { get; } =
		DependencyProperty.Register(
			nameof(DisabledGlyph),
			typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(string.Empty));

	/// <summary>
	/// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element that has been set by the user.
	/// </summary>
	public string Glyph
	{
		get => (string)GetValue(GlyphProperty);
		set => SetValue(GlyphProperty, value);
	}

	/// <summary>
	/// Identifies the Glyph dependency property.
	/// </summary>
	public static DependencyProperty GlyphProperty { get; } =
		DependencyProperty.Register(
			nameof(Glyph),
			typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(string.Empty));

	/// <summary>
	/// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element that is showing a placeholder value.
	/// </summary>
	public string PlaceholderGlyph
	{
		get => (string)GetValue(PlaceholderGlyphProperty);
		set => SetValue(PlaceholderGlyphProperty, value);
	}

	/// <summary>
	/// Identifies the PlaceholderGlyph dependency property.
	/// </summary>
	public static DependencyProperty PlaceholderGlyphProperty { get; } =
		DependencyProperty.Register(
			nameof(PlaceholderGlyph),
			typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(string.Empty));

	/// <summary>
	/// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element that has the pointer over it.
	/// </summary>
	public string PointerOverGlyph
	{
		get => (string)GetValue(PointerOverGlyphProperty);
		set => SetValue(PointerOverGlyphProperty, value);
	}

	/// <summary>
	/// Identifies the PointerOverGlyph dependency property.
	/// </summary>
	public static DependencyProperty PointerOverGlyphProperty { get; } =
		DependencyProperty.Register(
			nameof(PointerOverGlyph),
			typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(string.Empty));

	/// <summary>
	/// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element showing a placeholder value with the pointer over it.
	/// </summary>
	public string PointerOverPlaceholderGlyph
	{
		get => (string)GetValue(PointerOverPlaceholderGlyphProperty);
		set => SetValue(PointerOverPlaceholderGlyphProperty, value);
	}

	/// <summary>
	/// Identifies the PointerOverPlaceholderGlyph dependency property.
	/// </summary>
	public static DependencyProperty PointerOverPlaceholderGlyphProperty { get; } =
		DependencyProperty.Register(
			nameof(PointerOverPlaceholderGlyph),
			typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(string.Empty));

	/// <summary>
	/// Gets or sets a Segoe MDL2 Assets font glyph that represents a rating element that has not been set.
	/// </summary>
	public string UnsetGlyph
	{
		get => (string)GetValue(UnsetGlyphProperty);
		set => SetValue(UnsetGlyphProperty, value);
	}

	/// <summary>
	/// Identifies the UnsetGlyph dependency property.
	/// </summary>
	public static DependencyProperty UnsetGlyphProperty { get; } =
		DependencyProperty.Register(
			nameof(UnsetGlyph),
			typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(string.Empty));
}
