using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Windows.Storage.FileProperties
{
	public sealed partial class BasicProperties
	{
		private readonly FileInfo _fileInfo;

		internal BasicProperties(StorageFile file)
		{
			_fileInfo = new FileInfo(file.Path);
		}

		public ulong Size
		{
			get { return (ulong)_fileInfo.Length; }
		}

		public DateTimeOffset DateModified => File.GetLastWriteTime(_fileInfo.FullName);
	}

}
