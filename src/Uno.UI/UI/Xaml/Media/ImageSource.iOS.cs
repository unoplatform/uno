using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Media
{
	public partial class ImageSource
	{
		private readonly bool _isOriginalSourceUIImage;

		internal static readonly bool SupportsAsyncFromBundle;
		internal static readonly bool SupportsFromBundle;

		static ImageSource()
		{
			SupportsAsyncFromBundle = UIDevice.CurrentDevice.CheckSystemVersion(9, 0);
			SupportsFromBundle = UIDevice.CurrentDevice.CheckSystemVersion(8, 0);
		}

		private static NSUrlSession _defaultSession;
		/// <summary>
		/// The <see cref="NSUrlSession"/> to use for remote url downloads, if using the platform downloader.
		/// By default <see cref="NSUrlSession.SharedSession"/> will be used.
		/// </summary>
		public static NSUrlSession DefaultSession
		{
			get => _defaultSession ?? NSUrlSession.SharedSession;
			set => _defaultSession = value;
		}

		protected ImageSource()
		{
			UseTargetSize = true;
			InitializeDownloader();
		}

		protected ImageSource(UIImage image)
		{
			_isOriginalSourceUIImage = true;
			_imageData = ImageData.FromNative(image);
		}

		internal string BundleName { get; private set; }
		internal string BundlePath { get; private set; }

		static public implicit operator ImageSource(UIImage image) => new ImageSource(image);

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
		/// Open bundle is using either the name of the bundle (for 
		/// android compatibility), or the path to the bundle for Windows compatibility.
		/// </summary>
		internal ImageData OpenBundle()
		{
			var image = OpenBundleFromString(BundleName) ?? OpenBundleFromString(BundlePath);
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

		private static UIImage OpenBundleFromString(string bundle)
		{
			if (bundle.HasValueTrimmed())
			{
				return UIImage.FromBundle(bundle);
			}

			return null;
		}

		/// <summary>
		/// Indicates that this ImageSource has enough information to be opened
		/// </summary>
		private protected virtual bool IsSourceReady => false;

		private protected virtual bool TryOpenSourceSync([NotNullWhen(true)] out ImageData image)
		{
			image = default;
			return false;
		}

		private protected virtual bool TryOpenSourceAsync(CancellationToken ct, [NotNullWhen(true)] out Task<ImageData> asyncImage)
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
					using (var data = NSData.FromStream(Stream))
					{
						var nativeImage = UIImage.LoadFromData(data);
						if (nativeImage is not null)
						{
							return _imageData = ImageData.FromNative(nativeImage);
						}
						else
						{
							return ImageData.Empty;
						}
					}
				}

				if (FilePath.HasValue())
				{
					var uri = new Uri(FilePath);
					var nativeImage = UIImage.FromFile(FilePath);
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
					if (SupportsAsyncFromBundle)
					{
						return _imageData = OpenBundle();
					}
					else
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

					if (SupportsAsyncFromBundle)
					{
						// Since iOS9, UIImage.FromBundle is thread safe.
						var nativeImage = UIImage.FromBundle(localFileUri.LocalPath);
						if (nativeImage is not null)
						{
							return _imageData = ImageData.FromNative(nativeImage);
						}
						else
						{
							return _imageData = ImageData.Empty;
						}
					}
					else
					{
						await CoreDispatcher.Main.RunAsync(
							CoreDispatcherPriority.Normal,
							async () =>
							{
								if (SupportsFromBundle)
								{
									// FromBundle calls UIImage:fromName which caches the decoded image.
									// This is done to avoid decoding images from the byte array returned 
									// from the cache.
									var nativeImage = UIImage.FromBundle(localFileUri.LocalPath);
									if (nativeImage is not null)
									{
										_imageData = ImageData.FromNative(nativeImage);
									}
									else
									{
										_imageData = ImageData.Empty;
									}
								}
								else
								{
									// On iOS 7, FromBundle doesn't work. We can use this method instead.
									// Note that we must use OriginalString and not LocalPath.
									var nativeImage = UIImage.LoadFromData(NSData.FromUrl(new NSUrl(localFileUri.OriginalString)));
									if (nativeImage is not null)
									{
										_imageData = ImageData.FromNative(nativeImage);
									}
									else
									{
										_imageData = ImageData.Empty;
									}
								}
							}).AsTask(ct);
					}
				}

				return _imageData;
			}
		}

		/// <summary>
		/// Opens the targetted image for this image use using the native iOS image downloader, using NSUrl.
		/// </summary>
		private async Task OpenUsingPlatformDownloader(CancellationToken ct)
		{
			if (ct.IsCancellationRequested)
			{
				return;
			}

			using (var url = new NSUrl(AbsoluteUri.AbsoluteUri))
			{
				using (var request = NSUrlRequest.FromUrl(url))
				{
					NSUrlSessionDataTask task;
					var awaitable = DefaultSession.CreateDataTaskAsync(request, out task);
					ct.Register(OnCancel);
					try
					{
						task.Resume(); // We need to call this manually https://bugzilla.xamarin.com/show_bug.cgi?id=28425#c3
						var result = await awaitable;
						task = null;
						var response = result.Response as NSHttpUrlResponse;

						if (ct.IsCancellationRequested)
						{
							return;
						}
						else if (!IsSuccessful(response.StatusCode))
						{
							if (this.Log().IsEnabled(LogLevel.Error))
							{
								this.Log().LogError(NSHttpUrlResponse.LocalizedStringForStatusCode(response.StatusCode));
							}
						}
						else
						{
							var image = UIImage.LoadFromData(result.Data);
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
					catch (NSErrorException e)
					{
						// This can occur for various reasons: download was cancelled, NSAppTransportSecurity blocks download, host couldn't be resolved...
						if (ct.IsCancellationRequested)
						{
							if (this.Log().IsEnabled(LogLevel.Debug))
							{
								this.Log().LogDebug(e.ToString());
							}
						}
						else if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError(e.ToString());
						}
					}

					void OnCancel()
					{
						// Cancel the current download
						task?.Cancel();
					}

					bool IsSuccessful(nint status) => status < 300;
				}
			}
		}

		/// <summary>
		/// Similar to Dispose, but the ImageSource can still be used in the future.
		/// </summary>
		internal void UnloadImageData()
		{
			// If the original source is a UIImage, we can't dispose it because we will
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
			var source = Stream ?? AbsoluteUri ?? FilePath ?? (object)_imageData ?? BundlePath ?? BundleName ?? "[No source]";
			return "ImageSource: {0}".InvariantCultureFormat(source);
		}
	}
}
