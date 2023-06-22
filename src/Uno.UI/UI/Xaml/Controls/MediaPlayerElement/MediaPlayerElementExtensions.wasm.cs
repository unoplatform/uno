using System.Collections.Generic;
using Uno.UI.Helpers;

namespace Windows.UI.Xaml.Controls;

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

			// TODO: Rename "SetOption" to "SetAnonymousCORSEnabled" and let it take a single boolean.
			mediaPlayer?.MediaPlayer?.SetOption("AnonymousCORSEnabled", Boxes.Box(anonymousCORSEnabled));
		}
	}
}
