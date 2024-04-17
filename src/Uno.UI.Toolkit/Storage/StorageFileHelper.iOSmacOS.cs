#nullable enable
using System.Reflection;
using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Foundation;
using System.Threading.Tasks;

namespace Uno.UI.Toolkit;

partial class StorageFileHelper
{
	private static Task<bool> FileExistsInPackage(string fileName)
	{
		var directoryName = global::System.IO.Path.GetDirectoryName(fileName) ?? string.Empty;
		var fn = global::System.IO.Path.GetFileNameWithoutExtension(fileName);
		var fileExtension = global::System.IO.Path.GetExtension(fileName);

		var resourcePathname = NSBundle.MainBundle.PathForResource(global::System.IO.Path.Combine(directoryName, fn), fileExtension.Substring(1));

		return Task.FromResult(resourcePathname != null);
	}

	private static Task<string[]> GetFilesInDirectory(Func<string, bool> predicate)
	{
		string rootPath = AppDomain.CurrentDomain.BaseDirectory;
		string[] files = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);

		var results = files?.Where(e => predicate(e))
			.Select(e => e.Replace(rootPath, string.Empty).Replace('\\', '/'))
			.ToArray() ?? Array.Empty<string>();

		return Task.FromResult(results);
	}
}
