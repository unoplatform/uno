using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Uno.Extensions;
using Uno.Foundation.Logging;
using static Microsoft.UI.Xaml.Media.Imaging.BitmapImage;
using Windows.Storage.Helpers;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	partial class SvgImageSource
	{
		internal string ContentType { get; set; } = "image/svg+xml";

		partial void InitPartial()
		{
		}

		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			if (AbsoluteUri is { } absoluteUri)
			{
				image = default;

				var hasFileScheme = absoluteUri.IsAbsoluteUri && absoluteUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase);

				// Local files are assumed as coming from the remote server
				var uri = hasFileScheme switch
				{
					true => new Uri(absoluteUri.PathAndQuery.TrimStart('/'), UriKind.Relative),
					_ => absoluteUri
				};

				if (uri.IsAbsoluteUri)
				{
					if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
						uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
					{
						image = ImageData.FromUrl(uri.AbsoluteUri, this);
					}

					// TODO: Implement ms-appdata
				}
				else
				{
					var path = AssetsPathBuilder.BuildAssetUri(uri.OriginalString);
					image = ImageData.FromUrl(path, this);
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
			if (AbsoluteUri is { } uri)
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
