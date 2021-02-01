using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Uno.Extensions;
using Uno.Logging;
using static Windows.UI.Xaml.Media.Imaging.BitmapImage;

namespace Windows.UI.Xaml.Media.Imaging
{
	partial class SvgImageSource
	{
		private static readonly string UNO_BOOTSTRAP_APP_BASE = Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_APP_BASE));

		internal string ContentType { get; set; } = "image/svg+xml";

		partial void InitPartial()
		{
		}

		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			if (WebUri is { } webUri)
			{
				image = default;

				var hasFileScheme = webUri.IsAbsoluteUri && webUri.Scheme == "file";

				// Local files are assumed as coming from the remote server
				var uri = hasFileScheme switch
				{
					true => new Uri(webUri.PathAndQuery.TrimStart('/'), UriKind.Relative),
					_ => webUri
				};

				if (uri.IsAbsoluteUri)
				{
					if (uri.Scheme == "http" || uri.Scheme == "https")
					{
						image = new ImageData
						{
							Kind = ImageDataKind.Url,
							Value = uri.AbsoluteUri,
							Source = this
						};
					}

					// TODO: Implement ms-appdata
				}
				else
				{
					var path = Path.Combine(UNO_BOOTSTRAP_APP_BASE, uri.OriginalString);
					image = new ImageData
					{
						Kind = ImageDataKind.Url,
						Value = path,
						Source = this
					};
				}

				return image.Kind != default;
			}

			image = default;
			return false;
		}

		private protected override bool TryOpenSourceAsync(
			CancellationToken ct,
			int? targetWidth,
			int? targetHeight,
			out Task<ImageData> asyncImage)
		{
			if (_stream is { } stream)
			{
				var streamWithContentType = stream.TrySetContentType(ContentType);

				asyncImage = OpenFromStream(streamWithContentType, null, ct);

				return true;
			}

			asyncImage = default;
			return false;

		}

		internal override void ReportImageLoaded() => RaiseImageOpened();

		internal override void ReportImageFailed(string errorMessage) => RaiseImageFailed(SvgImageSourceLoadStatus.Other);

		public override string ToString()
		{
			if (WebUri is { } uri)
			{
				return $"{GetType().Name}/{uri}";
			}

			if (_stream is { } stream)
			{
				return $"{GetType().Name}/{stream.GetType()}";
			}

			return $"{GetType().Name}/-empty-";
		}
	}
}
