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
	partial class SvgImageSource : IImageSource
	{
		private static readonly string UNO_BOOTSTRAP_APP_BASE = Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_APP_BASE));

		internal string ContentType { get; set; } = "image/svg+xml";

		partial void InitPartial()
		{
		}
		private void RaiseImageFailed(SvgImageSourceLoadStatus loadStatus)
		{
			OpenFailed?.Invoke(this, new SvgImageSourceFailedEventArgs(loadStatus));
		}

		private void RaiseImageOpened()
		{
			Opened?.Invoke(this, new SvgImageSourceOpenedEventArgs());
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
			if (Stream is { } stream)
			{
				stream.Position = 0;

				async Task<ImageData> FetchImage()
				{
					try
					{
						stream.Position = 0;
						var bytes = await stream.ReadBytesAsync();
						var encodedBytes = Convert.ToBase64String(bytes);

						RaiseImageOpened();

						return new ImageData
						{
							Kind = ImageDataKind.DataUri,
							Value = "data:" + ContentType + ";base64," + encodedBytes
						};
					}
					catch (Exception ex)
					{
						RaiseImageFailed(SvgImageSourceLoadStatus.NetworkError);

						return new ImageData {Kind = ImageDataKind.Error, Error = ex};
					}
				}

				asyncImage = FetchImage();

				return true;
			}

			asyncImage = default;
			return false;

		}

		public void ReportImageLoaded() => RaiseImageOpened();

		public void ReportImageFailed() => RaiseImageFailed(SvgImageSourceLoadStatus.Other);
	}
}
