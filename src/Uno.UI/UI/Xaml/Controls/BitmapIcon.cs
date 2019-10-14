using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class BitmapIcon : IconElement
	{
		private Image _image;
		private Grid _grid;

		public BitmapIcon()
		{
			this.SetValue(ForegroundProperty, SolidColorBrushHelper.Black, DependencyPropertyValuePrecedences.Inheritance);

		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (_image == null)
			{
				_image = new Image
				{
					Stretch = Media.Stretch.Uniform
				};

				UpdateImageMonochromeColor();

				_image.SetBinding(
					dependencyProperty: Image.SourceProperty,
					binding: new Binding { Source = this, Path = nameof(UriSource) }
				);

				_grid = new Grid();
				_grid.Children.Add(_image);

				AddIconElementView(_grid);
			}


			return base.MeasureOverride(availableSize);
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

		private void OnShowAsMonochromeChanged(bool value) => RefreshImage();

		protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnForegroundChanged(e);
			RefreshImage();
		}

		private void RefreshImage()
		{
#if !NET461
			UpdateImageMonochromeColor();

			if (UriSource != null)
			{
				// Force a reload
				var tmp = UriSource;
				UriSource = null;
				UriSource = tmp;
			}
#endif
		}

		private void UpdateImageMonochromeColor()
		{
#if !NET461
			if (_image != null)
			{
				_image.MonochromeColor = ShowAsMonochrome ? (Foreground as SolidColorBrush)?.Color : null;
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
