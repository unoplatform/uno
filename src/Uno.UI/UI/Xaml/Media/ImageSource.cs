#nullable enable
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI;
using System.Net;

#if !IS_UNO
using Uno.Web.Query;
using Uno.Web.Query.Cache;
#endif

namespace Microsoft.UI.Xaml.Media
{
	[TypeConverter(typeof(ImageSourceConverter))]
	public partial class ImageSource : DependencyObject, IDisposable
	{
		private protected static HttpClient? _httpClient;
		private protected ImageData _imageData = ImageData.Empty;

		internal event Action? Invalidated;

		private protected void InvalidateImageSource() => Invalidated?.Invoke();

		public static class TraceProvider
		{
			public static readonly Guid Id = Guid.Parse("{FC4E2720-2DCF-418C-B360-93314AB3B813}");

			public const int ImageSource_SetImageDecodeStart = 1;
			public const int ImageSource_SetImageDecodeStop = 2;
		}

		const string MsAppXScheme = "ms-appx";

#pragma warning disable CA2211
		/// <summary>
		/// The default downloader instance used by all the new instances of <see cref="ImageSource"/>.
		/// </summary>
		public static IImageSourceDownloader? DefaultDownloader;

		/// <summary>
		/// The image downloader for the current instance.
		/// </summary>
		public IImageSourceDownloader? Downloader;
#pragma warning restore CA2211

		internal string? FilePath { get; private set; }

		public bool UseTargetSize { get; set; }

		protected ImageSource(string url) : this()
		{
			var uri = TryCreateUriFromString(url);

			if (uri is null)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("The uri [{0}] is not valid, skipping.", url);
				}
			}

			InitFromUri(uri);
		}

		protected ImageSource(Uri uri) : this()
		{
			InitFromUri(uri);
		}

		internal static Uri? TryCreateUriFromString(string url)
		{
			if (url is null)
			{
				return null;
			}

			if (url.StartsWith('/'))
			{
				url = MsAppXScheme + "://" + url;
			}

			if (!url.IsNullOrWhiteSpace() && Uri.TryCreate(url.Trim(), UriKind.RelativeOrAbsolute, out var uri))
			{
				if (!uri.IsAbsoluteUri || uri.Scheme.Length == 0)
				{
					uri = new Uri(MsAppXScheme + ":///" + uri.OriginalString.TrimStart("/"));
				}

				return uri;
			}

			return null;
		}

		internal void InitFromUri(Uri? uri)
		{
			CleanupResource();
			FilePath = null;
			AbsoluteUri = null;

			if (uri is null)
			{
				return;
			}

			if (!uri.IsAbsoluteUri || uri.Scheme == "")
			{
				uri = new Uri(MsAppXScheme + ":///" + uri.OriginalString.TrimStart("/"));
			}

			if (uri.IsLocalResource())
			{
				InitFromResource(uri);
				return;
			}

			if (uri.IsAppData())
			{
				var filePath = AppDataUriEvaluator.ToPath(uri);
				InitFromFile(filePath);
			}

			if (uri.IsFile)
			{
				InitFromFile(uri.PathAndQuery);
			}

			AbsoluteUri = uri;
		}

		private void InitFromFile(string filePath)
		{
			FilePath = filePath;
		}

		partial void InitFromResource(Uri uri);

		partial void CleanupResource();

		public static implicit operator ImageSource?(string url)
		{
			if (TryCreateUriFromString(url) is Uri uri)
			{
				return (ImageSource?)uri;
			}

			return null;
		}

		public static implicit operator ImageSource?(Uri uri)
		{
			if (uri is null)
			{
				return null;
			}

			if (__LinkerHints.Is_Microsoft_UI_Xaml_Media_Imaging_SvgImageSource_Available
				&& (uri.LocalPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
					uri.LocalPath.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase))
			)
			{
				return new SvgImageSource(uri);
			}
			else
			{
				return new BitmapImage(uri);
			}
		}

		public static implicit operator ImageSource(Stream stream)
		{
			throw new NotSupportedException("Implicit conversion from Stream to ImageSource is not supported");
		}

		partial void DisposePartial();

		public void Dispose()
		{
			UnloadImageData();
			DisposePartial();
		}

		internal Uri? AbsoluteUri { get; private set; }

		private protected async Task<Stream> OpenStreamFromUriAsync(Uri uri, CancellationToken ct)
		{
			if (uri.IsFile)
			{
				return File.Open(uri.LocalPath, FileMode.Open);
			}

			_httpClient ??= new HttpClient();
			var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead, ct);
			return await response.Content.ReadAsStreamAsync();
		}

		internal void UnloadImageData()
		{
			UnloadImageDataPlatform();
			UnloadImageSourceData();
			_imageData = ImageData.Empty;
		}

		partial void UnloadImageDataPlatform();

		/// <summary>
		/// Override in concrete ImageSource implementations
		/// to provide source-specific cleanup of image data.
		/// </summary>
		private protected virtual void UnloadImageSourceData()
		{
		}
	}
}
