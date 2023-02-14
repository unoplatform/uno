using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
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

		public static global::Microsoft.UI.Xaml.DependencyProperty ShowAsMonochromeProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				"ShowAsMonochrome", typeof(bool),
				typeof(global::Microsoft.UI.Xaml.Controls.BitmapIcon),
				new FrameworkPropertyMetadata(true, (s, e) => (s as BitmapIcon)?.OnShowAsMonochromeChanged((bool)e.NewValue)));

		private void OnShowAsMonochromeChanged(bool value) => UpdateImageMonochromeColor();

		protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnForegroundChanged(e);
			UpdateImageMonochromeColor();
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
