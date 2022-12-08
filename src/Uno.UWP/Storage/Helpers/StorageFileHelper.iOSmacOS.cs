#if __IOS__ || MACCATALYST || MACOS
using System.Reflection;
using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Foundation;
using System.Threading.Tasks;

namespace Windows.Storage.Helpers
{
	partial class StorageFileHelper
	{
		private static Task<bool> FileExistsInPackage(string filename)
		{
			var directoryName = global::System.IO.Path.GetDirectoryName(filename) + string.Empty;
			var fileName = global::System.IO.Path.GetFileNameWithoutExtension(filename);
			var fileExtension = global::System.IO.Path.GetExtension(filename);

			var resourcePathname = NSBundle.MainBundle.PathForResource(global::System.IO.Path.Combine(directoryName, fileName), fileExtension.Substring(1));

			return Task.FromResult(resourcePathname != null);
		}
	}
}
#endif
