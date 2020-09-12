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
using UIKit;
using Uno.Logging;
using Windows.UI.Core;
using Uno.Disposables;
using Microsoft.Extensions.Logging;

namespace Windows.UI.Xaml.Media
{
	public partial class ImageSource
	{
		private static readonly bool SupportsAsyncFromBundle;
		private static readonly bool SupportsFromBundle;

		private readonly bool _isOriginalSourceUIImage;

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
			_imageData = image;
		}

		private UIImage _imageData;

		internal string BundleName { get; private set; }
		internal string BundlePath { get; private set; }

		static public implicit operator ImageSource(UIImage image)
		{
			return new ImageSource(image);
		}

		public bool HasSource()
		{
			return IsSourceReady
				|| Stream != null
				|| WebUri != null
				|| FilePath.HasValueTrimmed()
				|| _imageData != null
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
		internal UIImage OpenBundle()
		{
			_imageData = OpenBundleFromString(BundleName) ?? OpenBundleFromString(BundlePath);

			if (_imageData == null)
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

		private protected virtual bool TryOpenSourceSync(out UIImage image)
		{
			image = default;
			return false;
		}

		private protected virtual bool TryOpenSourceAsync(out Task<UIImage> asyncImage)
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
		internal bool TryOpenSync(out UIImage image)
		{
			if (_imageData != null)
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

		internal async Task<UIImage> Open(CancellationToken ct)
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

				if (IsSourceReady && TryOpenSourceSync(out var img))
				{
					return _imageData = img;
				}

				if (IsSourceReady && TryOpenSourceAsync(out var asyncImg))
				{
					return _imageData = await asyncImg;
				}

				if (Stream != null)
				{
					Stream.Position = 0;
					using (var data = NSData.FromStream(Stream))
					{
						return _imageData = UIImage.LoadFromData(data);
					}
				}

				if (FilePath.HasValue())
				{
					var uri = new Uri(FilePath);
					return _imageData = UIImage.FromFile(FilePath);
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
					var localFileUri = await Download(ct, WebUri);

					if (localFileUri == null)
					{
						return null;
					}

					if (SupportsAsyncFromBundle)
					{
						// Since iOS9, UIImage.FromBundle is thread safe.
						_imageData = UIImage.FromBundle(localFileUri.LocalPath);
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
									_imageData = UIImage.FromBundle(localFileUri.LocalPath);
								}
								else
								{
									// On iOS 7, FromBundle doesn't work. We can use this method instead.
									// Note that we must use OriginalString and not LocalPath.
									_imageData = UIImage.LoadFromData(NSData.FromUrl(new NSUrl(localFileUri.OriginalString)));
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

			using (var url = new NSUrl(WebUri.OriginalString))
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
							_imageData = UIImage.LoadFromData(result.Data);
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
			if (_imageData != null)
			{
				_imageData.Dispose();
				_imageData = null;
			}
		}

		public override string ToString()
		{
			var source = Stream ?? WebUri ?? FilePath ?? (object)_imageData ?? BundlePath ?? BundleName ?? "[No source]";
			return "ImageSource: {0}".InvariantCultureFormat(source);
		}
	}
}
