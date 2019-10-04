using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class BitmapIcon : IconElement
	{
		private readonly Image _image;
		private readonly Grid _grid;

		public BitmapIcon()
		{
			Foreground = SolidColorBrushHelper.Black;

			_image = new Image {
				Stretch = Media.Stretch.Uniform,
#if !NET461
				MonochromeColor = (Foreground as SolidColorBrush)?.Color
#endif
			};

			_image.SetBinding(
				dependencyProperty: Image.SourceProperty,
				binding: new Binding { Source = this, Path = nameof(UriSource) }
			);

			_grid = new Grid();
			_grid.Children.Add(_image);

			AddIconElementView(_grid);
		}

		public bool ShowAsMonochrome
		{
			get => (bool)GetValue(ShowAsMonochromeProperty);
			set => SetValue(ShowAsMonochromeProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty ShowAsMonochromeProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"ShowAsMonochrome", typeof(bool),
				typeof(global::Windows.UI.Xaml.Controls.BitmapIcon),
				new FrameworkPropertyMetadata(true, (s, e) => (s as BitmapIcon)?.OnShowAsMonochromeChanged((bool)e.NewValue)));

		private void OnShowAsMonochromeChanged(bool value)
		{
#if !NET461
				_image.MonochromeColor = value ? (Foreground as SolidColorBrush)?.Color : null;

				if (!value)
				{
					// Force a reload
					var tmp = UriSource;
					UriSource = null;
					UriSource = tmp;
				}
#endif
		}

		public Uri UriSource
		{
			get => (Uri)GetValue(UriSourceProperty);
			set => SetValue(UriSourceProperty, value);
		}

		public static DependencyProperty UriSourceProperty { get; } =
			DependencyProperty.Register(
				"UriSource",
				typeof(Uri),
				typeof(BitmapIcon),
				new FrameworkPropertyMetadata(default(Uri))
			);
	}
}
