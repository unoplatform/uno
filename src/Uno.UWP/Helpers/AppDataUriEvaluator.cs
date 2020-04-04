using System;
using System.Globalization;
using System.IO;
using Windows.ApplicationModel;
using Windows.Storage;
using Path = System.IO.Path;

namespace Uno.Helpers
{
	/// <summary>
	/// Logic is based on <see cref="https://docs.microsoft.com/en-us/windows/uwp/app-resources/uri-schemes#ms-appdata" />.
	/// </summary>
	internal static class AppDataUriEvaluator
	{
		private const string LocalFolderRoute = "local";
		private const string RoamingFolderRoute = "roaming";
		private const string TemporaryFolderRoute = "temp";		

		/// <summary>
		/// Converts given ms-appdata: URI to filesystem path.
		/// </summary>
		/// <param name="appdataUri">ms-appdata: URI.</param>
		/// <returns>Filesystem path.</returns>
		public static string ToPath(Uri appdataUri)
		{
			if (appdataUri == null)
			{
				throw new ArgumentNullException(nameof(appdataUri));
			}

			if (!appdataUri.IsAbsoluteUri)
			{
				throw new ArgumentOutOfRangeException(nameof(appdataUri), "URI must be absolute.");
			}

			if (!appdataUri.Scheme.Equals("ms-appdata", StringComparison.InvariantCultureIgnoreCase))
			{
				throw new ArgumentOutOfRangeException(nameof(appdataUri), "URI must be ms-appdata.");
			}

			var path = appdataUri.AbsolutePath;
			if (!Path.IsPathRooted(path))
			{
				throw new ArgumentOutOfRangeException(nameof(appdataUri), "URI path must be rooted.");
			}

			var root = Path.GetPathRoot(path);
			var relativePath = Path.GetRelativePath(root, path);

			var directorySeparator = Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);

			// check if path starts with package name
			if (relativePath.IndexOf(Package.Current.Id.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
			{
				// remove start from path
				relativePath = relativePath.Substring(Package.Current.Id.Name.Length);

				// slash must follow package's name
				if (!relativePath.StartsWith(directorySeparator))
				{
					throw new ArgumentOutOfRangeException("URI path start is invalid.");
				}
			}

			string appDataFolderPath = null;

			// locate the application data path
			if (relativePath.StartsWith(LocalFolderRoute, StringComparison.InvariantCultureIgnoreCase))
			{
				appDataFolderPath = ApplicationData.Current.LocalFolder.Path;
				relativePath = relativePath.Substring(LocalFolderRoute.Length);
			}
			else if (relativePath.StartsWith(RoamingFolderRoute, StringComparison.InvariantCultureIgnoreCase))
			{
				appDataFolderPath = ApplicationData.Current.RoamingFolder.Path;
				relativePath = relativePath.Substring(RoamingFolderRoute.Length);
			}
			else if (relativePath.StartsWith(TemporaryFolderRoute, StringComparison.InvariantCultureIgnoreCase))
			{
				appDataFolderPath = ApplicationData.Current.TemporaryFolder.Path;
				relativePath = relativePath.Substring(TemporaryFolderRoute.Length);
			}

			// relative path is now either empty (URI points to the folder itself) or must start with separator to be valid
			if (appDataFolderPath == null ||
				(relativePath != string.Empty && !relativePath.StartsWith(directorySeparator)))
			{
				throw new ArgumentOutOfRangeException(nameof(appdataUri), "URI must point to local, roaming or temp folder");
			}

			// Path.Combine recognizes leading / as root - it needs to be removed first
			if (relativePath.StartsWith(directorySeparator))
			{
				relativePath = relativePath.Substring(1);
			}

			var targetPath = Path.Combine(appDataFolderPath, relativePath);

			// perform URL decoding - to handle special cases like "ms-appdata://local/Hello%23World.html"
			return Uri.UnescapeDataString(targetPath);
		}
	}
}
