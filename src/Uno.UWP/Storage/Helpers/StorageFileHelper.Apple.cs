#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Foundation;

namespace Windows.Storage.Helpers;

partial class StorageFileHelper
{
	private static Task<bool> FileExistsInPackage(string fileName)
	{
		var directoryName = Path.GetDirectoryName(fileName) ?? string.Empty;
		var fn = Path.GetFileNameWithoutExtension(fileName);
		var fileExtension = Path.GetExtension(fileName);

		var resourcePathname = NSBundle.MainBundle.PathForResource(Path.Combine(directoryName, fn), fileExtension.Substring(1));

		return Task.FromResult(resourcePathname != null);
	}
}
