#nullable enable

using System;
using System.IO;
using System.Security;
using Windows.ApplicationModel;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class LinuxAppNotificationAssetResolver
{
	private static readonly char[] PathSeparators = ['/', '\\'];

	public static string ResolveIcon(string source)
		=> ResolveIcon(source, Package.Current.InstalledPath, Package.Current.Id.Name, File.Exists);

	internal static string ResolveIcon(
		string source,
		string installedPath,
		string packageName,
		Func<string, bool> fileExists)
	{
		ArgumentNullException.ThrowIfNull(fileExists);
		if (string.IsNullOrEmpty(source) ||
			!Uri.TryCreate(source, UriKind.Absolute, out var uri) ||
			uri.Query.Length > 0 ||
			uri.Fragment.Length > 0)
		{
			return string.Empty;
		}

		try
		{
			if (HasUnsafeOriginalPath(source))
			{
				return string.Empty;
			}

			if (uri.IsFile)
			{
				if (uri.IsUnc || uri.Host.Length > 0)
				{
					return string.Empty;
				}
				var filePath = Path.GetFullPath(uri.LocalPath);
				return fileExists(filePath) ? uri.AbsoluteUri : string.Empty;
			}

			if (!uri.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase) ||
				uri.UserInfo.Length > 0 ||
				!uri.IsDefaultPort ||
				(uri.Host.Length > 0 && !uri.Host.Equals(packageName, StringComparison.OrdinalIgnoreCase)) ||
				string.IsNullOrEmpty(installedPath))
			{
				return string.Empty;
			}

			var decodedPath = Uri.UnescapeDataString(uri.AbsolutePath);
			if (decodedPath.Length <= 1 ||
				decodedPath[0] != '/' ||
				decodedPath[1] == '/' ||
				decodedPath.Contains('\\', StringComparison.Ordinal))
			{
				return string.Empty;
			}

			var packageRoot = Path.GetFullPath(installedPath);
			var relativePath = decodedPath[1..].Replace('/', Path.DirectorySeparatorChar);
			var assetPath = Path.GetFullPath(Path.Combine(packageRoot, relativePath));
			if (!IsWithinPackage(packageRoot, assetPath) || !fileExists(assetPath))
			{
				return string.Empty;
			}

			return new UriBuilder
			{
				Scheme = Uri.UriSchemeFile,
				Host = string.Empty,
				Path = assetPath,
			}.Uri.AbsoluteUri;
		}
		catch (Exception exception) when (exception is ArgumentException or FormatException or IOException or NotSupportedException or SecurityException or UnauthorizedAccessException)
		{
			return string.Empty;
		}
	}

	private static bool HasUnsafeOriginalPath(string source)
	{
		var separator = source.IndexOf(':');
		return separator < 0 ||
			HasUnsafeSegments(Uri.UnescapeDataString(source[(separator + 1)..]));
	}

	private static bool HasUnsafeSegments(string path)
	{
		if (path.IndexOf('\0') >= 0)
		{
			return true;
		}
		foreach (var segment in path.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries))
		{
			if (segment is "." or "..")
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsWithinPackage(string packageRoot, string path)
	{
		var relativePath = Path.GetRelativePath(packageRoot, path);
		return !Path.IsPathRooted(relativePath) &&
			!relativePath.Equals("..", StringComparison.Ordinal) &&
			!relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
			!relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
	}
}
