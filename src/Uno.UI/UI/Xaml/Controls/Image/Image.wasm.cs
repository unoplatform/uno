using System;
using System.Globalization;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Windows.UI.Xaml.Controls
{
	public class HtmlImage : UIElement
	{
		public HtmlImage() : base("img")
		{
		}
	}

	partial class Image : FrameworkElement
	{
		private readonly SerialDisposable _sourceDisposable = new SerialDisposable();

		private readonly HtmlImage _htmlImage;
		private Size _lastMeasuredSize;

		private static readonly Size _zeroSize = new Size(0d, 0d);

		public Image()
		{
			_htmlImage = new HtmlImage();

			ImageOpened += OnImageOpened;
			ImageFailed += OnImageFailed;

			AddChild(_htmlImage);
		}

		private void OnImageFailed(object sender, RoutedEventArgs e)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Image failed [{(Source as BitmapSource)?.WebUri}]");
			}
		}

		private void OnImageOpened(object sender, RoutedEventArgs e)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Image opened [{(Source as BitmapSource)?.WebUri}]");
			}

			if (_lastMeasuredSize == _zeroSize)
			{
				// If the image size hasn't being calculated
				// (sometimes the measure 
				InvalidateMeasure();
			}
		}

		/// <summary>
		/// When set, the resulting image is tentatively converted to Monochrome.
		/// </summary>
		internal Color? MonochromeColor { get; set; }

		public event RoutedEventHandler ImageOpened
		{
			add => _htmlImage.RegisterEventHandler("load", value);
			remove => _htmlImage.UnregisterEventHandler("load", value);
		}

		public event RoutedEventHandler ImageFailed
		{
			add => _htmlImage.RegisterEventHandler("error", value);
			remove => _htmlImage.UnregisterEventHandler("error", value);
		}

		#region Source DependencyProperty

		public ImageSource Source
		{
			get => (ImageSource)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		// Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register("Source", typeof(ImageSource), typeof(Image), new PropertyMetadata(null, (s, e) => ((Image)s)?.OnSourceChanged(e)));


		private void OnSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateHitTest();

			var source = e.NewValue as ImageSource;

			_lastMeasuredSize = _zeroSize;

			if (source is WriteableBitmap wb)
			{
				void setImageContent()
				{
					if (wb.PixelBuffer is InMemoryBuffer mb)
					{
						var gch = GCHandle.Alloc(mb.Data, GCHandleType.Pinned);
						var pinnedData = gch.AddrOfPinnedObject();

						try
						{
							WebAssemblyRuntime.InvokeJS(
								"Uno.UI.WindowManager.current.setImageRawData(" + _htmlImage.HtmlId + ", " + pinnedData + ", " + wb.PixelWidth + ", " + wb.PixelHeight + ");"
							);

							InvalidateMeasure();
						}
						finally
						{
							gch.Free();
						}
					}
				}

				void OnInvalidated(object sdn, EventArgs args)
				{
					setImageContent();
				}

				wb.Invalidated += OnInvalidated;
				_sourceDisposable.Disposable = Disposable.Create(() => wb.Invalidated -= OnInvalidated);
				setImageContent();
			}
			else
			{
				void setImageContent()
				{
					var url = source?.WebUri;

					if (url != null)
					{
						if (url.IsAbsoluteUri)
						{
							if (url.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
							{
								// Local files are assumed as coming from the remoter server
								SetImageUrl(url.PathAndQuery);
							}
							else
							{
								SetImageUrl(url.AbsoluteUri);
							}
						}
						else
						{
							SetImageUrl(url.OriginalString);
						}
					}
				}

				_sourceDisposable.Disposable = null;

				_sourceDisposable.Disposable =
					Source?.RegisterDisposablePropertyChangedCallback(
						BitmapImage.UriSourceProperty, (o, args) =>
						{
							if (!object.Equals(e.OldValue, args.NewValue))
							{
								setImageContent();
							}
						}
					);

				setImageContent();
			}
		}

		private void SetImageUrl(string url)
		{
			if (MonochromeColor != null)
			{
				WebAssemblyRuntime.InvokeJS(
					"Uno.UI.WindowManager.current.setImageAsMonochrome(" + _htmlImage.HtmlId + ", \"" + url + "\", \"" + MonochromeColor.Value.ToCssString() + "\");"
				);
			}
			else
			{
				_htmlImage.SetAttribute("src", url);
			}
		}

		#endregion

		public Stretch Stretch
		{
			get { return (Stretch)this.GetValue(StretchProperty); }
			set { this.SetValue(StretchProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Stretch.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StretchProperty =
			DependencyProperty.Register("Stretch", typeof(Stretch), typeof(Image), new PropertyMetadata(Media.Stretch.Uniform, (s, e) =>
				((Image)s).OnStretchChanged((Stretch)e.NewValue, (Stretch)e.OldValue)));

		private void OnStretchChanged(Stretch newValue, Stretch oldValue)
		{
			InvalidateArrange();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			_lastMeasuredSize = _htmlImage.MeasureView(new Size(double.PositiveInfinity, double.PositiveInfinity));
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

		protected override Size ArrangeOverride(Size finalSize)
		{
			(double x, double y, double? width, double? height) getHtmlImagePosition()
			{
				var sourceRect = new Windows.Foundation.Rect(Windows.Foundation.Point.Zero, _lastMeasuredSize);
				var imageRect = new Windows.Foundation.Rect(Windows.Foundation.Point.Zero, finalSize);

				this.MeasureSource(imageRect, ref sourceRect);
				this.ArrangeSource(imageRect, ref sourceRect);

				switch (Stretch)
				{
					case Stretch.Fill:
					case Stretch.None:
						return (sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height);

					case Stretch.Uniform:
						if (finalSize.Width >= _lastMeasuredSize.Width)
						{
							// /---/-----\---\
							// |   |     |   |
							// |   |     |   |
							// |   |     |   |
							// |   |     |   |
							// |   |     |   |
							// \---\-----/---/

							return (sourceRect.X, sourceRect.Y, null, sourceRect.Height);
						}
						else
						{
							return (sourceRect.X, sourceRect.Y, sourceRect.Width, null);
						}

					case Stretch.UniformToFill:
						var adjustedSize = this.AdjustSize(finalSize, _lastMeasuredSize);

						if (adjustedSize.Height <= finalSize.Height)
						{
							//
							// /-------------\
							// |             |
							// /-------------\
							// |             |
							// |             |
							// |             |
							// |             |
							// \-------------/
							// |             |
							// \-------------/
							//
							
							return (sourceRect.X, sourceRect.Y, null, finalSize.Height);
						}
						else
						{
							return (sourceRect.X, sourceRect.Y, finalSize.Width, null);
						}

					default:
						throw new NotSupportedException();
				}
			}

			var position = getHtmlImagePosition();

			// Clip the image to the parent's arrange size.
			var clipRect = new Rect(0, 0, finalSize.Width, finalSize.Height);

			var arrangeRect = new Rect(
				position.x,
				position.y,
				position.width != null ? position.width.Value : double.NaN,
				position.height != null ? position.height.Value : double.NaN
			);

			_htmlImage.ArrangeElementNative(arrangeRect, clipRect);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Arrange {this} _lastMeasuredSize:{_lastMeasuredSize} clip:{clipRect} position:{position} finalSize:{finalSize}");
			}

			// Image has no direct child that needs to be arranged explicitly
			return finalSize;
		}

		internal override bool IsViewHit()
		{
			return Source != null || base.IsViewHit();
		}
	}
}
