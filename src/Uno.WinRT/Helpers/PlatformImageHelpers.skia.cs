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

#pragma warning disable RS0030 // Do not use banned APIs // TODO MZ: Avoid this by using XamlRoot
		var resolutionScale = (int)(scaleOverride ?? DisplayInformation.GetForCurrentView().ResolutionScale);
#pragma warning restore RS0030 // Do not use banned APIs

		result = ResolveScaledPath(path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar), resolutionScale);
		if (IsCacheable(result))
		{
			_scaledBitmapCache[path] = result;
		}

		return Task.FromResult(result);
	}

	/// <summary>
	/// Whether a resolved image path can be kept for the life of the process. A host that declared additional
	/// content roots adds files while it runs, so a path whose file is not there yet is resolved again.
	/// </summary>
	/// <param name="resolvedPath">The path <c>ResolveScaledPath</c> returned.</param>
	/// <returns><see langword="true"/> when the path can be cached.</returns>
	internal static bool IsCacheable(string resolvedPath)
		=> WinRTFeatureConfiguration.Package.AdditionalContentRoots.Count == 0 || File.Exists(resolvedPath);

	/// <summary>
	/// The file to load for an image of the app's content at a resolution scale: its scaled file when there is
	/// one, in the install folder first, then in each of
	/// <c>WinRTFeatureConfiguration.Package.AdditionalContentRoots</c>.
	/// </summary>
	/// <param name="relativePath">The image's path relative to a content root.</param>
	/// <param name="resolutionScale">The resolution scale, as a percentage.</param>
	/// <returns>The path of the file, or the image's path in the install folder when no root has it.</returns>
	internal static string ResolveScaledPath(string relativePath, int resolutionScale)
	{
		var installedPath = Path.Combine(Package.Current.InstalledPath, relativePath);
		if (FindApplicableScale(installedPath, resolutionScale) is { } installedScale)
		{
			return installedScale;
		}

		// No further file-system access unless a host declared additional roots.
		var additionalRoots = WinRTFeatureConfiguration.Package.AdditionalContentRoots;
		if (additionalRoots.Count == 0 || File.Exists(installedPath))
		{
			return installedPath;
		}

		foreach (var root in additionalRoots)
		{
			var candidate = Path.Combine(root, relativePath);
			if (FindApplicableScale(candidate, resolutionScale) is { } scaled)
			{
				return scaled;
			}

			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		return installedPath;
	}

	private static string FindApplicableScale(string localPath, int resolutionScale)
		=> FindApplicableScale(localPath, resolutionScale, onlyMatching: true)
			?? FindApplicableScale(localPath, resolutionScale, onlyMatching: false);

	private static string FindApplicableScale(string localPath, int resolutionScale, bool onlyMatching)
	{
		var baseDirectory = Path.GetDirectoryName(localPath);
		var baseFileName = Path.GetFileNameWithoutExtension(localPath);
		var baseExtension = Path.GetExtension(localPath);

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
