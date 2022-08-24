using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using Uno.UI.Xaml.Media;
using System.Net.Http;
using Foundation;
using UIKit;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Windows.Storage;
using Windows.Storage.Streams;
using CoreFoundation;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private protected override bool IsSourceReady => true;

	private protected override bool TryOpenSourceAsync(CancellationToken ct, out Task<ImageData> asyncImage)
	{
		asyncImage = TryOpenSourceAsync(ct);
		return true;
	}

	private async Task<ImageData> TryOpenSourceAsync(CancellationToken ct)
	{
		var imageData = await TryReadImageDataAsync(ct);
		if (imageData.Kind == ImageDataKind.ByteArray)
		{
			_svgProvider.NotifySourceOpened(imageData.ByteArray);
		}
		return imageData;
	}

	private async Task<ImageData> TryReadImageDataAsync(CancellationToken ct)
	{
		if (Stream != null)
		{
			Stream.Position = 0;
			using (var data = NSData.FromStream(Stream))
			{
				var bytes = data.ToArray();
				return ImageData.FromBytes(bytes);
			}
		}

		if (FilePath.HasValue())
		{
			var uri = new Uri(FilePath);
			var nativeImage = UIImage.FromFile(FilePath);
			if (nativeImage is not null)
			{
				return ImageData.FromNative(nativeImage);
			}
			else
			{
				return ImageData.Empty;
			}
		}

		if (HasBundle)
		{
			var directoryName = global::System.IO.Path.GetDirectoryName(BundlePath);
			var fileName = global::System.IO.Path.GetFileNameWithoutExtension(BundlePath);
			var fileExtension = global::System.IO.Path.GetExtension(BundlePath);

			var resourcePathname = NSBundle.MainBundle.GetUrlForResource(global::System.IO.Path.Combine(directoryName, fileName), fileExtension.Substring(1));
			var data = NSData.FromUrl(resourcePathname);
			var bytes = data.ToArray();
			return ImageData.FromBytes(bytes);

			//if (SupportsAsyncFromBundle)
			//{
			//	return OpenSvgBundle();
			//}
			//else
			//{
			//	ImageData result = ImageData.Empty;
			//	await CoreDispatcher.Main.RunAsync(
			//		CoreDispatcherPriority.Normal,
			//		async () =>
			//		{
			//			// TODO MZ: Return ImageData from async method!
			//			result = OpenSvgBundle();
			//		}
			//	);

			//	return result;
			//}
		}

		if (Downloader == null)
		{
			return await DownloadSvgAsync(ct);
		}
		else
		{
			var localFileUri = await Download(ct, WebUri);

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
					return ImageData.FromNative(nativeImage);
				}
				else
				{
					return ImageData.Empty;
				}
			}
			else
			{
				ImageData result = ImageData.Empty;
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
								result = ImageData.FromNative(nativeImage);
							}
							else
							{
								result = ImageData.Empty;
							}
						}
						else
						{
							// On iOS 7, FromBundle doesn't work. We can use this method instead.
							// Note that we must use OriginalString and not LocalPath.
							var nativeImage = UIImage.LoadFromData(NSData.FromUrl(new NSUrl(localFileUri.OriginalString)));
							if (nativeImage is not null)
							{
								result = ImageData.FromNative(nativeImage);
							}
							else
							{
								result = ImageData.Empty;
							}
						}
					}).AsTask(ct);
				return result;
			}
		}
	}

	private ImageData OpenSvgBundle()
	{
		//TODO:MZ:Fixme
		//var bundle = OpenSvgBundleFromString(BundleName) ?? OpenSvgBundleFromString(BundlePath);
		//if (bundle is not null)
		//{
		//	bundle.Data
		//	_imageData = bundle..FromNative(image);
		//}
		//else
		//{
		//	_imageData = ImageData.Empty;
		//}

		//if (!_imageData.HasData)
		//{
		//	this.Log().ErrorFormat("Unable to locate bundle resource [{0}]", BundlePath ?? BundleName);
		//}

		//return _imageData;
		return ImageData.Empty;
	}

	private NSBundle OpenSvgBundleFromString(string bundle)
	{
		return NSBundle.FromPath(bundle);
	}

	/// <summary>
	/// Opens the targetted image for this image use using the native iOS image downloader, using NSUrl.
	/// </summary>
	private async Task<ImageData> DownloadSvgAsync(CancellationToken ct)
	{
		if (ct.IsCancellationRequested)
		{
			return ImageData.Empty;
		}

		using (var url = new NSUrl(WebUri.AbsoluteUri))
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
						return ImageData.Empty;
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
						var bytes = result.Data.ToArray();
						return ImageData.FromBytes(bytes);
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

		return ImageData.Empty;
	}

	private async Task<ImageData> ReadFromStreamAsync(Stream stream)
	{
		var memoryStream = new MemoryStream();
		await stream.CopyToAsync(memoryStream);
		var data = memoryStream.ToArray();
		return ImageData.FromBytes(data);
	}

	private static string GetApplicationPath(string rawPath)
	{
		var originalLocalPath =
			global::System.IO.Path.Combine(Windows.Application­Model.Package.Current.Installed­Location.Path,
				 rawPath.TrimStart('/').Replace('/', global::System.IO.Path.DirectorySeparatorChar)
			);

		return originalLocalPath;
	}
}
