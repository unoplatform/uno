using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class Image : FrameworkElement
	{
		private ImageSource.Subscription _subscription;

		// Changes whenever _subscription is set or released, so a Subscribe that the app superseded meanwhile is noticed.
		private int _subscriptionVersion;
		private Size _lastMeasuredSize;
		private ICompositionSurface _currentSurface;
		private CompositionSurfaceBrush _surfaceBrush;
		private readonly SpriteVisual _imageSprite;
		private ImageData? _pendingImageData;
		private int _pendingImageVersion;

		// The SourceVersion of Source that ImageOpened was raised for: WinUI raises it once per source, so the data
		// delivered again when the Image re-enters the tree, or decoded again after its release, raises nothing.
		private int? _imageOpenedVersion;

		public Image()
		{
			_imageSprite = Visual.Compositor.CreateSpriteVisual();
			Visual.Children.InsertAtTop(_imageSprite);
		}

		partial void OnSourceChanged(ImageSource newValue, bool forceReload)
		{
			// We clear the old image first. We do NOT wait for the new image to load
			// for it to replace the old one. This is what happens on WinUI.
			// The sprite lets go of the surface before the subscription can release it.
			_imageSprite.Brush = null;
			_currentSurface = null;
			_pendingImageData = null;
			_imageOpenedVersion = null;
			_subscription?.Applied(ImageData.Empty);
			ReleaseSubscription();
			_lastMeasuredSize = default;
			InvalidateMeasure();

			// SVG flows through the same neutral path as any source: its ISvgRenderer produces a live composition
			// surface (drawn as vector each frame, crisp at any size) that the sprite renders like any bitmap.
			// Subscribing is what decodes the source, so an unloaded Image (e.g. a recycled container whose binding
			// updates off-tree) waits for OnLoaded, as WinUI decodes only once the element is in the live tree.
			if (newValue is ImageSource source && IsLoaded)
			{
				InitializeImageSource(source);
			}
		}

		private void InitializeImageSource(ImageSource source)
		{
			// Back in the tree, the sprite still draws _currentSurface (see OnUnloaded). What it is given is drawn once
			// measured, and acknowledged then (TryProcessPendingSource).
			var version = ++_subscriptionVersion;
			var shown = _currentSurface;
			var subscription = source.Subscribe(
				img =>
				{
					_pendingImageData = img;
					_pendingImageVersion = source.SourceVersion;

					InvalidateMeasure();
				},
				shown: shown,
				appliedOnDelivery: false);

			if (version == _subscriptionVersion)
			{
				_subscription = subscription;
				return;
			}

			// Subscribing ran app code (opening a completed cache entry raises BitmapImage.ImageOpened at once) that changed
			// the source or unloaded this Image, which by now has the subscription it needs, if any.
			if (!ReferenceEquals(_currentSurface, shown))
			{
				subscription.Applied(ImageData.Empty);
			}

			subscription.Dispose();
		}

		private void ReleaseSubscription()
		{
			_subscriptionVersion++;
			_subscription?.Dispose();
			_subscription = null;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			TryProcessPendingSource();

			if (GetSourceSize() is { } sourceSize)
			{
				_lastMeasuredSize = sourceSize;

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
			var version = _pendingImageVersion;
			_pendingImageData = null;
			if (currentData is null)
			{
				// No image data is pending
				return;
			}

			var processedData = currentData.Value;

			// The sprite draws this from now on: what it drew before can go once nothing else draws it.
			_subscription?.Applied(processedData);

			if (processedData.HasData)
			{
				_currentSurface = processedData.CompositionSurface;
				_surfaceBrush = Visual.Compositor.CreateSurfaceBrush(_currentSurface);
				_surfaceBrush.MonochromeColor = MonochromeColor;
				_imageSprite.Brush = _surfaceBrush;

				if (_imageOpenedVersion != version)
				{
					_imageOpenedVersion = version;
					ImageOpened?.Invoke(this, new RoutedEventArgs(this));
				}
			}
			else if (processedData is { Kind: ImageDataKind.Empty })
			{
				// Ensure the previous content is unloaded
				_currentSurface = null;
				_imageSprite.Brush = null;
			}
			else if (processedData is { Kind: ImageDataKind.Error })
			{
				// A source that failed to open shows nothing, not the image it had before: WinUI drops its surfaces
				// (MUX Reference: CImageSource::FireImageFailed, imagesource.cpp), as the source releases data it owns.
				_currentSurface = null;
				_imageSprite.Brush = null;

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

				_imageSprite.Size = roundedSize;

				// A live (self-painting) surface fills the sprite bounds itself, so the brush transform is a no-op for
				// it; a texture-backed surface is stretched from its pixel size to the sprite size here.
				var srcSize = GetSurfaceSize(_currentSurface);
				if (srcSize is { X: > 0, Y: > 0 } size)
				{
					_surfaceBrush.TransformMatrix = Matrix3x2.CreateScale(_imageSprite.Size.X / size.X, _imageSprite.Size.Y / size.Y);
				}

				// Image has no direct child that needs to be arranged explicitly
				return containerSize;
			}
			else
			{
				_imageSprite.Size = default;
				return ArrangeFirstChild(finalSize);
			}
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			// The subscription was released on unload: subscribe again. A source that kept its data (shared through the
			// cache, or not yet released) delivers it at once; one that released it decodes it again.
			if (_subscription is null && Source is { } source)
			{
				InitializeImageSource(source);
			}
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			// Released so the source can let go of its pixels once they have waited (PendingImageReleases). The sprite keeps
			// its brush: it is not drawn off the tree, and an Image back in time still has them. Data not yet drawn is dropped.
			_pendingImageData = null;
			ReleaseSubscription();
		}

		partial void OnMonochromeColorChanged()
		{
			if (_surfaceBrush is not null)
			{
				// When the image is already loaded, just update the color filter on the brush
				// instead of reloading the entire image. This avoids flicker.
				_surfaceBrush.MonochromeColor = MonochromeColor;
			}
			else
			{
				OnSourceChanged(Source, forceReload: true);
			}
		}

		private bool IsSourceReady() => GetSourceSize() is not null;

		private Size? GetSourceSize()
		{
			if (Source is not ImageSource source)
			{
				return null;
			}

			// Pixels the source released because nothing displayed them keep their size until it delivers them again (a
			// cache-off URI downloads again), so an auto-sized Image doesn't collapse meanwhile.
			return (GetSurfaceSize(_currentSurface) ?? source.ReleasedPixelSize) is { } size ? new Size(size.X, size.Y) : null;
		}

		// Pixel/intrinsic size of the current surface, whichever kind it is: a texture-backed CompositionImageSurface
		// or a live self-painting IPaintableSurface (e.g. the SVG surface).
		private static Vector2? GetSurfaceSize(ICompositionSurface surface) => surface switch
		{
			CompositionImageSurface image => image.Size,
			IPaintableSurface paintable => paintable.Size,
			_ => null
		};

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
