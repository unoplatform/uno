// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BitmapIconSource.cpp, commit 083796a

using System;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
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

		private protected override IconElement CreateIconElementCore()
		{
			var bitmapIcon = new BitmapIcon();

			if (UriSource != null)
			{
				bitmapIcon.UriSource = UriSource;
			}

			if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Controls.BitmapIcon", "ShowAsMonochrome"))
			{
				bitmapIcon.ShowAsMonochrome = ShowAsMonochrome;
			}

			if (Foreground != null)
			{
				bitmapIcon.Foreground = Foreground;
			}

			return bitmapIcon;
		}

		private protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty sourceProperty)
		{
			if (sourceProperty == ShowAsMonochromeProperty)
			{
				return BitmapIcon.ShowAsMonochromeProperty;
			}
			else if (sourceProperty == UriSourceProperty)
			{
				return BitmapIcon.UriSourceProperty;
			}

			return base.GetIconElementPropertyCore(sourceProperty);
		}
	}
}
