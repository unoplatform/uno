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
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.Xaml.Media;
using Uno.Disposables;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class Image
	{
		private SerialDisposable _childViewDisposable = new SerialDisposable();

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

				if (Source is BitmapImage bitmapImage)
				{
					bitmapImage.PixelWidth = (int)_sourceImageSize.Width;
					bitmapImage.PixelHeight = (int)_sourceImageSize.Height;
				}
			}
		}

		public Image() { }

		partial void OnStretchChanged(Stretch newValue, Stretch oldValue)
		{
			UpdateContentMode(newValue);
		}

		private void TryOpenImage(bool forceReload = false)
		{
			//Skip opening the image source source is already loaded or if the view isn't loaded
			if (!forceReload && _openedSource == Source)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug(this.ToString() + " TryOpenImage - cancelling because Source has not changed");
				}
				return;
			}

			if (Window == null)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug(this.ToString() + " TryOpenImage - cancelling because view is not loaded");
				}
				return;
			}

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
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
				_openedSource = Source;

				if (_openedSource == null || !_openedSource.HasSource())
				{
					_nativeImageView?.Reset();
					SetNeedsLayoutOrDisplay();
					_imageFetchDisposable.Disposable = null;
				}
				else if (_openedSource.TryOpenSync(out var imageData))
				{
					SetImageData(imageData);
					_imageFetchDisposable.Disposable = null;
				}
				else
				{
					// The Jupiter behavior is to reset the visual right away, displaying nothing
					// then show the new image. We're rescheduling the work below, so there is going
					// to be a visual blank displayed.
					TryCreateNativeImageView();
					_nativeImageView.Reset();

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
							var imageData = await Task.Run(() => _openedSource?.Open(ct));

							//if both image and _openedImage are null this is ok just return;
							//otherwise call SetImage with null which will raise the OnImageFailed event
							if (ct.IsCancellationRequested ||
								(!imageData.HasData && _openedSource is null))
							{
								return;
							}

							SetImageData(imageData);
						}
					};

					Execute(scheduledFetch);
				}
			}
		}

		private void SetImageData(ImageData imageData)
		{
			using (
			_imageTrace.WriteEventActivity(
				TraceProvider.Image_SetImageStart,
				TraceProvider.Image_SetImageStop,
				new object[] { this.GetDependencyObjectId() }))
			{

				if (_openedSource is SvgImageSource svgImageSource && imageData.Kind == ImageDataKind.ByteArray)
				{
					SetSvgSource(svgImageSource, imageData.ByteArray);
				}
				else if (_openedSource is { } source && imageData.Kind == ImageDataKind.NativeImage)
				{
					SetNativeImage(source, imageData.NativeImage);
				}
				else
				{
					SetNativeImage(null, null);
				}
			}

			InvalidateMeasure();

			if (imageData.HasData)
			{
				OnImageOpened(_openedSource);
			}
			else
			{
				OnImageFailed(_openedSource, imageData.Error);
			}
		}

		private void SetSvgSource(SvgImageSource svgImageSource, byte[] byteArray)
		{
			_childViewDisposable.Disposable = null;

			_svgCanvas = svgImageSource.GetCanvas();
			AddSubview(_svgCanvas);

			_childViewDisposable.Disposable = Disposable.Create(() =>
			{
				_svgCanvas.RemoveFromSuperview();
				_svgCanvas = null;
			});

			SourceImageSize = svgImageSource.SourceSize;
		}

		private void SetNativeImage(ImageSource imageSource, _UIImage image)
		{
			using (
				_imageTrace.WriteEventActivity(
					TraceProvider.Image_SetImageStart,
					TraceProvider.Image_SetImageStop,
					new object[] { this.GetDependencyObjectId() }
				)
			)
			{
				if (MonochromeColor != null && image != null)
				{
					image = image.AsMonochrome(MonochromeColor.Value);
				}

				TryCreateNativeImageView();

				_nativeImageView.SetImage(image);

				SourceImageSize = image?.Size.ToFoundationSize() ?? default(Size);
			}
		}

		private void TryCreateNativeImageView()
		{
			if (_nativeImageView is null)
			{
				_childViewDisposable.Disposable = null;
				var imageView = new NativeImageView();
				_nativeImageView = imageView;

				AddSubview(_nativeImageView);

				UpdateContentMode(Stretch);

				_childViewDisposable.Disposable = Disposable.Create(() =>
				{
					imageView.RemoveFromSuperview();
					_nativeImageView = null;
				});
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

