#nullable enable

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Uno;
using Uno.Helpers;

namespace Windows.Storage
{
	public sealed partial class StorageFile : IStorageFile, IStorageFile2, IStorageItem, IStorageItem2
	{
		public static IAsyncOperation<StorageFile> GetFileFromPathAsync(string path)
			=> AsyncOperation.FromTask(ct => Task.FromResult(new StorageFile(new Local(path))));

		internal static StorageFile GetFileFromPath(string path)
			=> new StorageFile(new Local(path));

		[NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
		public static IAsyncOperation<StorageFile> GetFileFromApplicationUriAsync(Uri uri)
			=> AsyncOperation.FromTask(ct =>
			{
				if (uri.IsAppData())
				{
					return Task.FromResult(GetFileFromPath(AppDataUriEvaluator.ToPath(uri)));
				}

				return GetFileFromApplicationUri(ct, uri);
			});

#if IS_UNIT_TESTS || __NETSTD_REFERENCE__
		private static Task<StorageFile> GetFileFromApplicationUri(CancellationToken ct, Uri uri)
			=> throw new NotImplementedException();
#endif

		private StorageFile(ImplementationBase implementation)
		{
			Implementation = implementation;
			Implementation.InitOwner(this);
		}

		internal ImplementationBase Implementation { get; }

		/// <summary>
		/// Allows internal Uno implementations to override the storage provider.
		/// </summary>
		internal StorageProvider? ProviderOverride { get; set; }

		public StorageProvider Provider => ProviderOverride ?? Implementation.Provider;

		public string Path => Implementation.Path;

		public string FileType => Implementation.FileType;

		public string Name => Implementation.Name;

		public string DisplayName => Implementation.DisplayName;

		public string ContentType => Implementation.ContentType;

		public DateTimeOffset DateCreated => Implementation.DateCreated;

		public bool IsOfType(StorageItemTypes type)
			=> type == StorageItemTypes.File;

		public bool IsEqual(IStorageItem item)
			=> Implementation.IsEqual(item);

		#region internal API (Task)

		internal Task<StorageFolder?> GetParent(CancellationToken ct)
			=> Implementation.GetParentAsync(ct);

		internal Task<BasicProperties> GetBasicProperties(CancellationToken ct)
			=> Implementation.GetBasicPropertiesAsync(ct);

		internal Task<IRandomAccessStreamWithContentType> Open(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
			=> Implementation.OpenAsync(ct, accessMode, options);

		internal Task<Stream> OpenStream(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
			=> Implementation.OpenStreamAsync(ct, accessMode, options);

		internal Task<StorageStreamTransaction> OpenTransactedWrite(CancellationToken ct, StorageOpenOptions option)
			=> Implementation.OpenTransactedWriteAsync(ct, option);

		internal Task Delete(CancellationToken ct, StorageDeleteOption options)
			=> Implementation.DeleteAsync(ct, options);

		internal Task Rename(CancellationToken ct, string desiredName, NameCollisionOption option)
			=> Implementation.RenameAsync(ct, desiredName, option);

		internal Task<StorageFile> Copy(CancellationToken ct, IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			=> Implementation.CopyAsync(ct, destinationFolder, desiredNewName, option);

		internal Task CopyAndReplace(CancellationToken ct, IStorageFile target)
			=> Implementation.CopyAndReplaceAsync(ct, target);

		internal Task Move(CancellationToken ct, IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			=> Implementation.MoveAsync(ct, destinationFolder, desiredNewName, option);

		internal Task MoveAndReplace(CancellationToken ct, IStorageFile target)
			=> Implementation.MoveAndReplaceAsync(ct, target);

		#endregion

		#region public API (IAsync<Action|Operation>)
		public IAsyncOperation<StorageFolder?> GetParentAsync()
			=> AsyncOperation.FromTask(ct => Implementation.GetParentAsync(ct));

		public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync()
			=> AsyncOperation.FromTask(ct => Implementation.GetBasicPropertiesAsync(ct));

		public IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
			=> AsyncOperation.FromTask(async ct => await Implementation.OpenAsync(ct, FileAccessMode.Read, StorageOpenOptions.AllowReadersAndWriters));

		public IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
			=> AsyncOperation.FromTask<IRandomAccessStream>(async ct => await Implementation.OpenAsync(ct, accessMode, StorageOpenOptions.AllowReadersAndWriters));

		public IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options)
			=> AsyncOperation.FromTask<IRandomAccessStream>(async ct => await Implementation.OpenAsync(ct, accessMode, options));

		public IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync()
			=> AsyncOperation.FromTask(async ct => await Implementation.OpenTransactedWriteAsync(ct, StorageOpenOptions.AllowReadersAndWriters));

		[NotImplemented] // The options is ignored, we implement this only to increase compatibility
		public IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options)
			=> AsyncOperation.FromTask(async ct => await Implementation.OpenTransactedWriteAsync(ct, options));

		public IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder)
			=> AsyncOperation.FromTask(async ct => await Implementation.CopyAsync(ct, destinationFolder, global::System.IO.Path.GetFileName(Path), NameCollisionOption.FailIfExists));

		public IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> AsyncOperation.FromTask(async ct => await Implementation.CopyAsync(ct, destinationFolder, desiredNewName, NameCollisionOption.FailIfExists));

		public IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			=> AsyncOperation.FromTask(async ct => await Implementation.CopyAsync(ct, destinationFolder, desiredNewName, option));

		public IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace)
			=> AsyncAction.FromTask(ct => Implementation.CopyAndReplaceAsync(ct, fileToReplace));

		public IAsyncAction RenameAsync(string desiredName)
			=> AsyncAction.FromTask(ct => Implementation.RenameAsync(ct, desiredName, NameCollisionOption.FailIfExists));

		public IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
			=> AsyncAction.FromTask(ct => Implementation.RenameAsync(ct, desiredName, option));

		public IAsyncAction MoveAsync(IStorageFolder destinationFolder)
			=> AsyncAction.FromTask(ct => Implementation.MoveAsync(ct, destinationFolder, Name, NameCollisionOption.FailIfExists));

		public IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> AsyncAction.FromTask(ct => Implementation.MoveAsync(ct, destinationFolder, desiredNewName, NameCollisionOption.FailIfExists));

		public IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			=> AsyncAction.FromTask(ct => Implementation.MoveAsync(ct, destinationFolder, desiredNewName, option));

		public IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
			=> AsyncAction.FromTask(ct => Implementation.MoveAndReplaceAsync(ct, fileToReplace));

		public IAsyncAction DeleteAsync() => DeleteAsync(StorageDeleteOption.Default);

		[NotImplemented] // The options is ignored, we implement this only to increase compatibility
		public IAsyncAction DeleteAsync(StorageDeleteOption option)
			=> AsyncAction.FromTask(ct => Implementation.DeleteAsync(ct, option));
		#endregion

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

		#region Helpers
		private static FileAccess ToFileAccess(FileAccessMode accessMode)
			=> accessMode switch
			{
				FileAccessMode.Read => FileAccess.Read,
				FileAccessMode.ReadWrite => FileAccess.ReadWrite,
				_ => throw new ArgumentOutOfRangeException(nameof(accessMode))
			};

		/// <summary>
		/// Maps FileAccessMode to the appropriate FileMode for file operations.
		/// </summary>
		/// <param name="accessMode">The access mode requested for the file.</param>
		/// <returns>
		/// FileMode.Open for Read access (requires file to exist),
		/// FileMode.OpenOrCreate for ReadWrite access (creates file if it doesn't exist).
		/// </returns>
		private static FileMode ToFileMode(FileAccessMode accessMode)
			=> accessMode switch
			{
				FileAccessMode.Read => FileMode.Open,
				FileAccessMode.ReadWrite => FileMode.OpenOrCreate,
				_ => throw new ArgumentOutOfRangeException(nameof(accessMode))
			};

		private static FileShare ToFileShare(StorageOpenOptions options)
			=> options switch
			{
				StorageOpenOptions.None => FileShare.None,
				StorageOpenOptions.AllowOnlyReaders => FileShare.Read,
				StorageOpenOptions.AllowReadersAndWriters => FileShare.ReadWrite,
				_ => throw new ArgumentOutOfRangeException(nameof(options))
			};

		private static async Task<StorageFile> CreateDestination(CancellationToken ct, IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
		{
			var creationOption = option switch
			{
				NameCollisionOption.FailIfExists => CreationCollisionOption.FailIfExists,
				NameCollisionOption.GenerateUniqueName => CreationCollisionOption.GenerateUniqueName,
				NameCollisionOption.ReplaceExisting => CreationCollisionOption.ReplaceExisting,
				_ => throw new ArgumentOutOfRangeException(nameof(option)),
			};

			return await destinationFolder.CreateFileAsync(desiredNewName, creationOption).AsTask(ct);
		}
		#endregion
	}
}
