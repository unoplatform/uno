#nullable enable

using System;
using System.IO;
using System.Security;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AppleAppNotificationAssetPathResolver
{
	private static readonly char[] _pathSeparators = new[] { '/', '\\' };

	public static bool TryResolve(string source, string installedPath, out string path)
	{
		path = string.Empty;
		if (string.IsNullOrEmpty(source) || !Uri.TryCreate(source, UriKind.Absolute, out var uri))
		{
			return false;
		}

		try
		{
			if (HasUnsafeOriginalPath(source))
			{
				return false;
			}
			if (uri.IsFile)
			{
				if (uri.IsUnc || uri.Host.Length > 0 && !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				var unescapedPath = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
				if (HasUnsafeSegments(unescapedPath))
				{
					return false;
				}
				path = Path.GetFullPath(uri.LocalPath);
				return path.Length > 0;
			}

			if (!uri.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase) ||
				uri.Host.Length > 0 ||
				string.IsNullOrEmpty(installedPath))
			{
				return false;
			}

			var relativePath = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
			if (HasUnsafeSegments(relativePath))
			{
				return false;
			}
			relativePath = relativePath
				.Replace('\\', Path.DirectorySeparatorChar)
				.Replace('/', Path.DirectorySeparatorChar);
			var root = Path.GetFullPath(installedPath);
			var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
			var rootWithoutSeparator = Path.TrimEndingDirectorySeparator(root);
			var rootPrefix = Path.EndsInDirectorySeparator(root)
				? root
				: root + Path.DirectorySeparatorChar;
			if (!candidate.Equals(rootWithoutSeparator, StringComparison.Ordinal) &&
				!candidate.StartsWith(rootPrefix, StringComparison.Ordinal))
			{
				return false;
			}
			path = candidate;
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (IOException)
		{
			return false;
		}
		catch (NotSupportedException)
		{
			return false;
		}
		catch (UriFormatException)
		{
			return false;
		}
		catch (SecurityException)
		{
			return false;
		}
	}

	private static bool HasUnsafeOriginalPath(string source)
	{
		var separator = source.IndexOf(':');
		if (separator < 0)
		{
			return true;
		}
		var path = source.AsSpan(separator + 1);
		var suffix = path.IndexOfAny('?', '#');
		if (suffix >= 0)
		{
			path = path[..suffix];
		}
		return HasUnsafeSegments(Uri.UnescapeDataString(path.ToString()));
	}

	private static bool HasUnsafeSegments(string path)
	{
		if (path.IndexOf('\0') >= 0)
		{
			return true;
		}
		foreach (var segment in path.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries))
		{
			if (segment is "." or "..")
			{
				return true;
			}
		}
		return false;
	}
}
