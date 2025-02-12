using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Helpers;
using Uno.UI.Xaml.Media;
using Windows.Graphics.Display;
using Windows.Storage.Helpers;
using Windows.Storage.Streams;
using Path = global::System.IO.Path;

namespace Windows.UI.Xaml.Media.Imaging
{
	public sealed partial class BitmapImage : BitmapSource
	{
		internal ResolutionScale? ScaleOverride { get; set; }

		internal override string ContentType { get; } = "application/octet-stream";

		internal static async Task<ImageData> ResolveImageAsync(ImageSource source, Uri uri, ResolutionScale? scaleOverride, CancellationToken ct)
		{
			try
			{
				// ms-appx comes in as a relative path
				if (uri.IsAbsoluteUri)
				{
					if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
						uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
					{
						return ImageData.FromUrl(uri, source);
					}

					if (uri.IsAppData())
					{
						return await source.OpenMsAppData(uri, ct);
					}

					return ImageData.Empty;
				}

				return ImageData.FromUrl(await PlatformImageHelpers.GetScaledPath(uri, scaleOverride), source);
			}
			catch (Exception e)
			{
				return ImageData.FromError(e);
			}
		}

		private protected override bool TryOpenSourceAsync(
			CancellationToken ct,
			int? targetWidth,
			int? targetHeight,
			out Task<ImageData> asyncImage)
		{
			if (AbsoluteUri is { } uri)
			{
				var hasFileScheme = uri.IsAbsoluteUri && uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase);

				// Local files are assumed as coming from the remote server
				var newUri = hasFileScheme switch
				{
					true => new Uri(uri.PathAndQuery.TrimStart('/'), UriKind.Relative),
					_ => uri
				};

				asyncImage = ResolveImageAsync(this, newUri, ScaleOverride, ct);

				return true;
			}

			if (_stream is { } stream)
			{
				void OnProgress(ulong position, ulong? length)
				{
					if (position > 0 && length is { } actualLength)
					{
						var percent = (int)((position / (float)actualLength) * 100);
						RaiseDownloadProgress(percent);
					}
				}

				var streamWithContentType = stream.TrySetContentType(ContentType);

				asyncImage = OpenFromStream(streamWithContentType, OnProgress, ct);

				return true;
			}

			asyncImage = default;
			return false;
		}

		private void RaiseDownloadProgress(int progress = 0)
		{
			if (DownloadProgress is { } evt)
			{
				evt?.Invoke(this, new DownloadProgressEventArgs { Progress = progress });
			}
		}

		internal override void ReportImageLoaded()
		{
			RaiseImageOpened();
		}

		internal override void ReportImageFailed(string errorMessage)
		{
			RaiseImageFailed(new Exception("Unable to load image: " + errorMessage));
		}
	}
}
