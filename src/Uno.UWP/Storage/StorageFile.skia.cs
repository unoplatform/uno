using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

			var baseDir = global::System.IO.Path.GetDirectoryName(global::System.Reflection.Assembly.GetExecutingAssembly().Location);
			var resourcePathname = global::System.IO.Path.Combine(baseDir, path);

			if (resourcePathname != null)
			{
				return await StorageFile.GetFileFromPathAsync(resourcePathname);
			}
			else
			{
				throw new FileNotFoundException($"The file [{path}] cannot be found  in the package directory");
			}
		}
	}
}
