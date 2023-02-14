using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media
{
	public partial class ImageSource
	{
		internal static readonly bool SupportsAsyncFromBundle;
		internal static readonly bool SupportsFromBundle;

		private static NSUrlSession _defaultSession;

		static ImageSource()
		{
			SupportsAsyncFromBundle = UIDevice.CurrentDevice.CheckSystemVersion(9, 0);
			SupportsFromBundle = UIDevice.CurrentDevice.CheckSystemVersion(8, 0);
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

		/// <summary>
		/// The <see cref="NSUrlSession"/> to use for remote url downloads, if using the platform downloader.
		/// By default <see cref="NSUrlSession.SharedSession"/> will be used.
		/// </summary>
		public static NSUrlSession DefaultSession
		{
			get => _defaultSession ?? NSUrlSession.SharedSession;
			set => _defaultSession = value;
		}

		internal string BundleName { get; private set; }
		internal string BundlePath { get; private set; }

		static public implicit operator ImageSource(UIImage image) => new ImageSource(image);

		private static UIImage OpenBundleFromString(string bundle)
		{
			if (!bundle.IsNullOrWhiteSpace())
			{
				return UIImage.FromBundle(bundle);
			}

			return null;
		}

		private ImageData OpenImageDataFromStream()
		{
			Stream.Position = 0;
			using var data = NSData.FromStream(Stream);
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

		private Task<ImageData> OpenImageDataFromFilePathAsync()
		{
			var uri = new Uri(FilePath);
			var nativeImage = UIImage.FromFile(FilePath);

			_imageData = nativeImage is not null ?
				ImageData.FromNative(nativeImage) : ImageData.Empty;

			return Task.FromResult(_imageData);
		}

		private async Task<ImageData> OpenImageDataFromBundleAsync(CancellationToken ct)
		{
			if (SupportsAsyncFromBundle)
			{
				return _imageData = OpenBundle();
			}
			else
			{
				await CoreDispatcher.Main.RunAsync(
					CoreDispatcherPriority.Normal,
					() =>
					{
						_imageData = OpenBundle();
					}
				).AsTask(ct);

				return _imageData;
			}
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
						() =>
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
	}
}
