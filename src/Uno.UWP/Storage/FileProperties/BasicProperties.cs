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

		internal BasicProperties FromPath(string filePath)
		{
			var fileInfo = new FileInfo(filePath);
			return new BasicProperties((ulong)fileInfo.Length, File.GetLastWriteTime(_fileInfo.FullName));
		}

		public ulong Size { get; }

		public DateTimeOffset DateModified { get; }
	}

}
