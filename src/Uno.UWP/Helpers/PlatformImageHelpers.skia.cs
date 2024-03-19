using System;
using System.Collections.Generic;
using System.IO;
using Windows.ApplicationModel;
using Windows.Graphics.Display;

namespace Uno.Helpers;

internal static partial class PlatformImageHelpers
{
	// TODO: Introduce LRU caching if needed
	private static readonly Dictionary<string, Uri> _scaledBitmapCache = new();

	internal static Uri GetScaledPath(Uri uri)
	{
		var path = uri.PathAndQuery;
		if (uri.Host is { Length: > 0 } host)
		{
			path = host + "/" + path.TrimStart('/');
		}

		// Avoid querying filesystem if we already seen this file
		if (_scaledBitmapCache.TryGetValue(path, out var result))
		{
			return result;
		}

		var originalLocalPath =
			Path.Combine(Package.Current.InstalledPath,
				 path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
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

		result = new(applicableScale ?? originalLocalPath);
		_scaledBitmapCache[path] = result;
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
