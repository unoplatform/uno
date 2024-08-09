// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ImageIcon.h, tag winui3/release/1.4.2

using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class ImageIcon
{
	public ImageSource Source
	{
		get => (ImageSource)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	public static DependencyProperty SourceProperty { get; } =
		DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(ImageIcon), new FrameworkPropertyMetadata(null, OnSourcePropertyChanged));


	private static void OnSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = sender as ImageIcon;
		owner?.OnSourcePropertyChanged(args);
	}
}
