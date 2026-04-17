using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class Image : FrameworkElement
	{
		private readonly SerialDisposable _sourceDisposable = new SerialDisposable();
		private Size _lastMeasuredSize;
		private SkiaCompositionSurface _currentSurface;
		private CompositionSurfaceBrush _surfaceBrush;
		private readonly SpriteVisual _imageSprite;
		private ImageData? _pendingImageData;

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
			_pendingImageData = null;
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
			source.OpenFailed += OnSvgSourceFailed;
			_sourceDisposable.Disposable = Disposable.Create(() =>
			{
				RemoveChild(_svgCanvas);
				source.SourceLoaded -= OnSvgSourceLoaded;
				source.OpenFailed -= OnSvgSourceFailed;
				_svgCanvas = null;
			});
		}

		private void OnSvgSourceLoaded(object sender, EventArgs args)
		{
			InvalidateMeasure();
			ImageOpened?.Invoke(this, new RoutedEventArgs(this));
		}

		private void OnSvgSourceFailed(SvgImageSource sender, SvgImageSourceFailedEventArgs args)
		{
			InvalidateMeasure();
			ImageFailed?.Invoke(this, new ExceptionRoutedEventArgs(this, "Failed to load Svg source"));
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
			_pendingImageData = null;
			if (currentData is null)
			{
				// No image data is pending
				return;
			}

			var processedData = currentData.Value;

			if (processedData.HasData)
			{
				_currentSurface = processedData.CompositionSurface;
				_surfaceBrush = Visual.Compositor.CreateSurfaceBrush(_currentSurface);
				_surfaceBrush.MonochromeColor = MonochromeColor?.ToSKColor();
				_imageSprite.Brush = _surfaceBrush;
				ImageOpened?.Invoke(this, new RoutedEventArgs(this));
			}
			else if (processedData is { Kind: ImageDataKind.Empty })
			{
				// Ensure the previous content is unloaded
				_currentSurface = null;
				_imageSprite.Brush = null;
			}
			else if (processedData is { Kind: ImageDataKind.Error })
			{
				ImageFailed?.Invoke(this, new(
					this,
					processedData.Error?.Message ?? "Unknown error"));
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (IsSourceReady())
			{
				// Calculate the resulting space required on screen for the image;
				var containerSize = this.MeasureSource(finalSize, _lastMeasuredSize);

				var roundedSize = LayoutRound(new Vector2((float)containerSize.Width, (float)containerSize.Height));

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Arrange {this} _lastMeasuredSize:{_lastMeasuredSize}, containerSize:{containerSize}, finalSize:{finalSize}");
				}

				if (Source is SvgImageSource)
				{
					_svgCanvas?.Arrange(new Rect(0, 0, roundedSize.X, roundedSize.Y));
					return containerSize;
				}
				else
				{
					_imageSprite.Size = roundedSize;

					var transform = Matrix3x2.CreateScale(_imageSprite.Size.X / _currentSurface.Image.Width, _imageSprite.Size.Y / _currentSurface.Image.Height);

					_surfaceBrush.TransformMatrix = transform;

					// Image has no direct child that needs to be arranged explicitly
					return containerSize;
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

		/// <summary>
		/// Returns a mask that represents the alpha channel of the image as a CompositionBrush.
		/// This brush can be used with CompositionMaskBrush or DropShadow.Mask to create shaped effects.
		/// </summary>
		/// <returns>A CompositionBrush representing the image as an alpha mask.</returns>
		public CompositionBrush GetAlphaMask()
		{
			var compositor = Compositor.GetSharedCompositor();
			var surface = new AlphaMaskSurface(compositor, Visual);
			var brush = compositor.CreateSurfaceBrush(surface);
			brush.Stretch = CompositionStretch.None;
			return brush;
		}
	}
}
