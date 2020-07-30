using System;
using System.Globalization;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Windows.UI;
using Windows.UI.Composition;
using System.Numerics;

namespace Windows.UI.Xaml.Controls
{
	partial class Image : FrameworkElement
	{
		private readonly SerialDisposable _sourceDisposable = new SerialDisposable();
		private Size _lastMeasuredSize;
		private SkiaCompositionSurface _currentSurface;
		private CompositionSurfaceBrush _surfaceBrush;
		private readonly SpriteVisual _imageSprite;

		public event RoutedEventHandler ImageOpened;
		public event ExceptionRoutedEventHandler ImageFailed;

		public Image()
		{
			_imageSprite = Visual.Compositor.CreateSpriteVisual();
			Visual.Children.InsertAtTop(_imageSprite);
		}

		partial void OnSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is ImageSource source)
			{
				_sourceDisposable.Disposable = source.Subscribe(img =>
				{
					_currentSurface = img.Value;
					_surfaceBrush = Visual.Compositor.CreateSurfaceBrush(_currentSurface);
					_imageSprite.Brush = _surfaceBrush;
					InvalidateMeasure();
				});
			}
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (_currentSurface?.Image != null)
			{
				_lastMeasuredSize = new Size(_currentSurface.Image.Width, _currentSurface.Image.Height);

				Size ret;

				if (Source is BitmapSource bitmapSource)
				{
					bitmapSource.PixelWidth = (int)_lastMeasuredSize.Width;
					bitmapSource.PixelHeight = (int)_lastMeasuredSize.Height;
				}

				if (
					double.IsInfinity(availableSize.Width)
					&& double.IsInfinity(availableSize.Height)
				)
				{
					ret = _lastMeasuredSize;
				}
				else
				{
					ret = this.AdjustSize(availableSize, _lastMeasuredSize);
				}

				// Always making sure the ret size isn't bigger than the available size for an image with a fixed width or height
				ret = new Size(
					!Double.IsNaN(Width) && (ret.Width > availableSize.Width) ? availableSize.Width : ret.Width,
					!Double.IsNaN(Height) && (ret.Height > availableSize.Height) ? availableSize.Height : ret.Height
				);

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Measure {this} availableSize:{availableSize} measuredSize:{_lastMeasuredSize} ret:{ret} Stretch: {Stretch} Width:{Width} Height:{Height}");
				}

				return ret;
			}
			else
			{
				return default;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (_currentSurface?.Image != null)
			{
				// Calculate the resulting space required on screen for the image;
				var containerSize = this.MeasureSource(finalSize, _lastMeasuredSize);

				// Calculate the position of the image to follow stretch and alignment requirements
				var finalPosition = this.ArrangeSource(finalSize, containerSize);

				_imageSprite.Size = new Vector2((float)containerSize.Width, (float)containerSize.Height);
				_imageSprite.Offset = new Vector3((float)finalPosition.X, (float)finalPosition.Y, 0);

				var transform = Matrix3x2.CreateScale(_imageSprite.Size.X / _currentSurface.Image.Width, _imageSprite.Size.Y / _currentSurface.Image.Height);

				_surfaceBrush.TransformMatrix = transform;

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Arrange {this} _lastMeasuredSize:{_lastMeasuredSize} position:{finalPosition} finalSize:{finalSize}");
				}

				// Image has no direct child that needs to be arranged explicitly
				return finalSize;
			}
			else
			{
				_imageSprite.Size = default;
				return default;
			}
		}

	}
}
