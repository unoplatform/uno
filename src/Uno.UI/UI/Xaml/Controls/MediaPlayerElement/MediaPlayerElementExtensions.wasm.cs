﻿using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

public static class MediaPlayerElementExtensions
{
	public static DependencyProperty AnonymousCORSProperty { get; } =
	DependencyProperty.RegisterAttached(
		"AnonymousCORS",
		typeof(bool),
		typeof(MediaPlayerElementExtensions),
		new FrameworkPropertyMetadata(false, OnAnonymousCORSChanged));

	public static void SetAnonymousCORS(MediaPlayerElement d, bool value)
	{
		d.SetValue(AnonymousCORSProperty, value);
	}

	private static void OnAnonymousCORSChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is MediaPlayerElement mediaPlayer)
		{
			var anonymousCORSEnabled = (bool)e.NewValue;

			mediaPlayer?.MediaPlayer?.SetOption("AnonymousCORSEnabled", anonymousCORSEnabled);
		}
	}
}
