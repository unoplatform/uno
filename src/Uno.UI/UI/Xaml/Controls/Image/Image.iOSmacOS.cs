using System;
using System.Threading;
using System.Threading.Tasks;
using CoreGraphics;
#if __IOS__
using _UIImage = UIKit.UIImage;
#elif __MACOS__
using _UIImage = AppKit.NSImage;
using AppKit;
#endif
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using CoreAnimation;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class Image
	{
		private Size _sourceImageSize;

		/// <summary>
		/// The size of the native image data
		/// </summary>
		private Size SourceImageSize
		{
			get => _sourceImageSize;
			set
			{
				_sourceImageSize = value;

				if (Source is BitmapSource bitmapSource)
				{
					bitmapSource.PixelWidth = (int)_sourceImageSize.Width;
					bitmapSource.PixelHeight = (int)_sourceImageSize.Height;
				}
			}
		}

		public Image() { }

		private void TryOpenImage()
		{
			//Skip opening the image source source is already loaded or if the view isn't loaded
			if (_openedImage == Source)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug(this.ToString() + " TryOpenImage - cancelling because Source has not changed");
				}
				return;
			}

			if (Window == null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug(this.ToString() + " TryOpenImage - cancelling because view is not loaded");
				}
				return;
			}

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug(this.ToString() + " TryOpenImage - proceeding");
			}

			using (
				_imageTrace.WriteEventActivity(
					TraceProvider.Image_SetSourceStart,
					TraceProvider.Image_SetSourceStop,
					new object[] { this.GetDependencyObjectId() }
				)
			)
			{
				_openedImage = Source;

				if (_openedImage == null || !_openedImage.HasSource())
				{
					_native?.Reset();
					SetNeedsLayoutOrDisplay();
					_imageFetchDisposable.Disposable = null;
				}
				else if (_openedImage.TryOpenSync(out var img))
				{
					SetImage(img);
					_imageFetchDisposable.Disposable = null;
				}
				else
				{
					// The Jupiter behavior is to reset the visual right away, displaying nothing
					// then show the new image. We're rescheduling the work below, so there is going
					// to be a visual blank displayed.
					TryCreateNative();
					_native.Reset();

					Func<CancellationToken, Task> scheduledFetch = async (ct) =>
					{
						using (
						   _imageTrace.WriteEventActivity(
							   TraceProvider.Image_SetUriStart,
							   TraceProvider.Image_SetUriStop,
							   new object[] { this.GetDependencyObjectId() }
						   )
						)
						{
							//_openedImage could be set to null while trying to access it on the thread pool
							var image = await Task.Run(() => _openedImage?.Open(ct));

							//if both image and _openedImage are null this is ok just return;
							//otherwise call SetImage with null which will raise the OnImageFailed event
							if (ct.IsCancellationRequested ||
								(image == null && _openedImage == null))
							{
								return;
							}

							SetImage(image);
						}
					};

					Execute(scheduledFetch);
				}
			}
		}

		private void SetImage(_UIImage image)
		{
			using (
				_imageTrace.WriteEventActivity(
					TraceProvider.Image_SetImageStart,
					TraceProvider.Image_SetImageStop,
					new object[] { this.GetDependencyObjectId() }
				)
			)
			{
				if (MonochromeColor != null)
				{
					image = image.AsMonochrome(MonochromeColor.Value);
				}

				TryCreateNative();

				_native.SetImage(image);

				SourceImageSize = image?.Size.ToFoundationSize() ?? default(Size);
			}

			InvalidateMeasure();

			if (_native.HasImage)
			{
				OnImageOpened(image);
			}
			else
			{
				OnImageFailed(image);
			}
		}

		private void TryCreateNative()
		{
			if (_native == null)
			{
				_native = new NativeImage();

				AddSubview(_native);

				UpdateContentMode(Stretch);
			}
		}

		private void SetNeedsLayoutOrDisplay()
		{
			if (ShouldDowngradeLayoutRequest())
			{
				this.SetNeedsDisplay();
			}
			else
			{
				InvalidateMeasure();
			}
		}
	}
}

