using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Storage.Helpers;

namespace Uno.Helpers;

internal static partial class PlatformImageHelpers
{
	internal static async Task<string> GetScaledPath(Uri uri, ResolutionScale? scaleOverride)
	{
		var path = uri.OriginalString;
		if (!string.IsNullOrEmpty(path))
		{
			var assets = await AssetResolver.Assets;
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
}
