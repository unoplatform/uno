using System.Collections.Generic;

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
			if (mediaPlayer?.MediaPlayer?.Player is { } player)
			{
				//Current MediaElement is initialized, so apply value directly to it
				player.SetAnonymousCORS(anonymousCORSEnabled);
				return;
			}
			else
			{
				//Wait until current MediaElement is initialized and then configure CORS
				RoutedEventHandler handler = null;
				handler = (sender, args) =>
				{
					var mediaPlayer = (MediaPlayerElement)sender;
					if (mediaPlayer.MediaPlayer?.Player is { } player)
					{
						player.SetAnonymousCORS(anonymousCORSEnabled);
					}

					mediaPlayer.Loaded -= handler;
				};

				mediaPlayer.Loaded += handler;
			}
		}
	}
}
