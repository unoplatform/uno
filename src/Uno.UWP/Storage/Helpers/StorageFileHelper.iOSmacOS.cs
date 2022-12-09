#nullable enable
#if __IOS__ || MACCATALYST || MACOS
using System.Reflection;
using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Foundation;
using System.Threading.Tasks;

namespace Windows.Storage.Helpers;

	partial class StorageFileHelper
	{
		private static Task<bool> FileExistsInPackage(string fileName)
		{
			var directoryName = global::System.IO.Path.GetDirectoryName(fileName) + string.Empty;
			var fn = global::System.IO.Path.GetFileNameWithoutExtension(fileName);
			var fileExtension = global::System.IO.Path.GetExtension(fileName);

			var resourcePathname = NSBundle.MainBundle.PathForResource(global::System.IO.Path.Combine(directoryName, fn), fileExtension.Substring(1));

			return Task.FromResult(resourcePathname != null);
		}
	}

#endif
