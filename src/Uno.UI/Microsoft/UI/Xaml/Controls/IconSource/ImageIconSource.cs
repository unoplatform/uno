// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ImageIconSource.cpp & ImageIconSource.properties.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class ImageIconSource : IconSource
{
	public ImageSource ImageSource
	{
		get => (ImageSource)GetValue(ImageSourceProperty);
		set => SetValue(ImageSourceProperty, value);
	}

	public static DependencyProperty ImageSourceProperty { get; } =
		DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(ImageIconSource), new FrameworkPropertyMetadata(null, OnPropertyChanged));

#if !HAS_UNO_WINUI
	private
#endif
	protected override IconElement CreateIconElementCore()
	{
		var imageIcon = new ImageIcon();
		if (ImageSource is { } imageSource)
		{
			imageIcon.Source = imageSource;
		}
		if (Foreground is { } newForeground)
		{
			imageIcon.Foreground = newForeground;
		}
		return imageIcon;
	}

#if !HAS_UNO_WINUI
	private
#endif
	protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty iconSourceProperty)
	{
		if (iconSourceProperty == ImageSourceProperty)
		{
			return ImageIcon.SourceProperty;
		}

		return base.GetIconElementPropertyCore(iconSourceProperty);
	}
}
