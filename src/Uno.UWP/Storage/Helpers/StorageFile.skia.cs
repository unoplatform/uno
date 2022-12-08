using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Windows.Storage.Helpers
{
	partial class StorageFileHelper
	{
		private static Task<bool> FileExistsInPackage(string filename)
		{
			var executingPath = Assembly.GetExecutingAssembly().Location;
			if (!string.IsNullOrWhiteSpace(executingPath))
			{
				var path = Path.GetDirectoryName(executingPath);
				if (path is not null &&
					!string.IsNullOrWhiteSpace(path))
				{
					var fullPath = Path.Combine(path, filename);
					return Task.FromResult(File.Exists(fullPath));
				}
			}

			return Task.FromResult(true);
		}
	}
}
