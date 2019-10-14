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
using Uno.Logging;
using Windows.UI.Core;
using Uno.Disposables;

using AppKit;

namespace Windows.UI.Xaml.Media
{
	public partial class ImageSource
	{
		private readonly bool _isOriginalSourceUIImage;

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
			ImageData = image;
		}

		internal NSImage ImageData { get; private set; }
		internal string BundleName { get; private set; }
		internal string BundlePath { get; private set; }

		static public implicit operator ImageSource(NSImage image)
		{
			return new ImageSource(image);
		}

		public bool HasSource()
		{
			return Stream != null
				|| WebUri != null
				|| FilePath.HasValueTrimmed()
				|| ImageData != null
				|| HasBundle;
		}

		/// <summary>
		/// Determines if the current instance references a local bundle resource.
		/// </summary>
		public bool HasBundle => BundlePath.HasValueTrimmed() || BundleName.HasValueTrimmed();

		/// <summary>
		/// Open bundle is using either the name of the bundle (for 
		/// android compatibility), or the path to the bundle for Windows compatibility.
		/// </summary>
		internal NSImage OpenBundle()
		{
			ImageData = OpenBundleFromString(BundleName) ?? OpenBundleFromString(BundlePath);

			if (ImageData == null)
			{
				this.Log().ErrorFormat("Unable to locate bundle resource [{0}]", BundlePath ?? BundleName);
			}

			return ImageData;
		}

		private static NSImage OpenBundleFromString(string bundle)
		{
			if (bundle.HasValueTrimmed())
			{
				return NSImage.ImageNamed(bundle);
			}

			return null;
		}

		internal async Task<NSImage> Open(CancellationToken ct)
		{
			using (
			   _trace.WriteEventActivity(
				   TraceProvider.ImageSource_SetImageDecodeStart,
				   TraceProvider.ImageSource_SetImageDecodeStop,
				   new object[] { this.GetDependencyObjectId() }
			   )
			)
			{
				if (ct.IsCancellationRequested)
				{
					return null;
				}

				if (Stream != null)
				{
					Stream.Position = 0;
					return ImageData = NSImage.FromStream(Stream);
				}

				if (FilePath.HasValue())
				{
					using (var file = File.OpenRead(FilePath))
					{
						return ImageData = NSImage.FromStream(file);
					}
				}

				if (HasBundle)
				{
					await CoreDispatcher.Main.RunAsync(
						CoreDispatcherPriority.Normal,
						async () =>
						{
							ImageData = OpenBundle();
						}
					);

					return ImageData;
				}

				if (Downloader == null)
				{
					await OpenUsingPlatformDownloader(ct);
				}
				else
				{
					var localFileUri = await Download(ct, WebUri);

					if (localFileUri == null)
					{
						return null;
					}

					await CoreDispatcher.Main.RunAsync(
						CoreDispatcherPriority.Normal,
						async () =>
						{
							ImageData = NSImage.ImageNamed(localFileUri.LocalPath);
						}).AsTask(ct);
				}

				return ImageData;
			}
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
			using (var url = new NSUrl(WebUri.OriginalString))
			{
				NSError error;

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Loading image from [{WebUri.OriginalString}]");
				}

				// fallback on the platform's loader
				using (var data = NSData.FromUrl(url, NSDataReadingOptions.Coordinated, out error))
				{
					if (error != null)
					{
						this.Log().Error(error.LocalizedDescription);
					}
					else
					{
						ImageData = new NSImage(data);
					}
				}
			}
		}

		/// <summary>
		/// Similar to Dispose, but the ImageSource can still be used in the future.
		/// </summary>
		internal void UnloadImageData()
		{
			// If the original source is a NSImage, we can't dispose it because we will
			// not be able to restore it later (from a WebUri, BundleName, file path, etc.)
			if (!_isOriginalSourceUIImage)
			{
				DisposeUIImage();
			}
		}

		partial void InitFromResource(Uri uri)
		{
			var path = uri
				.PathAndQuery
				.TrimStart(new[] { '/' })

				// UWP supports backward slash in path for directory separators.
				.Replace("\\", "/");

			BundlePath = path;

			BundleName = uri != null
				? Path.GetFileName(uri.AbsolutePath)
				: null;
		}

		partial void DisposePartial()
		{
			DisposeUIImage();
		}

		private void DisposeUIImage()
		{
			if (ImageData != null)
			{
				ImageData.Dispose();
				ImageData = null;
			}
		}

		public override string ToString()
		{
			var source = Stream ?? WebUri ?? FilePath ?? (object)ImageData ?? BundlePath ?? BundleName ?? "[No source]";
			return "ImageSource: {0}".InvariantCultureFormat(source);
		}
	}
}
