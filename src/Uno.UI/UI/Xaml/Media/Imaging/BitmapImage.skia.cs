using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Uno.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	public sealed partial class BitmapImage : BitmapSource
	{
		private const int MIN_DIMENSION_SYNC_LOADING = 100;

		// TODO: Introduce LRU caching if needed
		private static readonly Dictionary<string, string> _scaledBitmapCache = new();

		private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
		{
			asyncImage = TryOpenSourceAsync(ct, targetWidth, targetHeight);

			return true;
		}

		private async Task<ImageData> TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight)
		{
			var surface = new SkiaCompositionSurface();

			try
			{
				if (UriSource is not null)
				{
					if (!UriSource.IsAbsoluteUri)
					{
						return ImageData.FromError(new InvalidOperationException($"UriSource must be absolute"));
					}

					if (UriSource.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
						UriSource.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
						UriSource.IsFile)
					{
						using var imageStream = await OpenStreamFromUriAsync(UriSource, ct);

						return OpenFromStream(targetWidth, targetHeight, surface, imageStream);
					}
					else if (UriSource.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase))
					{
						var path = UriSource.PathAndQuery;

						if (UriSource.Host is { Length: > 0 } host)
						{
							path = host + "/" + path.TrimStart('/');
						}

						var filePath = GetScaledPath(path);
						using var fileStream = File.OpenRead(filePath);

						return OpenFromStream(targetWidth, targetHeight, surface, fileStream);
					}
					else if (UriSource.Scheme.Equals("ms-appdata", StringComparison.OrdinalIgnoreCase))
					{
						using var fileStream = File.OpenRead(FilePath);

						return OpenFromStream(targetWidth, targetHeight, surface, fileStream);
					}
				}
				else if (_stream != null)
				{
					return OpenFromStream(targetWidth, targetHeight, surface, _stream.AsStream());
				}
			}
			catch (Exception e)
			{
				return ImageData.FromError(e);
			}

			return default;
		}

		private ImageData OpenFromStream(int? targetWidth, int? targetHeight, SkiaCompositionSurface surface, global::System.IO.Stream imageStream)
		{
			var result = surface.LoadFromStream(targetWidth, targetHeight, imageStream);

			if (result.success)
			{
				RaiseImageOpened();
				return ImageData.FromCompositionSurface(surface);
			}
			else
			{
				var exception = new InvalidOperationException($"Image load failed ({result.nativeResult})");
				RaiseImageFailed(exception);
				return ImageData.FromError(exception);
			}
		}

		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			if (_stream != null &&
				targetWidth is { } width &&
				targetHeight is { } height &&
				height < MIN_DIMENSION_SYNC_LOADING &&
				width < MIN_DIMENSION_SYNC_LOADING)
			{
				var surface = new SkiaCompositionSurface();
				image = OpenFromStream(targetWidth, targetHeight, surface, _stream.AsStream());
				return image.CompositionSurface != null;
			}

			return base.TryOpenSourceSync(targetWidth, targetHeight, out image);
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

		internal static string GetScaledPath(string rawPath)
		{
			// Avoid querying filesystem if we already seen this file
			if (_scaledBitmapCache.TryGetValue(rawPath, out var result))
			{
				return result;
			}

			var originalLocalPath =
				Path.Combine(Windows.Application­Model.Package.Current.InstalledPath,
					 rawPath.TrimStart('/').Replace('/', global::System.IO.Path.DirectorySeparatorChar)
				);

			var resolutionScale = (int)DisplayInformation.GetForCurrentView().ResolutionScale;

			var baseDirectory = Path.GetDirectoryName(originalLocalPath);
			var baseFileName = Path.GetFileNameWithoutExtension(originalLocalPath);
			var baseExtension = Path.GetExtension(originalLocalPath);

			var applicableScale = FindApplicableScale(true);
			if (applicableScale is null)
			{
				applicableScale = FindApplicableScale(false);
			}

			result = applicableScale ?? originalLocalPath;
			_scaledBitmapCache[rawPath] = result;
			return result;

			string FindApplicableScale(bool onlyMatching)
			{
				for (var i = KnownScales.Length - 1; i >= 0; i--)
				{
					var probeScale = KnownScales[i];

					if ((onlyMatching && resolutionScale >= probeScale) ||
						(!onlyMatching && resolutionScale < probeScale))
					{
						var filePath = Path.Combine(baseDirectory, $"{baseFileName}.scale-{probeScale}{baseExtension}");

						if (File.Exists(filePath))
						{
							return filePath;
						}
					}
				}

				return null;
			}
		}
	}
}
