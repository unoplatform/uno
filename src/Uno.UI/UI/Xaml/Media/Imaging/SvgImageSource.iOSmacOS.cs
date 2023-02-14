using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using Uno.UI.Xaml.Media;
using System.Net.Http;
using Foundation;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Windows.Storage;
using Windows.Storage.Streams;
using CoreFoundation;
using Microsoft.UI.Xaml.Shapes;

#if __IOS__
using UIKit;
#else
using AppKit;
#endif

namespace Microsoft.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private protected override bool IsSourceReady => true;

	private protected override bool TryOpenSourceAsync(CancellationToken ct, out Task<ImageData> asyncImage) =>
		TryOpenSvgImageData(ct, out asyncImage);

	private async Task<ImageData> GetSvgImageDataAsync(CancellationToken ct)
	{
		try
		{
			if (Stream != null)
			{
				Stream.Position = 0;
				using var data = NSData.FromStream(Stream);
				var bytes = data.ToArray();
				return ImageData.FromBytes(bytes);
			}

			if (!FilePath.IsNullOrEmpty())
			{
				using var data = NSData.FromFile(FilePath);
				var bytes = data.ToArray();
				return ImageData.FromBytes(bytes);
			}

			if (HasBundle)
			{
				return OpenSvgBundle(BundlePath);
			}

			if (Downloader == null)
			{
				return await DownloadSvgAsync(ct);
			}
			else
			{
				var localFileUri = await Download(ct, AbsoluteUri);

				if (localFileUri == null)
				{
					return ImageData.Empty;
				}

				return OpenSvgBundle(localFileUri.LocalPath);
			}
		}
		catch (Exception ex)
		{
			return ImageData.FromError(ex);
		}
	}

	private ImageData OpenSvgBundle(string bundlePath)
	{
		var directoryName = global::System.IO.Path.GetDirectoryName(bundlePath);
		var fileName = global::System.IO.Path.GetFileNameWithoutExtension(bundlePath);
		var fileExtension = global::System.IO.Path.GetExtension(bundlePath);

		var resourcePathname = NSBundle.MainBundle.GetUrlForResource(global::System.IO.Path.Combine(directoryName, fileName), fileExtension.Substring(1));
		using var data = NSData.FromUrl(resourcePathname);
		var bytes = data.ToArray();
		return ImageData.FromBytes(bytes);
	}

	/// <summary>
	/// Opens the targetted image for this image use using the native iOS image downloader, using NSUrl.
	/// </summary>
	private
#if __IOS__
		async
#endif
		Task<ImageData> DownloadSvgAsync(CancellationToken ct)
	{
		if (ct.IsCancellationRequested)
		{
#if __IOS__
			return ImageData.Empty;
#else
			return Task.FromResult(ImageData.Empty);
#endif
		}

		using var url = new NSUrl(AbsoluteUri.AbsoluteUri);
#if __IOS__
		using var request = NSUrlRequest.FromUrl(url);
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

		return ImageData.Empty;
#else
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Loading image from [{AbsoluteUri.OriginalString}]");
		}

#pragma warning disable CS0618
		// fallback on the platform's loader
		using var data = NSData.FromUrl(url, NSDataReadingOptions.Mapped, out var error);
		if (error != null)
		{
			this.Log().Error(error.LocalizedDescription);
		}
		else
		{
			var bytes = data.ToArray();
			return Task.FromResult(ImageData.FromBytes(bytes));
		}

		return Task.FromResult(ImageData.Empty);
#endif
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
