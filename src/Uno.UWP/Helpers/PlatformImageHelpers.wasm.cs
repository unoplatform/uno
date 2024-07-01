using System;
using System.IO;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Graphics.Display;
using Windows.Storage.Helpers;

namespace Uno.Helpers;

internal static partial class PlatformImageHelpers
{
	internal static async Task<string> GetScaledPath(Uri uri, ResolutionScale? scaleOverride)
	{
		// When using ms-appx on Wasm Skia, we still get the uri as ms-appx://path/to/image.png
		// When using ms-appx on Wasm native, we get the uri as /path/to/image.png
		// For now, we handle both cases here.
		// However, we should align both callers to always use the same format.
		var isLocalResource = uri.IsLocalResource();
		var path = isLocalResource ? uri.PathAndQuery.TrimStart("/") : uri.OriginalString;

		if (!string.IsNullOrEmpty(path))
		{
			var assets = await AssetResolver.Assets;
			var directory = Path.GetDirectoryName(path);
			var filename = Path.GetFileNameWithoutExtension(path);
			var extension = Path.GetExtension(path);

#pragma warning disable RS0030 // Do not use banned APIs // TODO MZ: Avoid this by using XamlRoot
			var resolutionScale = scaleOverride == null ? (int)DisplayInformation.GetForCurrentView().ResolutionScale : (int)scaleOverride;
#pragma warning restore RS0030 // Do not use banned APIs

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
						if (isLocalResource)
						{
							// This is the case for Wasm Skia.
							return $"ms-appx:///{directory}/{filename}.scale-{probeScale}{extension}";
						}
						else
						{
							// This is the case for Wasm native.
							return AssetsPathBuilder.BuildAssetUri(filePath);
						}
					}
				}
			}

			if (isLocalResource)
			{
				return uri.OriginalString;
			}
			else
			{
				return AssetsPathBuilder.BuildAssetUri(path);
			}
		}

		return uri.OriginalString;
	}
}
