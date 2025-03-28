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
using Windows.UI.Composition;
using Uno.Extensions;
using Uno.Helpers;
using Uno.UI.Xaml.Media;
using Windows.Application­Model;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;

namespace Windows.UI.Xaml.Media.Imaging
{
	public sealed partial class BitmapImage : BitmapSource
	{
		// TODO: Introduce LRU caching if needed
		private static readonly Dictionary<string, string> _scaledBitmapCache = new();

		private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
		{
			asyncImage = TryOpenSourceAsync(ct);

			return true;
		}

		private async Task<ImageData> TryOpenSourceAsync(CancellationToken ct)
		{
			try
			{
				var uri = UriSource;
				if (uri is not null)
				{
					if (!uri.IsAbsoluteUri)
					{
						return ImageData.FromError(new InvalidOperationException($"UriSource must be absolute"));
					}

					if (uri.IsLocalResource())
					{
						uri = new Uri(await PlatformImageHelpers.GetScaledPath(uri, scaleOverride: null));
					}

					var imageData = await ImageSourceHelpers.GetImageDataFromUriAsCompositionSurface(uri, ct);
					if (imageData.Kind == ImageDataKind.Error)
					{
						PixelWidth = 0;
						PixelHeight = 0;
						RaiseImageFailed(imageData.Error);
					}
					else if (imageData.Kind == ImageDataKind.CompositionSurface)
					{
						var image = imageData.CompositionSurface.Image;
						PixelWidth = image.Width;
						PixelHeight = image.Height;
						RaiseImageOpened();
					}

					return imageData;
				}
				else if (_stream != null)
				{
					return await ImageSourceHelpers.ReadFromStreamAsCompositionSurface(_stream.AsStream(), ct);
				}
			}
			catch (Exception e)
			{
				return ImageData.FromError(e);
			}

			return default;
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
				Path.Combine(Package.Current.InstalledPath,
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
