#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Foundation;

namespace Windows.Storage
{
	partial class StorageFile
	{
		private static async Task<StorageFile> GetFileFromApplicationUri(CancellationToken ct, Uri uri)
		{
			if (uri.Scheme != "ms-appx")
			{
				throw new InvalidOperationException("Uri is not using the ms-appx scheme");
			}

			var path = uri.PathAndQuery.TrimStart(new char[] { '/' });

			var directoryName = global::System.IO.Path.GetDirectoryName(path);
			var fileName = global::System.IO.Path.GetFileNameWithoutExtension(path);
			var fileExtension = global::System.IO.Path.GetExtension(path);

			if (!string.IsNullOrEmpty(directoryName))
			{
				var resourcePathname = NSBundle.MainBundle.PathForResource(global::System.IO.Path.Combine(directoryName, fileName), fileExtension.Substring(1));

				if (resourcePathname != null)
				{
					return await StorageFile.GetFileFromPathAsync(resourcePathname);
				}
			}

			throw new FileNotFoundException($"The file [{path}] cannot be found in the bundle");
		}
	}
}
