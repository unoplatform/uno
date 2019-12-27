#if !NET461 && !__WASM__
using Uno.Extensions;
using Uno.Diagnostics.Eventing;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Uno.Logging;
using Microsoft.Extensions.Logging;

#if XAMARIN_IOS
using UIKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class Image : DependencyObject
	{
		/// <summary>
		/// Setting this flag instructs the image control not to dispose pending image fetches when it is removed from the visual tree. 
		/// This should generally be left false, but may be required in cases that the image is rapidly unloaded and reloaded, or that 
		/// OnUnloaded/OnDetachedFromWindow is improperly called when the view isn't really being removed, and performance/stability is affected.
		/// </summary>
		public bool PreserveStateOnUnload { get; set; } = false;

		private readonly static IEventProvider _imageTrace = Tracing.Get(TraceProvider.Id);

		private readonly SerialDisposable _imageFetchDisposable = new SerialDisposable();
		private readonly SerialDisposable _sourceDisposable = new SerialDisposable();

		//Set just as image source is going to be set (which may be dispatched)
		private ImageSource _openedImage;
		//Set after image source fetch has successfully resolved
		private ImageSource _successfullyOpenedImage;

		private bool? _hasFiniteBounds;
		private Size _layoutSize;

		private ImageLayouter _layouter;

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{15E13473-560E-4601-86FF-C9E1EDB73701}");

			public const int Image_SetSourceStart = 1;
			public const int Image_SetSourceStop = 2;
			public const int Image_SetUriStart = 3;
			public const int Image_SetUriStop = 4;
			public const int Image_SetImageStart = 5;
			public const int Image_SetImageStop = 6;
		}

		void Initialize()
		{
			InitializeBinder();
			IFrameworkElementHelper.Initialize(this);

			_layouter = new ImageLayouter(this);
		}

		public event RoutedEventHandler ImageOpened;
		public event ExceptionRoutedEventHandler ImageFailed;

		/// <summary>
		/// When set, the resulting image is tentatively converted to Monochrome.
		/// </summary>
		internal Color? MonochromeColor { get; set; }

		protected virtual void OnImageFailed(ImageSource imageSource)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug(this.ToString() + " Image failed to open");
			}

			ImageFailed?.Invoke(this, new ExceptionRoutedEventArgs(this, "Image failed to download"));
		}

		protected virtual void OnImageOpened(ImageSource imageSource)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug(this.ToString() + " Image opened successfully");
			}

			ImageOpened?.Invoke(this, new RoutedEventArgs(this));
			_successfullyOpenedImage = imageSource;
		}

		#region Stretch
		public Stretch Stretch
		{
			get { return (Stretch)this.GetValue(StretchProperty); }
			set { this.SetValue(StretchProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Stretch.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StretchProperty =
			DependencyProperty.Register("Stretch", typeof(Stretch), typeof(Image), new PropertyMetadata(Stretch.Uniform, (s, e) =>
			((Image)s).OnStretchChanged((Stretch)e.NewValue, (Stretch)e.OldValue)));

		partial void OnStretchChanged(Stretch newValue, Stretch oldValue);
		#endregion

		#region Source
		public ImageSource Source
		{
			get { return (ImageSource)this.GetValue(SourceProperty); }
			set { this.SetValue(SourceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register(
				"Source",
				typeof(ImageSource),
				typeof(Image),
				new PropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => ((Image)s).OnSourceChanged((ImageSource)e.OldValue, (ImageSource)e.NewValue))
				);

		private void OnSourceChanged(ImageSource oldValue, ImageSource newValue)
		{
			if (newValue is WriteableBitmap wb)
			{
				wb.Invalidated += OnInvalidated;
				_sourceDisposable.Disposable = Disposable.Create(() => wb.Invalidated -= OnInvalidated);

				void OnInvalidated(object sdn, EventArgs args)
				{
					_openedImage = null;
					TryOpenImage();
				}
			}
			else
			{
				_sourceDisposable.Disposable =
					Source?.RegisterDisposablePropertyChangedCallback(
						BitmapImage.UriSourceProperty, (o, e) =>
						{
							if (!object.Equals(e.OldValue, e.NewValue))
							{
								_openedImage = null;
								TryOpenImage();
							}
						}
					);
			}

			TryOpenImage();
		}

		#endregion

		partial void OnLoadedPartial()
		{
			TryOpenImage();
		}

		partial void OnUnloadedPartial()
		{
			//If PreserveStateOnUnload is set, don't cancel pending image downloads/fetches. See comment on PreserveStateOnUnload.
			if (PreserveStateOnUnload)
			{
				return;
			}

			_imageFetchDisposable.Disposable = null;
			if (_successfullyOpenedImage != _openedImage)
			{
				//Dispatched image fetch did not resolve, so we force it to be rescheduled next time TryOpenImage is called
				_openedImage = null;
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
		private bool ShouldDowngradeLayoutRequest()
		{
			return CanDowngradeLayoutRequest && !double.IsNaN(Width) && !double.IsNaN(Height);
		}

		private void Dispatch(Func<CancellationToken, Task> handler)
		{
			var cd = new CancellationDisposable();

			Dispatcher.RunAsync(
				Core.CoreDispatcherPriority.Normal,
				() => handler(cd.Token)
			).AsTask(cd.Token);

			_imageFetchDisposable.Disposable = cd;
		}

		private void Execute(Func<CancellationToken, Task> handler)
		{
			var cd = new CancellationDisposable();

			var dummy = handler(cd.Token);

			_imageFetchDisposable.Disposable = cd;
		}

		private double GetKnownWidth(double stretchedWidth, double fallbackIfInfinite)
		{
			return double.IsNaN(Width) ? (double.IsInfinity(stretchedWidth) ? fallbackIfInfinite : stretchedWidth) : Width;
		}

		private double GetKnownHeight(double stretchedHeight, double fallbackIfInfinite)
		{
			return double.IsNaN(Height) ? (double.IsInfinity(stretchedHeight) ? fallbackIfInfinite : stretchedHeight) : Height;
		}

		partial void SetTargetImageSize(Size targetSize);

		public override string ToString()
		{
			return base.ToString() + ";Source={0}".InvariantCultureFormat(Source?.ToString() ?? "[null]");
		}

#if __ANDROID__ || __IOS__
		private AutomationPeer OnCreateAutomationPeerOverride()
		{
			return new ImageAutomationPeer(this);
		}

		private string GetAccessibilityInnerTextOverride()
		{
			return null;
		}
#else
		protected AutomationPeer OnCreateAutomationPeer()
		{
			return new ImageAutomationPeer(this);
		}
#endif

		private partial class ImageLayouter : Layouter
		{
			private Image ImageControl => Panel as Image;

			public ImageLayouter(Image image) : base(image)
			{
			}
			protected override string Name => Panel.Name;

			protected override Size MeasureOverride(Size availableSize)
			{
				var img = ImageControl;
				var sourceSize = img.SourceImageSize;

				if (sourceSize == default)
				{
					return default;
				}

				if (img.Stretch == Stretch.None)
				{
					// On Stretch=None, we simply use the image size
					// without considering the availableSize.

					var size = img._layoutSize = img.ApplySizeConstraints(sourceSize);
					ImageControl._hasFiniteBounds = double.IsFinite(size.Width) && double.IsFinite(size.Height);
					return size;
				}

				// Get real available size after applying local constrains
				var constrainedAvailableSize = img.ApplySizeConstraints(availableSize);
				img._layoutSize = constrainedAvailableSize;

				var isWidthDefined = double.IsFinite(constrainedAvailableSize.Width);
				var isHeightDefined = double.IsFinite(constrainedAvailableSize.Height);

				var aspectRatio = sourceSize.AspectRatio();

				if (isWidthDefined && isHeightDefined)
				{
					// If both available width & available height are known here
					if (img.Stretch != Stretch.Uniform) // Fill or UniformToFill
					{
						// Fill & UniformToFill will both take all the available size
						return constrainedAvailableSize;
					}

					// Apply the Stretch=Uniform logic...

					var containerSize = img.MeasureSource(availableSize, sourceSize);

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug(Panel.ToString() + $" measuring with Stretch.Uniform with availableSize={constrainedAvailableSize}, returning desiredSize={containerSize}");
					}

					ImageControl._hasFiniteBounds = true;
					return containerSize;
				}

				ImageControl._hasFiniteBounds = false;

				if (!isWidthDefined && !isHeightDefined)
				{
					// If both width & height are unspecified, we simply apply the constrains on image
					// size and use that as measurement for the layout.
					return img.ApplySizeConstraints(sourceSize);
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
					var knownWidth = img.GetKnownWidth(constrainedAvailableSize.Width, sourceSize.Width);
					var desiredSize = new Size();
					switch (img.Stretch)
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

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug(Panel.ToString() + $" measuring with knownWidth={knownWidth} with availableSize={constrainedAvailableSize}, returning desiredSize={desiredSize}");
					}

					return desiredSize;
				}
				if (isHeightDefined)
				{
					var knownHeight = img.GetKnownHeight(constrainedAvailableSize.Height, sourceSize.Height);
					var desiredSize = new Size();
					switch (img.Stretch)
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

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug(Panel.ToString() + $" measuring with knownHeight={knownHeight} with availableSize={constrainedAvailableSize}, returning desiredSize={desiredSize}");
					}

					return desiredSize;
				}

				throw new InvalidOperationException("Should never reach here.");
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug(Panel.ToString() + $" arranging with finalSize={finalSize}");
				}

				//If we are given a non-zero size to draw into, set the target dimensions to load the image with accordingly
				ImageControl.SetTargetImageSize(finalSize);

				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					if (ImageControl._openedImage != null)
					{
						var renderedSize = finalSize.LogicalToPhysicalPixels();
						var loadedSize = ImageControl.SourceImageSize.LogicalToPhysicalPixels();

						if (((renderedSize.Width + 512) < loadedSize.Width ||
							(renderedSize.Height + 512) < loadedSize.Height) && ImageControl.Source.UseTargetSize)
						{
							this.Log().Warn("The image was opened with a size of {0} and is displayed using a size of only {1}. Try optimizing the image size by using a smaller source or not using Stretch.Uniform or using fixed Width and Height."
								.InvariantCultureFormat(loadedSize, renderedSize));
						}
					}
				}

#if __ANDROID__
				// Images on UWP are always clipped to the control's boundaries.
				var physicalSize = finalSize.LogicalToPhysicalPixels();
				ImageControl.ClipBounds = new Android.Graphics.Rect(0, 0, (int)physicalSize.Width, (int)physicalSize.Height);
#endif

				return finalSize;
			}
		}
	}
}
#endif
