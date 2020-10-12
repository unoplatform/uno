using System;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
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
			DependencyProperty.Register(nameof(UriSource), typeof(Uri), typeof(BitmapIconSource), new PropertyMetadata(default(Uri)));

		public bool ShowAsMonochrome
		{
			get => (bool)GetValue(ShowAsMonochromeProperty);
			set => SetValue(ShowAsMonochromeProperty, value);
		}

		public static DependencyProperty ShowAsMonochromeProperty { get; } =
			DependencyProperty.Register(nameof(ShowAsMonochrome), typeof(bool), typeof(BitmapIconSource), new PropertyMetadata(default(bool)));
	}
}
