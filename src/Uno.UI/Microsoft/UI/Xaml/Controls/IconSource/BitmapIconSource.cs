#nullable disable

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BitmapIconSource.cpp, commit 083796a

using System;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class BitmapIconSource : IconSource
{
	public Uri UriSource
	{
		get => (Uri)GetValue(UriSourceProperty);
		set => SetValue(UriSourceProperty, value);
	}

	public static DependencyProperty UriSourceProperty { get; } =
		DependencyProperty.Register(nameof(UriSource), typeof(Uri), typeof(BitmapIconSource), new FrameworkPropertyMetadata(default(Uri), OnPropertyChanged));

	public bool ShowAsMonochrome
	{
		get => (bool)GetValue(ShowAsMonochromeProperty);
		set => SetValue(ShowAsMonochromeProperty, value);
	}

	public static DependencyProperty ShowAsMonochromeProperty { get; } =
		DependencyProperty.Register(nameof(ShowAsMonochrome), typeof(bool), typeof(BitmapIconSource), new FrameworkPropertyMetadata(true, OnPropertyChanged));

#if !HAS_UNO_WINUI
	private
#endif
	protected override IconElement CreateIconElementCore()
	{
		var bitmapIcon = new BitmapIcon();

		if (UriSource != null)
		{
			bitmapIcon.UriSource = UriSource;
		}

		if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.BitmapIcon", "ShowAsMonochrome"))
		{
			bitmapIcon.ShowAsMonochrome = ShowAsMonochrome;
		}

		if (Foreground != null)
		{
			bitmapIcon.Foreground = Foreground;
		}

		return bitmapIcon;
	}

#if !HAS_UNO_WINUI
	private
#endif
	protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty iconSourceProperty)
	{
		if (iconSourceProperty == ShowAsMonochromeProperty)
		{
			return BitmapIcon.ShowAsMonochromeProperty;
		}
		else if (iconSourceProperty == UriSourceProperty)
		{
			return BitmapIcon.UriSourceProperty;
		}

		return base.GetIconElementPropertyCore(iconSourceProperty);
	}
}
