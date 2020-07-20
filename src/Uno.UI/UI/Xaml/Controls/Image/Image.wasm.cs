using System;
using System.IO;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Windows.UI;

namespace Windows.UI.Xaml.Controls
{
	public class HtmlImage : UIElement
	{
		public HtmlImage() : base("img")
		{
		}
	}

	partial class Image : FrameworkElement, ICustomClippingElement
	{
		private readonly SerialDisposable _sourceDisposable = new SerialDisposable();

		private readonly HtmlImage _htmlImage;
		private Size _lastMeasuredSize;

		private static readonly Size _zeroSize = new Size(0d, 0d);

		public Image()
		{
			_htmlImage = new HtmlImage();

			_htmlImage.SetAttribute("draggable", "false");

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

		public static DependencyProperty SourceProperty { get ; } =
			DependencyProperty.Register("Source", typeof(ImageSource), typeof(Image), new FrameworkPropertyMetadata(null, (s, e) => ((Image)s)?.OnSourceChanged(e)));

		private void OnSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateHitTest();

			_lastMeasuredSize = _zeroSize;

			if (e.NewValue is ImageSource source)
			{
				_sourceDisposable.Disposable = source.Subscribe(img =>
				{
					switch (img.Kind)
					{
						case ImageDataKind.Empty:
							_htmlImage.SetAttribute("src", "");
							break;

						case ImageDataKind.Base64:
						case ImageDataKind.Url:
						default:
							if (MonochromeColor != null)
							{
								WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.current.setImageAsMonochrome("
									+ _htmlImage.HtmlId + ", \""
									+ img.Value + "\", \""
									+ MonochromeColor.Value.ToHexString() + "\");");
							}
							else
							{
								_htmlImage.SetAttribute("src", img.Value);
							}
							break;

						case ImageDataKind.Error:
							_htmlImage.SetAttribute("src", "");
							_htmlImage.InternalDispatchEvent("error", EventArgs.Empty);
							break;
					}
				});
			}
			else
			{
				_htmlImage.SetAttribute("src", "");
			}
		}
		#endregion

		public static DependencyProperty StretchProperty { get ; } =
			DependencyProperty.Register(
				"Stretch",
				typeof(Stretch),
				typeof(Image),
				new FrameworkPropertyMetadata(
					Media.Stretch.Uniform,
					(s, e) => ((Image)s).OnStretchChanged((Stretch)e.NewValue, (Stretch)e.OldValue)));

		public Stretch Stretch
		{
			get => (Stretch)GetValue(StretchProperty);
			set => SetValue(StretchProperty, value);
		}

		private void OnStretchChanged(Stretch newValue, Stretch oldValue) => InvalidateArrange();

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

			_htmlImage.ArrangeElementNative(finalPosition, false, clipRect: null);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Arrange {this} _lastMeasuredSize:{_lastMeasuredSize} position:{finalPosition} finalSize:{finalSize}");
			}

			// Image has no direct child that needs to be arranged explicitly
			return finalSize;
		}

		internal override bool IsViewHit() => Source != null || base.IsViewHit();

		bool ICustomClippingElement.AllowClippingToLayoutSlot => true;

		bool ICustomClippingElement.ForceClippingToLayoutSlot => true;
	}
}
