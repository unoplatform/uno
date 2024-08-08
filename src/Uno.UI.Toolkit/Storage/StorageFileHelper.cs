#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uno;

namespace Uno.UI.Toolkit;

public partial class StorageFileHelper
{
	/// <summary>
	/// Determines if an asset or resource exists within application package
	/// </summary>
	/// <param name="fileName">relative file path</param>
	/// <returns>A task that will complete with a result of true if file exists, otherwise with a result of false.</returns>
	public static async Task<bool> ExistsInPackage(string fileName)
	{
#if HAS_UNO
		return await Windows.Storage.Helpers.StorageFileHelper.ExistsInPackage(fileName);
#else
		return await FileExistsInPackage(fileName);
#endif
	}

#if !HAS_UNO
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
#endif
}
