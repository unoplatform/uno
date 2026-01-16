// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference BitmapIconSource_Partial.cpp, tag winui3/release/1.4.2

using System;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

	protected override IconElement CreateIconElementCore()
	{
		Uri uriSource = UriSource;
		bool showAsMonochrome = ShowAsMonochrome;

		var bitmapIcon = new BitmapIcon();

		bitmapIcon.UriSource = UriSource;
		bitmapIcon.ShowAsMonochrome = ShowAsMonochrome;

		return bitmapIcon;
	}

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
		else
		{
			return base.GetIconElementPropertyCore(iconSourceProperty);
		}
	}
}
