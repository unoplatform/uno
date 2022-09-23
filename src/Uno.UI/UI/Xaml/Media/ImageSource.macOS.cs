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
			_imageData = ImageData.FromNative(image);
		}

		private protected ImageData _imageData;

		internal ImageData ImageData => _imageData;

		internal string BundleName { get; private set; }
		internal string BundlePath { get; private set; }

		static public implicit operator ImageSource(NSImage image)
		{
			return new ImageSource(image);
		}

		public bool HasSource()
		{
			return IsSourceReady
				|| Stream != null
				|| AbsoluteUri != null
				|| FilePath.HasValueTrimmed()
				|| _imageData.HasData
				|| HasBundle;
		}

		/// <summary>
		/// Determines if the current instance references a local bundle resource.
		/// </summary>
		public bool HasBundle => BundlePath.HasValueTrimmed() || BundleName.HasValueTrimmed();

		/// <summary>
		/// Open the image from the app's main bundle.
		/// </summary>
		internal ImageData OpenBundle()
		{
			var image = OpenResourceFromString(BundlePath) ?? OpenResourceFromString(BundleName);

			if (image is not null)
			{
				_imageData = ImageData.FromNative(image);
			}
			else
			{
				_imageData = ImageData.Empty;
			}

			if (!_imageData.HasData)
			{
				this.Log().ErrorFormat("Unable to locate bundle resource [{0}]", BundlePath ?? BundleName);
			}

			return _imageData;
		}

		private static NSImage OpenResourceFromString(string name)
		{
			if (name.HasValueTrimmed())
			{
				var path = Path.Combine(NSBundle.MainBundle.ResourcePath, name);
				if (File.Exists(path))
					return new NSImage(path);
			}

			return null;
		}

		/// <summary>
		/// Indicates that this ImageSource has enough information to be opened
		/// </summary>
		private protected virtual bool IsSourceReady => false;

		private protected virtual bool TryOpenSourceSync(out ImageData image)
		{
			image = default;
			return false;
		}

		private protected virtual bool TryOpenSourceAsync(CancellationToken ct, out Task<ImageData> asyncImage)
		{
			asyncImage = default;
			return false;
		}

		/// <summary>
		/// Retrieves the already loaded image, or for supported source (eg. WriteableBitmap, cf remarks),
		/// create a native image from the data in memory.
		/// </summary>
		/// <remarks>
		/// This is only intended to convert **uncompressed data already in memory**,
		/// and should not be used to decompress a JPEG for instance, even if the already in memory.
		/// </remarks>
		internal bool TryOpenSync(out ImageData image)
		{
			if (_imageData.HasData)
			{
				image = _imageData;
				return true;
			}

			if (IsSourceReady && TryOpenSourceSync(out image))
			{
				return true;
			}

			image = default;
			return false;
		}

		internal async Task<ImageData> Open(CancellationToken ct)
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
					return ImageData.Empty;
				}

				if (IsSourceReady && TryOpenSourceSync(out var img))
				{
					return _imageData = img;
				}

				if (IsSourceReady && TryOpenSourceAsync(ct, out var asyncImg))
				{
					return _imageData = await asyncImg;
				}

				if (Stream != null)
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

				if (FilePath.HasValue())
				{
					await using var file = File.OpenRead(FilePath);
					var nativeImage = NSImage.FromStream(file);
					if (nativeImage is not null)
					{
						return _imageData = ImageData.FromNative(nativeImage);
					}
					else
					{
						return _imageData = ImageData.Empty;
					}
				}

				if (HasBundle)
				{
					await CoreDispatcher.Main.RunAsync(
						CoreDispatcherPriority.Normal,
						async () =>
						{
							_imageData = OpenBundle();
						}
					);

					return _imageData;
				}

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
						async () =>
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

		/// <summary>
		/// Similar to Dispose, but the ImageSource can still be used in the future.
		/// </summary>
		internal void UnloadImageData()
		{
			// If the original source is a NSImage, we can't dispose it because we will
			// not be able to restore it later (from a AbsoluteUri, BundleName, file path, etc.)
			if (!_isOriginalSourceUIImage)
			{
				DisposeImageData();
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

		partial void CleanupResource()
		{
			BundlePath = null;
			BundleName = null;
		}

		partial void DisposePartial()
		{
			DisposeImageData();
		}

		private void DisposeImageData()
		{
			if (_imageData.Kind == ImageDataKind.NativeImage)
			{
				_imageData.NativeImage?.Dispose();
			}

			_imageData = ImageData.Empty;
		}

		public override string ToString()
		{
			var source = Stream ?? AbsoluteUri ?? FilePath ?? (object)_imageData.NativeImage ?? BundlePath ?? BundleName ?? "[No source]";
			return "ImageSource: {0}".InvariantCultureFormat(source);
		}
	}
}
