using System;
using System.Linq;
using System.Numerics;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Windows.UI.Xaml.Controls
{
	partial class Image : FrameworkElement
	{
		private readonly SerialDisposable _sourceDisposable = new SerialDisposable();
		private Size _lastMeasuredSize;
		private SkiaCompositionSurface _currentSurface;
		private CompositionSurfaceBrush _surfaceBrush;
		private readonly SpriteVisual _imageSprite;

		// TODO: ImageOpened and ImageFailed should be implemented #9533
#pragma warning disable CS0067 // not used in skia
		public event RoutedEventHandler ImageOpened;
		public event ExceptionRoutedEventHandler ImageFailed;
#pragma warning restore CS0067

		public Image()
		{
			_imageSprite = Visual.Compositor.CreateSpriteVisual();
			Visual.Children.InsertAtTop(_imageSprite);
		}

		partial void OnSourceChanged(ImageSource newValue)
		{
			_sourceDisposable.Disposable = null;

			if (newValue is SvgImageSource svgImageSource)
			{
				InitializeSvgSource(svgImageSource);
			}
			else if (newValue is ImageSource source)
			{
				InitializeImageSource(source);
			}
		}

		private void InitializeSvgSource(SvgImageSource source)
		{
			_svgCanvas = source.GetCanvas();
			AddChild(_svgCanvas);
			_sourceDisposable.Disposable = Disposable.Create(() =>
			{
				RemoveChild(_svgCanvas);
				_svgCanvas = null;
			});
		}

		private void InitializeImageSource(ImageSource source)
		{
			_sourceDisposable.Disposable = source.Subscribe(img =>
			{
				_currentSurface = img.Value;
				_surfaceBrush = Visual.Compositor.CreateSurfaceBrush(_currentSurface);
				_imageSprite.Brush = _surfaceBrush;
				InvalidateMeasure();
			});
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (Source is SvgImageSource)
			{
				return MeasureSvgSource(availableSize);
			}
			else if (Source is ImageSource)
			{
				return MeasureImageSource(availableSize);
			}
			else
			{
				return default;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (Source is SvgImageSource)
			{
				return ArrangeSvgSource(finalSize);
			}
			else if (Source is ImageSource)
			{
				return ArrangeImageSource(finalSize);
			}
			else
			{
				return default;
			}
		}

		private Size MeasureSvgSource(Size availableSize)
		{
			return _lastMeasuredSize;
		}

		private Size MeasureImageSource(Size availableSize)
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

		private Size ArrangeSvgSource(Size finalSize)
		{
			var svgChild = GetChildren().FirstOrDefault();
			if (svgChild is null)
			{
				// SVG canvas not yet created
				return default;
			}

			svgChild.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
			return finalSize;
		}

		private Size ArrangeImageSource(Size finalSize)
		{
			if (_currentSurface?.Image != null)
			{
				// Calculate the resulting space required on screen for the image;
				var containerSize = this.MeasureSource(finalSize, _lastMeasuredSize);

				// Calculate the position of the image to follow stretch and alignment requirements
				var finalPosition = LayoutRound(this.ArrangeSource(finalSize, containerSize));

				_imageSprite.Size = LayoutRound(new Vector2((float)containerSize.Width, (float)containerSize.Height));
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
