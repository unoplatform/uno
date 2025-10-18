using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Graphics.Display;

namespace Uno.Helpers;

internal static partial class PlatformImageHelpers
{
	// TODO: Introduce LRU caching if needed
	private static readonly Dictionary<string, string> _scaledBitmapCache = new();

	internal static Task<string> GetScaledPath(Uri uri, ResolutionScale? scaleOverride)
	{
		// Unescaping is necessary for things like spaces that are valid in a file system path but not in a URI
		var path = Uri.UnescapeDataString(uri.AbsolutePath);
		if (uri.Host is { Length: > 0 } host)
		{
			path = host + "/" + path.TrimStart('/');
		}

		// Avoid querying filesystem if we already seen this file
		if (_scaledBitmapCache.TryGetValue(path, out var result))
		{
			return Task.FromResult(result);
		}

		var originalLocalPath =
			Path.Combine(Package.Current.InstalledPath,
				 path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
			);

#pragma warning disable RS0030 // Do not use banned APIs // TODO MZ: Avoid this by using XamlRoot
		var resolutionScale = (int)(scaleOverride ?? DisplayInformation.GetForCurrentView().ResolutionScale);
#pragma warning restore RS0030 // Do not use banned APIs
		var baseDirectory = Path.GetDirectoryName(originalLocalPath);
		var baseFileName = Path.GetFileNameWithoutExtension(originalLocalPath);
		var baseExtension = Path.GetExtension(originalLocalPath);
		var applicableScale = FindApplicableScale(true);
		if (applicableScale is null)
		{
			applicableScale = FindApplicableScale(false);
		}

		result = applicableScale ?? originalLocalPath;
		_scaledBitmapCache[path] = result;
		return Task.FromResult(result);

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
