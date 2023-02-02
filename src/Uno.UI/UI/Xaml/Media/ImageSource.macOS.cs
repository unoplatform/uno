using Foundation;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using Uno.Disposables;

using AppKit;
using Uno.UI.Xaml.Media;

namespace Windows.UI.Xaml.Media
{
	public partial class ImageSource
	{
		static ImageSource()
		{
		}

		protected ImageSource()
		{
			UseTargetSize = true;
			InitializeDownloader();
		}

		protected ImageSource(NSImage image)
		{
			_isOriginalSourceUIImage = true;
			_imageData = ImageData.FromNative(image);
		}

		internal ImageData ImageData => _imageData;

		internal string BundleName { get; private set; }
		internal string BundlePath { get; private set; }

		static public implicit operator ImageSource(NSImage image) => new ImageSource(image);

		private static NSImage OpenBundleFromString(string name)
		{
			if (!name.IsNullOrWhiteSpace())
			{
				var path = Path.Combine(NSBundle.MainBundle.ResourcePath, name);
				if (File.Exists(path))
					return new NSImage(path);
			}

			return null;
		}

		private ImageData OpenImageDataFromStream()
		{
			Stream.Position = 0;
			var nativeImage = NSImage.FromStream(Stream);

			if (nativeImage is not null)
			{
				return _imageData = ImageData.FromNative(nativeImage);
			}
			else
			{
				return ImageData.Empty;
			}
		}

		private async Task<ImageData> OpenImageDataFromFilePathAsync()
		{
			await using var file = File.OpenRead(FilePath);
			var nativeImage = NSImage.FromStream(file);

			_imageData = nativeImage is not null ?
				ImageData.FromNative(nativeImage) : ImageData.Empty;

			return _imageData;
		}

		private async Task<ImageData> OpenImageDataFromBundleAsync(CancellationToken ct)
		{
			await CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.Normal,
				() =>
				{
					_imageData = OpenBundle();
				}).AsTask(ct);

			return _imageData;
		}

		private async Task<ImageData> DownloadAndOpenImageDataAsync(CancellationToken ct)
		{
			if (Downloader == null)
			{
				await OpenUsingPlatformDownloader(ct);
			}
			else
			{
				var localFileUri = await Download(ct, AbsoluteUri);

				if (localFileUri == null)
				{
					return ImageData.Empty;
				}

				await CoreDispatcher.Main.RunAsync(
					CoreDispatcherPriority.Normal,
					() =>
					{
						var nativeImage = NSImage.ImageNamed(localFileUri.LocalPath);
						if (nativeImage is not null)
						{
							_imageData = ImageData.FromNative(nativeImage);
						}
						else
						{
							_imageData = ImageData.Empty;
						}
					}).AsTask(ct);
			}

			return _imageData;
		}

		/// <summary>
		/// Opens the targetted image for this image use using the native iOS image downloader, using NSUrl.
		/// </summary>
		private async Task OpenUsingPlatformDownloader(CancellationToken ct)
		{
			await CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.Normal,
				() =>
				{
					DownloadUsingPlatformDownloader();
				}
			).AsTask(ct);
		}

		private void DownloadUsingPlatformDownloader()
		{
			using (var url = new NSUrl(AbsoluteUri.AbsoluteUri))
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Loading image from [{AbsoluteUri.OriginalString}]");
				}

#pragma warning disable CS0618
				// fallback on the platform's loader
				using (var data = NSData.FromUrl(url, NSDataReadingOptions.Mapped, out var error))
#pragma warning restore CS0618
				{
					if (error != null)
					{
						this.Log().Error(error.LocalizedDescription);
					}
					else
					{
						var image = new NSImage(data);
						if (image is not null)
						{
							_imageData = ImageData.FromNative(image);
						}
						else
						{
							_imageData = ImageData.Empty;
						}
					}
				}
			}
		}
	}
}
