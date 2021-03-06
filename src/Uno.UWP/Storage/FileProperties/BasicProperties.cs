using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Windows.Storage.FileProperties
{
	public sealed partial class BasicProperties
	{
		internal BasicProperties(ulong size, DateTimeOffset dateModified)
		{
			Size = size;
			DateModified = dateModified;
		}

		internal static BasicProperties FromFilePath(string filePath)
		{
			var fileInfo = new FileInfo(filePath);
			return new BasicProperties((ulong)fileInfo.Length, File.GetLastWriteTime(fileInfo.FullName));
		}

		internal static BasicProperties FromDirectoryPath(string directoryPath) =>
			new BasicProperties(0UL, Directory.GetLastWriteTime(directoryPath));

		public ulong Size { get; }

		public DateTimeOffset DateModified { get; }
	}

}
