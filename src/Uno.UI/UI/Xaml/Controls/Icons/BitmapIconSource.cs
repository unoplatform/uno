using System;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Controls;

public partial class BitmapIconSource : IconSource
{
	public BitmapIconSource()
	{
	}

	public Uri UriSource
	{
		get => (Uri)GetValue(UriSourceProperty);
		set => SetValue(UriSourceProperty, value);
	}

	public static DependencyProperty UriSourceProperty { get; } =
		DependencyProperty.Register(nameof(UriSource), typeof(Uri), typeof(BitmapIconSource), new FrameworkPropertyMetadata(default(Uri)));

	public bool ShowAsMonochrome
	{
		get => (bool)GetValue(ShowAsMonochromeProperty);
		set => SetValue(ShowAsMonochromeProperty, value);
	}

	public static DependencyProperty ShowAsMonochromeProperty { get; } =
		DependencyProperty.Register(nameof(ShowAsMonochrome), typeof(bool), typeof(BitmapIconSource), new FrameworkPropertyMetadata(true));

	public override IconElement CreateIconElement()
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
}
