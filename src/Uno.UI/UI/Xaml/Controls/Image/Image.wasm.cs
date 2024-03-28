using System;
using System.IO;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;
using Windows.UI;

namespace Windows.UI.Xaml.Controls
{
	partial class Image : FrameworkElement
	{
		private readonly SerialDisposable _sourceDisposable = new SerialDisposable();

		private readonly HtmlImage _htmlImage;
		private Size _lastMeasuredSize;

		private static readonly Size _zeroSize = new Size(0d, 0d);
		private ImageData _currentImg;

		public Image()
		{
			_htmlImage = new HtmlImage();

			_htmlImage.SetStyle("visibility", "hidden");

			ImageOpened += OnImageOpened;
			ImageFailed += OnImageFailed;

			AddChild(_htmlImage);
		}

		/// <summary>
		/// Occurs when the image source is downloaded and decoded with no failure. You can use this event to determine the natural size of the image source.
		/// </summary>
		public event RoutedEventHandler ImageOpened
		{
			add => _htmlImage.RegisterEventHandler("load", value, GenericEventHandlers.RaiseRoutedEventHandler);
			remove => _htmlImage.UnregisterEventHandler("load", value, GenericEventHandlers.RaiseRoutedEventHandler);
		}

		/// <summary>
		/// Occurs when there is an error associated with image retrieval or format.
		/// </summary>		
		public event ExceptionRoutedEventHandler ImageFailed
		{
			add => _htmlImage.RegisterEventHandler("error", value, GenericEventHandlers.RaiseExceptionRoutedEventHandler, payloadConverter: ImageFailedConverter);
			remove => _htmlImage.UnregisterEventHandler("error", value, GenericEventHandlers.RaiseExceptionRoutedEventHandler);
		}

		private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Image failed [{_currentImg.Source}]: {e.ErrorMessage}");
			}

			_htmlImage.SetStyle("visibility", "hidden");

			_currentImg.Source?.ReportImageFailed(e.ErrorMessage);
		}

		private void OnImageOpened(object sender, RoutedEventArgs e)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Image opened [{(Source as BitmapSource)?.AbsoluteUri}]");
			}

			_htmlImage.SetStyle("visibility", "visible");

			if (_lastMeasuredSize == _zeroSize)
			{
				// If the image size hasn't being calculated
				// (sometimes the measure 
				InvalidateMeasure();
			}
			_currentImg.Source?.ReportImageLoaded();
		}

		private ExceptionRoutedEventArgs ImageFailedConverter(object sender, string e)
			=> new ExceptionRoutedEventArgs(sender, e);

		partial void OnSourceChanged(ImageSource newValue, bool forceReload)
		{
			UpdateHitTest();

			_lastMeasuredSize = _zeroSize;
			// Hide the old image until the new image is loaded. This is the behaviour on WinUI.
			// Attempting to set src to "" will incorrectly raise ImageFailed
			_htmlImage.SetStyle("visibility", "hidden");
			_currentImg = default;

			if (newValue is ImageSource source)
			{
				void OnSourceOpened(ImageData img)
				{
					_currentImg = img;
					switch (img.Kind)
					{
						case ImageDataKind.Empty:
							_htmlImage.SetAttribute("src", "");
							break;

						case ImageDataKind.DataUri:
						case ImageDataKind.Url:
						default:
							if (MonochromeColor != null)
							{
								WindowManagerInterop.SetImageAsMonochrome(_htmlImage.HtmlId, img.Value, MonochromeColor.Value.ToHexString());
							}
							else
							{
								_htmlImage.SetAttribute("src", img.Value);
							}

							break;

						case ImageDataKind.Error:
							_htmlImage.SetAttribute("src", "");
							var errorArgs = new ExceptionRoutedEventArgs(this, img.Error?.ToString());
							_htmlImage.InternalDispatchEvent("error", errorArgs);
							break;
					}
				}

				_sourceDisposable.Disposable = null;
				_sourceDisposable.Disposable = source.Subscribe(OnSourceOpened);
			}
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
			// Calculate the resulting space required on screen for the image
			var containerSize = this.MeasureSource(finalSize, _lastMeasuredSize);

			// Calculate the position of the image to follow stretch and alignment requirements
			var finalPosition = this.ArrangeSource(finalSize, containerSize);

			_htmlImage.ArrangeVisual(finalPosition, clipRect: null);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Arrange {this} _lastMeasuredSize:{_lastMeasuredSize} position:{finalPosition} finalSize:{finalSize}");
			}

			// Image has no direct child that needs to be arranged explicitly
			return finalSize;
		}
	}
}
