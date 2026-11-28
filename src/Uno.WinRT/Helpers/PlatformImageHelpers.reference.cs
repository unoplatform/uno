using System;
using System.Threading.Tasks;
using Windows.Graphics.Display;

namespace Uno.Helpers;

internal static partial class PlatformImageHelpers
{
	internal static Task<string> GetScaledPath(Uri uri, ResolutionScale? scaleOverride)
		=> throw new NotSupportedException("Reference assembly");

	internal static string ResolveScaledPath(string relativePath, int resolutionScale)
		=> throw new NotSupportedException("Reference assembly");

	internal static bool IsCacheable(string resolvedPath)
		=> throw new NotSupportedException("Reference assembly");
}
