#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Windows.Storage
{
	public partial class StorageFile : StorageItem, IStorageFile
	{
		private Uri _fileUri;

		private string Scheme => _fileUri.Scheme.ToUpperInvariant();

		public string Path => _fileUri.LocalPath;

		public string Name => global::System.IO.Path.GetFileName(Path);

		public string DisplayName => global::System.IO.Path.GetFileNameWithoutExtension(Path);

		public static async Task<StorageFile> GetFileFromPathAsync(string path)
		{
			return new StorageFile(new Uri("file://" + path));
		}

		private StorageFile(Uri uri)
		{
			_fileUri = uri;
		}

		public IAsyncAction DeleteAsync() =>
			AsyncAction.FromTask(async ct =>
			{
				await DeleteAsync(CancellationToken.None);
			});
			
		public async Task DeleteAsync(CancellationToken ct)
		{
			if (Scheme != "FILE")
			{
				throw new InvalidOperationException("Cannot delete a file on a non local storage.");
			}

			var fileInfo = new FileInfo(Path);

			fileInfo.Delete();
		}

		public global::System.DateTimeOffset DateCreated => File.GetCreationTime(Path);

#if false
		public async Task<Stream> GetThumbnailAsync(CancellationToken ct, ThumbnailMode mode, int size)
		{
			return await GetThumbnailAsync(ct, mode);
		}
		public async Task<Stream> GetThumbnailAsync(CancellationToken ct, ThumbnailMode mode)
		{
			var stream = await UIThread.Current.Run(async ict =>
			{
				try
				{
					var fileUrl = NSUrl.FromFilename(this.Path);
					var movie = new MPMoviePlayerController(fileUrl);

					movie.ShouldAutoplay = false;
					movie.Stop();

					var image = movie.ThumbnailImageAt(1.0, MPMovieTimeOption.Exact);

					movie.SafeDispose();

					return image.AsPNG().AsStream();
				}

				catch (Exception e)
				{
					this.Log().Error("The thumbnail could not retrieved.", e);
					return null;
				}
					
			}, ct);

			return stream;
		}
#endif

		[Uno.NotImplemented]
        public global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenAsync(global::Windows.Storage.FileAccessMode accessMode)
        {
            throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> StorageFile.OpenAsync(FileAccessMode accessMode) is not implemented in Uno.");
        }

        private class FileRandomAccessStream : Windows.Storage.Streams.IRandomAccessStream
        {
            private readonly string _path;
            private readonly FileAccessMode _accessMode;
            private readonly Stream _source;

            public FileRandomAccessStream(string path, global::Windows.Storage.FileAccessMode accessMode)
            {
                _path = path;
                _accessMode = accessMode;
                _source = File.OpenRead(path);
            }

            public bool CanRead => _source.CanRead;

            public bool CanWrite => _source.CanWrite;

            public ulong Position => (ulong)_source.Position;

            public ulong Size {
                get => (ulong)_source.Length;
                set => throw new NotSupportedException("Setting the stream size is not supported");
            }

            public IRandomAccessStream CloneStream() => new FileRandomAccessStream(_path, _accessMode);
            public void Dispose() => _source.Dispose();
            public IAsyncOperation<bool> FlushAsync() => AsyncOperation.FromTask(async ct => { await _source.FlushAsync(); return true; });
            public IInputStream GetInputStreamAt(ulong position) => throw new NotImplementedException();
            public IOutputStream GetOutputStreamAt(ulong position) => throw new NotImplementedException();
            public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) => throw new NotImplementedException();
            public void Seek(ulong position) => throw new NotImplementedException();
            public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => throw new NotImplementedException();
        }


        public async Task<Stream> OpenStreamForReadAsync(CancellationToken ct)
		{
			switch (Scheme)
			{
				default:
					return File.OpenRead(Path);
			}
		}

		public async Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct)
		{
			if (Scheme != "FILE")
			{
				throw new InvalidOperationException("Cannot write on a non local file.");
			}

			return new StorageStreamTransaction(this);
		}

		public async Task CopyAndReplaceAsync(CancellationToken ct, StorageFile destination)
		{
			switch (Scheme)
			{
				default:
					File.Copy(this.Name, destination.Name, true);
					break;
			}
		}

		public async Task MoveAsync(CancellationToken ct, StorageFolder targetFolder)
		{
			await MoveAsync(ct, targetFolder, Name, NameCollisionOption.FailIfExists);
		}

		public async Task MoveAsync(CancellationToken ct, StorageFolder targetFolder, string desiredNewName)
		{
			await MoveAsync(ct, targetFolder, desiredNewName, NameCollisionOption.FailIfExists);
		}

		public async Task MoveAsync(CancellationToken ct, StorageFolder targetFolder, string desiredNewName, NameCollisionOption option)
		{
			// TODO: Check the _scheme of the target folder
			var targetPath = global::System.IO.Path.Combine(targetFolder.Path, desiredNewName);

			if (File.Exists(targetPath))
			{
				switch (option)
				{
					case NameCollisionOption.FailIfExists:
						throw new IOException("File {0} already exists".InvariantCultureFormat(targetPath));

					case NameCollisionOption.GenerateUniqueName:
						var extension = global::System.IO.Path.GetExtension(desiredNewName);
						desiredNewName = global::System.IO.Path.ChangeExtension(desiredNewName, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + extension);
						await MoveAsync(ct, targetFolder, desiredNewName, option);
						return;

					case NameCollisionOption.ReplaceExisting:
						File.Delete(targetPath);
						break;

					default:
						throw new ArgumentOutOfRangeException("option");
				}
			}

			switch (Scheme)
			{
				default:
					File.Move(Path, targetPath);
					break;
			}

			_fileUri = new Uri("file://" + targetPath);
		}

		public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync()
			=> GetBasicPropertiesAsync(new CancellationToken()).AsAsyncOperation<BasicProperties>();

		public async Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct)
		{
			return new BasicProperties(this);
		}
	}

	public enum ThumbnailMode
	{
		MusicView,
		VideosView
	}

	public enum NameCollisionOption
	{
		// Summary:
		//     Automatically generate a unique name by appending a number to the name of
		//     the file or folder.
		GenerateUniqueName = 0,
		//
		// Summary:
		//     Replace the existing file or folder. Your app must have permission to access
		//     the location that contains the existing file or folder. Access to a location
		//     can be granted in several ways, for example, by a capability declared in
		//     your application's manifest, or by the user through the file picker. You
		//     can use Windows.Storage.AccessCache to manage the list of locations that
		//     are accessible to your app via the file picker.
		ReplaceExisting = 1,
		//
		// Summary:
		//     Return an error if another file or folder exists with the same name and abort
		//     the operation.
		FailIfExists = 2,
	}
}
