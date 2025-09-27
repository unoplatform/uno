// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference FontIconSource_Partial.cpp, tag winui3/release/1.4.2

using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using FontStyle = Windows.UI.Text.FontStyle;

namespace Microsoft.UI.Xaml.Controls;

public partial class FontIconSource : IconSource
{
	public bool MirroredWhenRightToLeft
	{
		get => (bool)GetValue(MirroredWhenRightToLeftProperty);
		set => SetValue(MirroredWhenRightToLeftProperty, value);
	}

	public static DependencyProperty MirroredWhenRightToLeftProperty { get; } =
		DependencyProperty.Register(nameof(MirroredWhenRightToLeft), typeof(bool), typeof(FontIconSource), new FrameworkPropertyMetadata(false, OnPropertyChanged));

	public bool IsTextScaleFactorEnabled
	{
		get => (bool)GetValue(IsTextScaleFactorEnabledProperty);
		set => SetValue(IsTextScaleFactorEnabledProperty, value);
	}

	public static DependencyProperty IsTextScaleFactorEnabledProperty { get; } =
		DependencyProperty.Register(nameof(IsTextScaleFactorEnabled), typeof(bool), typeof(FontIconSource), new FrameworkPropertyMetadata(true, OnPropertyChanged));

	public string Glyph
	{
		get => (string)GetValue(GlyphProperty);
		set => SetValue(GlyphProperty, value);
	}

	public static DependencyProperty GlyphProperty { get; } =
		DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(FontIconSource), new FrameworkPropertyMetadata(string.Empty, OnPropertyChanged));

	public FontWeight FontWeight
	{
		get => (FontWeight)GetValue(FontWeightProperty);
		set => SetValue(FontWeightProperty, value);
	}

	public static DependencyProperty FontWeightProperty { get; } =
		DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(FontIconSource), new FrameworkPropertyMetadata(new FontWeight(400), OnPropertyChanged));

	public FontStyle FontStyle
	{
		get => (FontStyle)GetValue(FontStyleProperty);
		set => SetValue(FontStyleProperty, value);
	}

	public static DependencyProperty FontStyleProperty { get; } =
		DependencyProperty.Register(nameof(FontStyle), typeof(FontStyle), typeof(FontIconSource), new FrameworkPropertyMetadata(FontStyle.Normal, OnPropertyChanged));

	public double FontSize
	{
		get => (double)GetValue(FontSizeProperty);
		set => SetValue(FontSizeProperty, value);
	}

	public static DependencyProperty FontSizeProperty { get; } =
		DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(FontIconSource), new FrameworkPropertyMetadata(20.0, OnPropertyChanged));

	public FontFamily FontFamily
	{
		get => (FontFamily)GetValue(FontFamilyProperty);
		set => SetValue(FontFamilyProperty, value);
	}

	public static DependencyProperty FontFamilyProperty { get; } =
		DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily), typeof(FontIconSource), new FrameworkPropertyMetadata(new FontFamily(Uno.UI.FeatureConfiguration.Font.SymbolsFont), OnPropertyChanged));

	/// <inheritdoc />
#if !HAS_UNO_WINUI
	private
#endif
	protected override IconElement CreateIconElementCore()
	{
		var fontIcon = new FontIcon()
		{
			Glyph = Glyph,
			FontSize = FontSize,
			FontWeight = FontWeight,
			FontStyle = FontStyle,
			IsTextScaleFactorEnabled = IsTextScaleFactorEnabled,
			MirroredWhenRightToLeft = MirroredWhenRightToLeft,
		};

		if (FontFamily != null)
		{
			fontIcon.FontFamily = FontFamily;
		}

		return fontIcon;
	}

#if !HAS_UNO_WINUI
	private
#endif
	protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty iconSourceProperty)
	{
		if (iconSourceProperty == FontFamilyProperty)
		{
			return FontIcon.FontFamilyProperty;
		}
		else if (iconSourceProperty == FontSizeProperty)
		{
			return FontIcon.FontSizeProperty;
		}
		else if (iconSourceProperty == FontStyleProperty)
		{
			return FontIcon.FontStyleProperty;
		}
		else if (iconSourceProperty == FontWeightProperty)
		{
			return FontIcon.FontWeightProperty;
		}
		else if (iconSourceProperty == GlyphProperty)
		{
			return FontIcon.GlyphProperty;
		}
		else if (iconSourceProperty == IsTextScaleFactorEnabledProperty)
		{
			return FontIcon.IsTextScaleFactorEnabledProperty;
		}
		else if (iconSourceProperty == MirroredWhenRightToLeftProperty)
		{
			return FontIcon.MirroredWhenRightToLeftProperty;
		}
		else
		{
			return base.GetIconElementPropertyCore(iconSourceProperty);
		}
	}
}
