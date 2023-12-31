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
using Windows.Graphics.Display;
using Windows.Storage.Helpers;
using Windows.Storage.Streams;
using Uno.UI.Xaml.Media;
using Path = global::System.IO.Path;
using NativeMethods = __Windows.Storage.Helpers.AssetsManager.NativeMethods;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	public sealed partial class BitmapImage : BitmapSource
	{
		internal ResolutionScale? ScaleOverride { get; set; }

		internal override string ContentType { get; } = "application/octet-stream";

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

				asyncImage = AssetResolver.ResolveImageAsync(this, newUri, ScaleOverride, ct);

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

		internal static partial class AssetResolver
		{
			private static readonly Lazy<Task<HashSet<string>>> _assets = new Lazy<Task<HashSet<string>>>(GetAssets);

			private static async Task<HashSet<string>> GetAssets()
			{
				var assetsUri = AssetsPathBuilder.BuildAssetUri("uno-assets.txt");

				var assets = await NativeMethods.DownloadAssetsManifestAsync(assetsUri);

				return new HashSet<string>(LineMatch().Split(assets));
			}

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

					// POTENTIAL BUG HERE: if the "fetch" failed, the application
					// will never retry to fetch it again.
					var assets = await _assets.Value;

					return ImageData.FromUrl(GetScaledPath(uri.OriginalString, assets, scaleOverride), source);
				}
				catch (Exception e)
				{
					return ImageData.FromError(e);
				}
			}

			private static string GetScaledPath(string path, HashSet<string> assets, ResolutionScale? scaleOverride)
			{
				if (!string.IsNullOrEmpty(path))
				{
					var directory = Path.GetDirectoryName(path);
					var filename = Path.GetFileNameWithoutExtension(path);
					var extension = Path.GetExtension(path);

					var resolutionScale = scaleOverride == null ? (int)DisplayInformation.GetForCurrentView().ResolutionScale : (int)scaleOverride;

					// On Windows, the minimum scale is 100%, however, on Wasm, we can have lower scales.
					// This condition is to allow Wasm to use the .scale-100 image when the scale is < 100%
					if (resolutionScale < 100)
					{
						resolutionScale = 100;
					}


					for (var i = KnownScales.Length - 1; i >= 0; i--)
					{
						var probeScale = KnownScales[i];

						if (resolutionScale >= probeScale)
						{
							var filePath = Path.Combine(directory, $"{filename}.scale-{probeScale}{extension}");

							if (assets.Contains(filePath))
							{
								return AssetsPathBuilder.BuildAssetUri(filePath);
							}
						}
					}

					return AssetsPathBuilder.BuildAssetUri(path);
				}

				return path;
			}

			private static readonly int[] KnownScales =
			{
				(int)ResolutionScale.Scale100Percent,
				(int)ResolutionScale.Scale120Percent,
				(int)ResolutionScale.Scale125Percent,
				(int)ResolutionScale.Scale140Percent,
				(int)ResolutionScale.Scale150Percent,
				(int)ResolutionScale.Scale160Percent,
				(int)ResolutionScale.Scale175Percent,
				(int)ResolutionScale.Scale180Percent,
				(int)ResolutionScale.Scale200Percent,
				(int)ResolutionScale.Scale225Percent,
				(int)ResolutionScale.Scale250Percent,
				(int)ResolutionScale.Scale300Percent,
				(int)ResolutionScale.Scale350Percent,
				(int)ResolutionScale.Scale400Percent,
				(int)ResolutionScale.Scale450Percent,
				(int)ResolutionScale.Scale500Percent
			};

			[GeneratedRegex("\r\n|\r|\n")]
			private static partial Regex LineMatch();
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
