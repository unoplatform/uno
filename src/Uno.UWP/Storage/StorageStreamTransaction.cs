#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.Storage
{
	public partial class StorageStreamTransaction : IDisposable
	{
		private readonly object _gate;
		private readonly StorageFile _file;

		private IDisposable _fileLock;
		private OwnedStream _tempFile;

		public StorageStreamTransaction(StorageFile file)
		{
			_file = file;
			_gate = _file.Path; // Set the gate to the file path in order to prevent concurrent access (only for this process unfortunately)

			OpenFiles();
		}

		public Stream GetStream()
		{
			return _tempFile;
		}

		public async Task CommitAsync(CancellationToken ct)
		{
			lock (_gate)
			{
				ct.ThrowIfCancellationRequested();

				CloseFiles();
				File.Replace(_file.Path + ".tmp", _file.Path, null);
				OpenFiles();
			}
		}

		private void OpenFiles()
		{
			_fileLock = File.Open(_file.Path, FileMode.Open, FileAccess.Read, FileShare.None);
			_tempFile = new OwnedStream(File.Open(_file.Path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None), this);
		}

		private void CloseFiles()
		{
			_fileLock.Dispose();
			_tempFile.DisposeBy(this);
			_tempFile.Dispose();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		private void Dispose(bool isDisposing)
		{
			if (isDisposing)
			{
			}

			lock (_gate)
			{
				CloseFiles();

				try
				{
					File.Delete(_file.Path + ".tmp");
				}
				catch (Exception e)
				{
					this.Log().Error("Could not delete temporary file.", e);
				}
			}
		}

		~StorageStreamTransaction()
		{
			Dispose(false);
		}
	}
}
