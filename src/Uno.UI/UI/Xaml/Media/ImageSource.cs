using Uno.Extensions;
using Uno.Foundation.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Uno;
using Uno.Diagnostics.Eventing;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Helpers;

#if !IS_UNO
using Uno.Web.Query;
using Uno.Web.Query.Cache;
#endif

namespace Windows.UI.Xaml.Media
{
	[TypeConverter(typeof(ImageSourceConverter))]
	public partial class ImageSource : DependencyObject, IDisposable
	{
		private static readonly IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public static class TraceProvider
		{
			public static readonly Guid Id = Guid.Parse("{FC4E2720-2DCF-418C-B360-93314AB3B813}");

			public const int ImageSource_SetImageDecodeStart = 1;
			public const int ImageSource_SetImageDecodeStop = 2;
		}

		const string MsAppXScheme = "ms-appx";
		const string MsAppDataScheme = "ms-appdata";

		/// <summary>
		/// The default downloader instance used by all the new instances of <see cref="ImageSource"/>.
		/// </summary>
		public static IImageSourceDownloader DefaultDownloader;

		/// <summary>
		/// The image downloader for the current instance.
		/// </summary>
		public IImageSourceDownloader Downloader;

		/// <summary>
		/// Initializes the Uno image downloader.
		/// </summary>
		private void InitializeDownloader()
		{
			Downloader = DefaultDownloader;
		}

#if !(__NETSTD__)
		internal Stream Stream { get; set; }
#endif

		internal string FilePath { get; private set; }

		public bool UseTargetSize { get; set; }

		protected ImageSource(string url) : this()
		{
			var uri = TryCreateUriFromString(url);

			if (uri != null)
			{
				InitFromUri(uri);
			}
			else
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("The uri [{0}] is not valid, skipping.", url);
				}
			}
		}

		protected ImageSource(Uri uri) : this()
		{
			InitFromUri(uri);
		}

		internal static Uri TryCreateUriFromString(string url)
		{
			if (url is null)
			{
				return null;
			}

			if (url.StartsWith("/"))
			{
				url = MsAppXScheme + "://" + url;
			}

			if (url.HasValueTrimmed() && Uri.TryCreate(url.Trim(), UriKind.RelativeOrAbsolute, out var uri))
			{
				if (!uri.IsAbsoluteUri || uri.Scheme.Length == 0)
				{
					uri = new Uri(MsAppXScheme + ":///" + uri.OriginalString.TrimStart("/"));
				}

				return uri;
			}

			return null;
		}

		internal void InitFromUri(Uri uri)
		{
			if (!uri.IsAbsoluteUri || uri.Scheme == "")
			{
				uri = new Uri(MsAppXScheme + ":///" + uri.OriginalString.TrimStart("/"));
			}

			CleanupResource();
			FilePath = null;
			AbsoluteUri = null;

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

		public static implicit operator ImageSource(string stringSource)
		{
			var uri = TryCreateUriFromString(stringSource);
			return (ImageSource)uri;
		}

		public static implicit operator ImageSource(Uri uri)
		{
			if (uri is null)
			{
				return null;
			}

			if (uri.LocalPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
				uri.LocalPath.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase))
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

		public void Dispose() => DisposePartial();

		/// <summary>
		/// Downloads an image from the provided Uri.
		/// </summary>
		/// <returns>n Uri containing a local path for the downloaded image.</returns>
		internal async Task<Uri> Download(CancellationToken ct, Uri uri)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("Initiated download from {0}", uri);
			}

			if (Downloader != null)
			{
				return await Downloader.Download(ct, uri);
			}
			else
			{
				throw new InvalidOperationException("No Downloader has been specified for this ImageSource. An IImageSourceDownloader may be provided to enable image downloads.");
			}
		}

		private Uri _absoluteUri;

		internal Uri AbsoluteUri
		{
			get => _absoluteUri;

			private set
			{
				_absoluteUri = value;

				if (value != null)
				{
					SetImageLoader();
				}
			}
		}

		partial void SetImageLoader();
	}
}
