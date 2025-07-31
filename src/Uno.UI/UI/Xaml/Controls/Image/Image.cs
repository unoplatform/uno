#if !IS_UNIT_TESTS && !UNO_REFERENCE_API
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Uno.Diagnostics.Eventing;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Image : FrameworkElement
	{
		/// <summary>
		/// Setting this flag instructs the image control not to dispose pending image fetches when it is removed from the visual tree.
		/// This should generally be left false, but may be required in cases that the image is rapidly unloaded and reloaded, or that
		/// OnUnloaded/OnDetachedFromWindow is improperly called when the view isn't really being removed, and performance/stability is affected.
		/// </summary>
		public bool PreserveStateOnUnload { get; set; }

		private readonly static IEventProvider _imageTrace = Tracing.Get(TraceProvider.Id);

		private readonly SerialDisposable _imageFetchDisposable = new SerialDisposable();
		private readonly SerialDisposable _sourceDisposable = new SerialDisposable();

		//Set just as image source is going to be set (which may be dispatched)
		private ImageSource _openedSource;

		//Set after image source fetch has successfully resolved
		private ImageSource _successfullyOpenedImage;

		private bool? _hasFiniteBounds;
		private Size _layoutSize;

		private NativeImageView _nativeImageView;

		public static new class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{15E13473-560E-4601-86FF-C9E1EDB73701}");

			public const int Image_SetSourceStart = 1;
			public const int Image_SetSourceStop = 2;
			public const int Image_SetUriStart = 3;
			public const int Image_SetUriStop = 4;
			public const int Image_SetImageStart = 5;
			public const int Image_SetImageStop = 6;
		}

		private protected void OnImageFailed(ImageSource imageSource, Exception exception)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				if (exception is null)
				{
					this.Log().Debug($"Image '{this}' failed to open");
				}
				else
				{
					this.Log().Debug($"Image '{this}' failed to open: {exception}");
				}
			}

#if !__SKIA__ && !__WASM__ // TODO: Have consistent handling on Wasm and Skia.
			if (imageSource is BitmapImage bitmapImage && exception is not null)
			{
				bitmapImage.RaiseImageFailed(exception);
			}
#endif

			if (exception is null)
			{
				ImageFailed?.Invoke(this, new ExceptionRoutedEventArgs(this, "Image failed to download"));
			}
			else
			{
				ImageFailed?.Invoke(this, new ExceptionRoutedEventArgs(this, "Image failed to download: " + exception.ToString()));
			}
		}

		private void OnImageOpened(ImageSource imageSource)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug(this.ToString() + " Image opened successfully");
			}

#if !__SKIA__ && !__WASM__ // TODO: Have consistent handling on Wasm and Skia.
			if (imageSource is BitmapImage bitmapImage)
			{
				bitmapImage.RaiseImageOpened();
			}
#endif

			ImageOpened?.Invoke(this, new RoutedEventArgs(this));
			_successfullyOpenedImage = imageSource;
		}

		partial void OnStretchChanged(Stretch newValue, Stretch oldValue);

		private void OnSourceChanged(ImageSource newValue, bool forceReload = false)
		{
			if (Source is null)
			{
				_sourceDisposable.Disposable = null;
			}
			else if (newValue is WriteableBitmap wb)
			{
				wb.Invalidated += OnInvalidated;
				_sourceDisposable.Disposable = Disposable.Create(() => wb.Invalidated -= OnInvalidated);

				void OnInvalidated()
				{
					_openedSource = null;
					TryOpenImage();
				}
			}
			else if (newValue is SvgImageSource svgImageSource)
			{
				var compositeDisposable = new CompositeDisposable();
				compositeDisposable.Add(
					Source?.RegisterDisposablePropertyChangedCallback(
						SvgImageSource.UriSourceProperty, (o, e) =>
						{
							if (!object.Equals(e.OldValue, e.NewValue))
							{
								_openedSource = null;
								TryOpenImage(true);
							}
						}
				));
				svgImageSource.StreamLoaded += ForceReloadSource;
				compositeDisposable.Add(() => svgImageSource.StreamLoaded -= ForceReloadSource);

				_sourceDisposable.Disposable = compositeDisposable;
			}
			else
			{
				var compositeDisposable = new CompositeDisposable();
				compositeDisposable.Add(
					Source?.RegisterDisposablePropertyChangedCallback(
						BitmapImage.UriSourceProperty, (o, e) =>
						{
							if (!object.Equals(e.OldValue, e.NewValue))
							{
								_openedSource = null;
								TryOpenImage();
							}
						}
					));

				if (Source is BitmapSource bitmapSource)
				{
					bitmapSource.StreamLoaded += ForceReloadSource;
					compositeDisposable.Add(() => bitmapSource.StreamLoaded -= ForceReloadSource);
				}

				_sourceDisposable.Disposable = compositeDisposable;
			}

			TryOpenImage(forceReload);
		}

		private void ForceReloadSource(object sender, EventArgs args)
		{
			_openedSource = null;
			TryOpenImage(true);
		}

		internal override bool IsViewHit() => Source?.HasSource() ?? false;

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			OnSourceChanged(Source, false);
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			//If PreserveStateOnUnload is set, don't cancel pending image downloads/fetches. See comment on PreserveStateOnUnload.
			if (PreserveStateOnUnload)
			{
				return;
			}

			_imageFetchDisposable.Disposable = null;
			_sourceDisposable.Disposable = null;
			if (_successfullyOpenedImage != _openedSource)
			{
				//Dispatched image fetch did not resolve, so we force it to be rescheduled next time TryOpenImage is called
				_openedSource = null;
			}
		}

		/// <summary>
		/// Gets or sets whether we should allow to downgrade a request for remeasure and layout to redraw the image only.
		/// This value can be set to false as a workaround to issues caused by optimizations introduced with ShouldDowngradeLayoutRequest.
		/// Default value: true
		/// </summary>
		public bool CanDowngradeLayoutRequest { get; set; } = true;

		/// <summary>
		/// Check whether we should downgrade a request for remeasure and layout to a request to redraw the image only,
		/// ie because we know that the image's dimensions have not changed after setting the source.
		/// </summary>
		/// <returns>True if we know that the image's dimensions have not changed.</returns>
		internal bool ShouldDowngradeLayoutRequest()
		{
			return CanDowngradeLayoutRequest && !double.IsNaN(Width) && !double.IsNaN(Height);
		}

		private async void Execute(Func<CancellationToken, Task> handler)
		{
			var cd = new CancellationDisposable();
			_imageFetchDisposable.Disposable = cd;

			try
			{
				await handler(cd.Token);
			}
			catch (Exception ex)
			{
				this.Log().LogError("Failed executing async operation.", ex);
			}
		}

		/// <summary>
		/// True if horizontally stretched within finite container, or defined by this.Width
		/// </summary>
		private bool HasKnownWidth(double availableWidth) => !double.IsNaN(Width) ||
			(HorizontalAlignment == HorizontalAlignment.Stretch && !double.IsInfinity(availableWidth));

		/// <summary>
		/// True if vertically stretched within finite container, or defined by this.Height
		/// </summary>
		private bool HasKnownHeight(double availableHeight) => !double.IsNaN(Height) ||
			(VerticalAlignment == VerticalAlignment.Stretch && !double.IsInfinity(availableHeight));

		private double GetKnownWidth(double stretchedWidth, double fallbackIfInfinite)
		{
			return double.IsNaN(Width) ? (double.IsInfinity(stretchedWidth) ? fallbackIfInfinite : stretchedWidth) : Width;
		}

		private double GetKnownHeight(double stretchedHeight, double fallbackIfInfinite)
		{
			return double.IsNaN(Height) ? (double.IsInfinity(stretchedHeight) ? fallbackIfInfinite : stretchedHeight) : Height;
		}

		partial void SetTargetImageSize(Size targetSize);
		partial void UpdateArrangeSize(Size arrangeSize);

		public override string ToString()
		{
			return base.ToString() + ";Source={0}".InvariantCultureFormat(Source?.ToString() ?? "[null]");
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug(ToString() + $" measuring with availableSize={availableSize}");
			}

			SetTargetImageSize(availableSize);

			var size = InnerMeasureOverride(availableSize);

			if (_svgCanvas is not null)
			{
				_svgCanvas.Measure(availableSize);
			}

			return size;
		}

		private Size InnerMeasureOverride(Size availableSize)
		{
			var sourceSize = SourceImageSize;

			if (sourceSize == default)
			{
				// Setting _hasFiniteBounds here is important if the Source hasn't been set or fetched yet
				_hasFiniteBounds = HasKnownWidth(availableSize.Width) && HasKnownHeight(availableSize.Height);
				return default;
			}

			if (Stretch == Stretch.None)
			{
				// On Stretch=None, we simply use the image size
				// without considering the availableSize.

				var size = _layoutSize = this.ApplySizeConstraints(sourceSize);
				_hasFiniteBounds = double.IsFinite(size.Width) && double.IsFinite(size.Height);
				return size;
			}

			// Get real available size after applying local constrains
			var constrainedAvailableSize = this.ApplySizeConstraints(availableSize);
			_layoutSize = constrainedAvailableSize;

			var isWidthDefined = double.IsFinite(constrainedAvailableSize.Width);
			var isHeightDefined = double.IsFinite(constrainedAvailableSize.Height);

			var aspectRatio = sourceSize.AspectRatio();

			if (isWidthDefined && isHeightDefined)
			{
				// If both available width & available height are known here
				_hasFiniteBounds = true;

				if (Stretch != Stretch.Uniform) // Fill or UniformToFill
				{
					// Fill & UniformToFill will both take all the available size
					return constrainedAvailableSize;
				}

				// Apply the Stretch=Uniform logic...

				var containerSize = this.MeasureSource(availableSize, sourceSize);

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug(ToString() + $" measuring with Stretch.Uniform with availableSize={constrainedAvailableSize}, returning desiredSize={containerSize}");
				}

				return containerSize;
			}

			_hasFiniteBounds = false;

			if (!isWidthDefined && !isHeightDefined)
			{
				// If both width & height are unspecified, we simply apply the constrains on image
				// size and use that as measurement for the layout.
				return this.ApplySizeConstraints(sourceSize);
			}

			// If one dimension is known and the other isn't, we need to consider uniformity based on the dimension we know.
			// Example: Horizontal=Stretch, Vertical=Top, Stretch=Uniform, SourceWidth=200, SourceHeight=100 (AspectRatio=2)
			//			This Image is Inside a StackPanel (infinite height and width=300).
			//			When being measured, the height can be calculated using the aspect ratio of the source image and the available width.
			//			That means the Measure should return
			//						height = (KnownWidth=300) / (AspectRatio=2) = 150
			//			...and not	height = (SourceHeight=100) = 100
			if (isWidthDefined)
			{
				var knownWidth = GetKnownWidth(constrainedAvailableSize.Width, sourceSize.Width);
				var desiredSize = new Size();
				switch (Stretch)
				{
					case Stretch.Uniform:
						// If sourceSize is empty, aspect ratio is undefined so we return 0.
						// Since apsect ratio can have a lot of decimal, iOS ceils Image size to 0.5 if it's not a precise size (like 111.111111111)
						// so the desiredSize will never match the actual size causing an infinite measuring and can freeze the app
						desiredSize.Width = knownWidth;
						desiredSize.Height = sourceSize == default(Size) ? 0 : Math.Ceiling((knownWidth / aspectRatio) * 2) / 2;
						break;
					case Stretch.None:
						desiredSize.Width = sourceSize.Width;
						desiredSize.Height = sourceSize.Height;
						break;
					case Stretch.Fill:
					case Stretch.UniformToFill:
						desiredSize.Width = knownWidth;
						desiredSize.Height = double.IsInfinity(constrainedAvailableSize.Height) ? sourceSize.Height : constrainedAvailableSize.Height;
						break;
				}

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug(ToString() + $" measuring with knownWidth={knownWidth} with availableSize={constrainedAvailableSize}, returning desiredSize={desiredSize}");
				}

				return desiredSize;
			}
			if (isHeightDefined)
			{
				var knownHeight = GetKnownHeight(constrainedAvailableSize.Height, sourceSize.Height);
				var desiredSize = new Size();
				switch (Stretch)
				{
					case Stretch.Uniform:
						//If sourceSize is empty, aspect ratio is undefined so we return 0
						// Since apsect ratio can have a lot of decimal, iOS ceils Image size to 0.5 if it's not a precise size (like 111.111111111)
						// so the desiredSize will never match the actual size causing an infinite measuring and can freeze the app
						desiredSize.Width = sourceSize == default(Size) ? 0 : Math.Ceiling(knownHeight * aspectRatio * 2) / 2;
						desiredSize.Height = knownHeight;
						break;
					case Stretch.None:
						desiredSize.Width = sourceSize.Width;
						desiredSize.Height = sourceSize.Height;
						break;
					case Stretch.Fill:
					case Stretch.UniformToFill:
						desiredSize.Width = double.IsInfinity(constrainedAvailableSize.Width) ? sourceSize.Width : constrainedAvailableSize.Width;
						desiredSize.Height = knownHeight;
						break;
				}

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug(ToString() + $" measuring with knownHeight={knownHeight} with availableSize={constrainedAvailableSize}, returning desiredSize={desiredSize}");
				}

				return desiredSize;
			}

			throw new InvalidOperationException("Should never reach here.");
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug(ToString() + $" arranging with finalSize={finalSize}");
			}

			//If we are given a non-zero size to draw into, set the target dimensions to load the image with accordingly
			UpdateArrangeSize(finalSize);
			SetTargetImageSize(finalSize);

			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				if (_openedSource != null)
				{
					var renderedSize = finalSize.LogicalToPhysicalPixels();
					var loadedSize = SourceImageSize.LogicalToPhysicalPixels();

					if (((renderedSize.Width + 512) < loadedSize.Width ||
						(renderedSize.Height + 512) < loadedSize.Height) && !Source.UseTargetSize)
					{
						this.Log().Warn("The image was opened with a size of {0} and is displayed using a size of only {1}. Try optimizing the image size by using a smaller source or not using Stretch.Uniform or using fixed Width and Height."
							.InvariantCultureFormat(loadedSize, renderedSize));
					}
				}
			}

#if __APPLE_UIKIT__ || __ANDROID__
			if (Source is SvgImageSource svgImageSource && _svgCanvas is not null)
			{
#if __ANDROID__
				ClipBounds = null;
#endif
				// Calculate the resulting space required on screen for the image;
				var containerSize = this.MeasureSource(finalSize, svgImageSource.SourceSize);

				// Calculate the position of the image to follow stretch and alignment requirements
				var finalPosition = LayoutRound(this.ArrangeSource(finalSize, containerSize));
				var roundedSize = LayoutRound(new Vector2((float)containerSize.Width, (float)containerSize.Height));

				_svgCanvas.Arrange(new Rect(finalPosition.X, finalPosition.Y, roundedSize.X, roundedSize.Y));
				_svgCanvas.Clip = new RectangleGeometry() { Rect = new Rect(0, 0, finalSize.Width, finalSize.Height) };
				return finalSize;
			}
#endif

#if __ANDROID__
			// Images on UWP are always clipped to the control's boundaries.
			var physicalSize = finalSize.LogicalToPhysicalPixels();
			ClipBounds = new ARect(0, 0, (int)physicalSize.Width, (int)physicalSize.Height);

			_lastLayoutSize = finalSize;

			// Try opening the image in the case where UseTargetSize has been set, as now
			// we have both _targetWidth and _targetWidth that have been set.
			try
			{
				_isInLayout = true;
				TryOpenImage();
			}
			finally
			{
				_isInLayout = false;
			}
#endif

			return ArrangeFirstChild(finalSize);
		}
	}
}
#endif
