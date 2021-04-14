using System;
using System.Globalization;
using Windows.ApplicationModel;
using Windows.Storage;
using Path = System.IO.Path;

namespace Uno.Helpers
{
	/// <summary>
	/// Logic is based on https://docs.microsoft.com/en-us/windows/uwp/app-resources/uri-schemes#ms-appdata.
	/// </summary>
	internal static class AppDataUriEvaluator
	{
		private const string AppdataSchema = "ms-appdata";
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

			if (!appdataUri.Scheme.Equals(AppdataSchema, StringComparison.InvariantCultureIgnoreCase))
			{
				throw new ArgumentOutOfRangeException(nameof(appdataUri), "URI must be ms-appdata.");
			}

			if (!string.IsNullOrEmpty(appdataUri.Host))
			{
				// is host is provided, it must be equal to package name
				if (!appdataUri.Host.Equals(Package.Current.Id.Name, StringComparison.InvariantCultureIgnoreCase))
				{
					throw new ArgumentOutOfRangeException(nameof(appdataUri), $"When provided, the URI host must match package name ('{Package.Current.Id.Name}').");
				}
			}

			var path = appdataUri.AbsolutePath;

			if (path == string.Empty)
			{
				throw new ArgumentOutOfRangeException(nameof(appdataUri), "URI path must not be empty.");
			}

			// normalize slash type
			path = Path.GetFullPath(path);

			var root = Path.GetPathRoot(path);
			var relativePath = path.Substring(root.Length);

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

			var directorySeparator = Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);

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
