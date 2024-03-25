using System;
using System.Linq;
using System.Numerics;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Microsoft.UI.Xaml.Controls
{
	partial class Image : FrameworkElement
	{
		private readonly SerialDisposable _sourceDisposable = new SerialDisposable();
		private Size _lastMeasuredSize;
		private SkiaCompositionSurface _currentSurface;
		private CompositionSurfaceBrush _surfaceBrush;
		private readonly SpriteVisual _imageSprite;
		private ImageData _pendingImageData;

		public Image()
		{
			_imageSprite = Visual.Compositor.CreateSpriteVisual();
			Visual.Children.InsertAtTop(_imageSprite);
		}

		partial void OnSourceChanged(ImageSource newValue, bool forceReload)
		{
			// We clear the old image first. We do NOT wait for the new image to load
			// for it to replace the old one. This is what happens on WinUI.
			_sourceDisposable.Disposable = null;
			_lastMeasuredSize = default;
			_imageSprite.Brush = null;
			_currentSurface = null;
			_pendingImageData = new();
			InvalidateMeasure();

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
			source.SourceLoaded += OnSvgSourceLoaded;
			_sourceDisposable.Disposable = Disposable.Create(() =>
			{
				RemoveChild(_svgCanvas);
				source.SourceLoaded -= OnSvgSourceLoaded;
				_svgCanvas = null;
			});
		}

		private void OnSvgSourceLoaded(object sender, EventArgs args)
		{
			InvalidateMeasure();
		}

		private void InitializeImageSource(ImageSource source)
		{
			_sourceDisposable.Disposable = source.Subscribe(img =>
			{
				_pendingImageData = img;

				InvalidateMeasure();
			});
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			TryProcessPendingSource();

			if (IsSourceReady())
			{
				_lastMeasuredSize = GetSourceSize();

				Size ret;

				if (Source is BitmapImage bitmapImage)
				{
					bitmapImage.PixelWidth = (int)_lastMeasuredSize.Width;
					bitmapImage.PixelHeight = (int)_lastMeasuredSize.Height;
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

		private void TryProcessPendingSource()
		{
			var currentData = _pendingImageData;
			_pendingImageData = new();
			if (currentData.HasData)
			{
				_currentSurface = currentData.CompositionSurface;
				_surfaceBrush = Visual.Compositor.CreateSurfaceBrush(_currentSurface);
				_surfaceBrush.UsePaintColorToColorSurface = MonochromeColor is not null;
				_imageSprite.SetPaintColor(MonochromeColor);
				_imageSprite.Brush = _surfaceBrush;
				ImageOpened?.Invoke(this, new RoutedEventArgs(this));
			}
			else if (currentData is { Kind: ImageDataKind.Error })
			{
				ImageFailed?.Invoke(this, new(
					this,
					currentData.Error?.Message ?? "Unknown error"));
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (IsSourceReady())
			{
				// Calculate the resulting space required on screen for the image;
				var containerSize = this.MeasureSource(finalSize, _lastMeasuredSize);

				// Calculate the position of the image to follow stretch and alignment requirements
				var finalPosition = LayoutRound(this.ArrangeSource(finalSize, containerSize));

				var roundedSize = LayoutRound(new Vector2((float)containerSize.Width, (float)containerSize.Height));

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Arrange {this} _lastMeasuredSize:{_lastMeasuredSize} position:{finalPosition} finalSize:{finalSize}");
				}

				if (Source is SvgImageSource)
				{
					_svgCanvas?.Arrange(new Rect(finalPosition.X, finalPosition.Y, roundedSize.X, roundedSize.Y));
					return finalSize;
				}
				else
				{
					_imageSprite.Size = roundedSize;
					_imageSprite.Offset = new Vector3((float)finalPosition.X, (float)finalPosition.Y, 0);

					var transform = Matrix3x2.CreateScale(_imageSprite.Size.X / _currentSurface.Image.Width, _imageSprite.Size.Y / _currentSurface.Image.Height);

					_surfaceBrush.TransformMatrix = transform;

					// Image has no direct child that needs to be arranged explicitly
					return finalSize;
				}
			}
			else
			{
				_imageSprite.Size = default;
				return ArrangeFirstChild(finalSize);
			}
		}

		private bool IsSourceReady()
		{
			if (Source is SvgImageSource svgImageSource)
			{
				return svgImageSource.IsParsed;
			}
			else if (Source is ImageSource imageSource)
			{
				return _currentSurface?.Image != null;
			}

			return false;
		}

		private Size GetSourceSize()
		{
			if (Source is SvgImageSource svgImageSource)
			{
				return svgImageSource.SourceSize;
			}
			else
			{
				return new Size(_currentSurface.Image.Width, _currentSurface.Image.Height);
			}
		}
	}
}
