#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Uno.Extensions;
#if !NETSTANDARD
using Uno.Foundation.Logging;
#endif

namespace Uno.UI.Xaml
{
	internal static class XamlFilePathHelper
	{
		public const string AppXIdentifier = AppXScheme + ":///";
		public const string AppXScheme = "ms-appx";
		public const string MSResourceScheme = "ms-resource";
		public const string MSResourceIdentifier = MSResourceScheme + ":///";
		private const string MsResourceFilesFolder = "Files/";
		public const string MsResourceFilesPrefix = MSResourceIdentifier + MsResourceFilesFolder;
		public const string WinUICompactURL = "Microsoft.UI.Xaml/DensityStyles/Compact.xaml";

#if !NETSTANDARD
		/// <summary>
		/// Converts the MRT local-resource form the XAML compiler emits for relative URIs
		/// (<c>ms-resource:///Files/logo.png</c>) to the equivalent <c>ms-appx:///logo.png</c>,
		/// which is the form asset resolution understands.
		/// </summary>
		internal static Uri NormalizeMsResourceFilesUri(Uri uri)
		{
			if (!uri.IsAbsoluteUri || !uri.Scheme.Equals(MSResourceScheme, StringComparison.Ordinal))
			{
				return uri;
			}

			// Matched on the parsed path rather than on the original string, so that a leading
			// whitespace or an upper-cased scheme the Uri parser accepted still resolves.
			var path = uri.PathAndQuery.TrimStart('/');

			// An authority names another package, whose assets ms-appx cannot reach - mapping it anyway
			// would silently resolve the *app's* asset of the same name.
			if (uri.Authority.Length > 0
				|| !path.StartsWith(MsResourceFilesFolder, StringComparison.OrdinalIgnoreCase)
				// The fragment is load-bearing for SvgImageSource and PathAndQuery drops it. Callers rely
				// on this being total, so a remainder that is not a valid URI stays untouched.
				|| !Uri.TryCreate(string.Concat(AppXIdentifier, path.AsSpan(MsResourceFilesFolder.Length), uri.Fragment.AsSpan()), UriKind.Absolute, out var appxUri))
			{
				// Only an ms-resource URI reaches here, and one that maps to nothing resolves to no
				// asset downstream without raising - so say so, or it just silently never appears.
				if (typeof(XamlFilePathHelper).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(XamlFilePathHelper).Log().Warn($"'{uri}' is not an MRT local-file resource ('{MsResourceFilesPrefix}…') and will not resolve to an asset.");
				}

				return uri;
			}

			// The value read back from the property is not the one being resolved, so trace the pair.
			if (typeof(XamlFilePathHelper).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(XamlFilePathHelper).Log().Debug($"Resolving local resource '{uri}' as '{appxUri}'");
			}

			return appxUri;
		}
#endif

		/// <summary>
		/// Convert relative source path to absolute path.
		/// </summary>
		internal static string ResolveAbsoluteSource(string origin, string relativeTargetPath)
		{
			if (IsAbsolutePath(relativeTargetPath))
			{
				// The path is already absolute. (Currently we assume it's in the local assembly.)
				var trimmedPath = relativeTargetPath.TrimStart(AppXIdentifier);
				return trimmedPath;
			}
#if NETSTANDARD
			else if (relativeTargetPath.StartsWith("/", StringComparison.Ordinal))
#else
			else if (relativeTargetPath.StartsWith('/'))
#endif
			{
				// Paths that start with '/' mean they're relative to the root (ie, absolute paths).
				// We remove the leading / because that's what the callers expect.
				return relativeTargetPath.Substring(1);
			}

			var originDirectory = Path.GetDirectoryName(origin);
			if (originDirectory.IsNullOrWhiteSpace())
			{
				return relativeTargetPath;
			}

			var absoluteTargetPath = GetAbsolutePath(originDirectory, relativeTargetPath);

			return absoluteTargetPath.Replace('\\', '/');
		}

		internal static bool IsAbsolutePath(string relativeTargetPath) => relativeTargetPath.StartsWith(AppXIdentifier, StringComparison.Ordinal)
			|| relativeTargetPath.StartsWith(MSResourceIdentifier, StringComparison.Ordinal);

		internal static string GetWinUIThemeResourceUrl(int version)
		{
			return version switch
			{
				1 => "Microsoft.UI.Xaml/Themes/themeresources_v1.xaml",
				2 => "Microsoft.UI.Xaml/Themes/themeresources_v2.xaml",
				_ => throw new ArgumentOutOfRangeException(nameof(version), $"'version' must be between 1 and 2. Found {version}."),
			};
		}

		private static string GetAbsolutePath(string originDirectory, string relativeTargetPath)
		{
			var addedRootLength = 0;
			if (Path.GetPathRoot(originDirectory) is { Length: 0 })
			{
				var localRoot = Path.GetPathRoot(Directory.GetCurrentDirectory())!;
				addedRootLength = localRoot.Length;
				// Prepend a dummy root so that GetFullPath doesn't try to add the working directory. We remove it immediately afterward.
				originDirectory = localRoot + originDirectory;
			}
			var absoluteTargetPath = Path.GetFullPath(
					Path.Combine(originDirectory, relativeTargetPath)
				);

			absoluteTargetPath = absoluteTargetPath.Substring(addedRootLength);

			return absoluteTargetPath;
		}

		internal static bool TryGetMsAppxAssetPath(string? uri, [NotNullWhen(true)] out string? path)
		{
			if (Uri.TryCreate(uri, UriKind.Absolute, out var newUri) && TryGetMsAppxAssetPath(newUri, out path))
			{
				return true;
			}
			else
			{
				path = null;
				return false;
			}
		}

		/// <summary>
		/// Builds an internal asset path based on the assembly name and asset path
		/// </summary>
		/// <param name="uri">An ms-appx schemed uri</param>
		/// <returns>The local asset path</returns>
		internal static bool TryGetMsAppxAssetPath(Uri uri, [NotNullWhen(true)] out string? path)
		{
			if (uri.IsAbsoluteUri && uri.Scheme.Equals(XamlFilePathHelper.AppXScheme, StringComparison.OrdinalIgnoreCase))
			{
				path = uri.PathAndQuery.TrimStart('/');

				return true;
			}
			else
			{
				path = null;
				return false;
			}
		}
	}
}
