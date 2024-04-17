#nullable enable
using System.Threading;
using System;
using System.Threading.Tasks;
using Uno;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Uno.UI.Toolkit;

public partial class StorageFileHelper
{
	/// <summary>
	/// Determines if an asset or resource exists within application package
	/// </summary>
	/// <param name="fileName">relative file path</param>
	/// <returns>A task that will complete with a result of true if file exists, otherwise with a result of false.</returns>
	public static async Task<bool> ExistsInPackage(string fileName) => await FileExistsInPackage(fileName);

	/// <summary>
	/// This asynchronous method retrieves the paths of files within a directory based on a specified filter for file extensions.
	/// </summary>
	/// <param name="extensionsFilter">An array of strings representing the file extensions to filter the files.If null, all files in the directory are considered.</param>
	/// <returns>An array of strings containing the paths of the filtered files within the directory.</returns>
	public static async Task<string[]> GetFilesInDirectoryAsync(string[] extensionsFilter)
		=> await GetFilesInDirectory(e => extensionsFilter == null || extensionsFilter.Any(filter => e.EndsWith(filter, StringComparison.OrdinalIgnoreCase)));

#if IS_UNIT_TESTS || __NETSTD_REFERENCE__
	private static Task<bool> FileExistsInPackage(string fileName)
		=> throw new NotImplementedException();

	private static Task<string[]> GetFilesInDirectory(Func<string, bool> predicate)
		=> throw new NotImplementedException();
#endif

#if __SKIA__ || WINDOWS || WINAPPSDK || WINDOWS_UWP || WINUI
	private static Task<bool> FileExistsInPackage(string fileName)
	{
		var executingPath = Assembly.GetExecutingAssembly().Location;
		if (!string.IsNullOrEmpty(executingPath))
		{
			var path = Path.GetDirectoryName(executingPath);
			if (!string.IsNullOrEmpty(path))
			{
				var fullPath = Path.Combine(path, fileName);
				return Task.FromResult(File.Exists(fullPath));
			}
		}

		return Task.FromResult(false);
	}

	private static Task<string[]> GetFilesInDirectory(Func<string, bool> predicate)
	{
		List<string> assetsFiles = new();
		string path = string.Empty;
		var executingPath = Assembly.GetExecutingAssembly().Location;
		if (!string.IsNullOrEmpty(executingPath))
		{
			path = Path.GetDirectoryName(executingPath) + string.Empty;
			if (!string.IsNullOrEmpty(path))
			{
				assetsFiles = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
										.Where(e => predicate(e))
										.Select(e => e.Replace(path, string.Empty).Replace('\\', '/'))
										.ToList();
			}
		}
		return Task.FromResult(assetsFiles.ToArray());
	}
#endif
}
