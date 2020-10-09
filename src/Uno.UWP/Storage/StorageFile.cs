#nullable enable
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

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

namespace Windows.Storage
{
	public sealed partial class StorageFile : StorageItem, IStorageFile
	{
		public static IAsyncOperation<StorageFile> GetFileFromPathAsync(string path)
			=> AsyncOperation.FromTask(async ct => new StorageFile(new Local(path)));

		internal static StorageFile GetFileFromPath(string path)
			=> new StorageFile(new Local(path));

		[NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static IAsyncOperation<StorageFile> GetFileFromApplicationUriAsync(Uri uri)
			=> AsyncOperation.FromTask(ct => GetFileFromApplicationUri(ct, uri));

#if NET461 || __SKIA__ || __NETSTD_REFERENCE__
		private static Task<StorageFile> GetFileFromApplicationUri(CancellationToken ct, Uri uri)
			=> throw new NotImplementedException();
#endif

		private readonly ImplementationBase _impl;

		private StorageFile(ImplementationBase impl)
		{
			_impl = impl;
			_impl.InitOwner(this);
		}

		public string Path => _impl.Path;

		public string FileType => _impl.FileType;

		public string Name => _impl.Name;

		public string DisplayName => _impl.DisplayName;

		public string ContentType => _impl.ContentType;

		public DateTimeOffset DateCreated => _impl.DateCreated;

		public bool IsOfType(StorageItemTypes type)
			=> type == StorageItemTypes.File;

		public bool IsEqual(IStorageItem item)
			=> _impl.IsEqual(item);

		#region internal API (Task)
		internal Task<StorageFolder> GetParent(CancellationToken ct)
			=> _impl.GetParent(ct);

		internal Task<BasicProperties> GetBasicProperties(CancellationToken ct)
			=> _impl.GetBasicProperties(ct);

		internal Task<IRandomAccessStreamWithContentType> Open(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
			=> _impl.Open(ct, accessMode, options);

		internal Task<Stream> OpenStream(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
			=> _impl.OpenStream(ct, accessMode, options);

		internal Task<StorageStreamTransaction> OpenTransactedWrite(CancellationToken ct, StorageOpenOptions option)
			=> _impl.OpenTransactedWrite(ct, option);

		internal Task Delete(CancellationToken ct, StorageDeleteOption options)
			=> _impl.Delete(ct, options);

		internal Task Rename(CancellationToken ct, string desiredName, NameCollisionOption option)
			=> _impl.Rename(ct, desiredName, option);

		internal Task<StorageFile> Copy(CancellationToken ct, IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			=> _impl.Copy(ct, destinationFolder, desiredNewName, option);

		internal Task CopyAndReplace(CancellationToken ct, IStorageFile target)
			=> _impl.CopyAndReplace(ct, target);

		internal Task Move(CancellationToken ct, IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			=> _impl.Move(ct, destinationFolder, desiredNewName, option);

		internal Task MoveAndReplace(CancellationToken ct, IStorageFile target)
			=> _impl.MoveAndReplace(ct, target);
		#endregion

		#region public API (IAsync<Action|Operation>)
		public IAsyncOperation<StorageFolder> GetParentAsync()
			=> AsyncOperation.FromTask(_impl.GetParent);

		public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync()
			=> AsyncOperation.FromTask(_impl.GetBasicProperties);

		public IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
			=> AsyncOperation<IRandomAccessStreamWithContentType>.FromTask((ct, _) => _impl.Open(ct, FileAccessMode.Read, StorageOpenOptions.None));

		public IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
			=> AsyncOperation<IRandomAccessStream>.FromTask(async (ct, _) => await _impl.Open(ct, accessMode, StorageOpenOptions.None));

		public IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options)
			=> AsyncOperation<IRandomAccessStream>.FromTask(async (ct, _) => await _impl.Open(ct, accessMode, options));

		public IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync()
			=> AsyncOperation<StorageStreamTransaction>.FromTask((ct, _) => _impl.OpenTransactedWrite(ct, StorageOpenOptions.None));

		[NotImplemented] // The options is ignored, we implement this only to increase compatibility
		public IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options)
			=> AsyncOperation<StorageStreamTransaction>.FromTask((ct, _) => _impl.OpenTransactedWrite(ct, options));

		public IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder)
			=> AsyncOperation<StorageFile>.FromTask((ct, _) => _impl.Copy(ct, destinationFolder, global::System.IO.Path.GetFileName(Path), NameCollisionOption.FailIfExists));

		public IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> AsyncOperation<StorageFile>.FromTask((ct, _) => _impl.Copy(ct, destinationFolder, desiredNewName, NameCollisionOption.FailIfExists));

		public IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			=> AsyncOperation<StorageFile>.FromTask((ct, _) => _impl.Copy(ct, destinationFolder, desiredNewName, option));

		public IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace)
			=> AsyncAction.FromTask(ct => _impl.CopyAndReplace(ct, fileToReplace));

		public IAsyncAction RenameAsync(string desiredName)
			=> AsyncAction.FromTask(ct => _impl.Rename(ct, desiredName, NameCollisionOption.FailIfExists));

		public IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
			=> AsyncAction.FromTask(ct => _impl.Rename(ct, desiredName, option));

		public IAsyncAction MoveAsync(IStorageFolder destinationFolder)
			=> AsyncAction.FromTask(ct => _impl.Move(ct, destinationFolder, Name, NameCollisionOption.FailIfExists));

		public IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> AsyncAction.FromTask(ct => _impl.Move(ct, destinationFolder, desiredNewName, NameCollisionOption.FailIfExists));

		public IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			=> AsyncAction.FromTask(ct => _impl.Move(ct, destinationFolder, desiredNewName, option));

		public IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
			=> AsyncAction.FromTask(ct => _impl.MoveAndReplace(ct, fileToReplace));

		public IAsyncAction DeleteAsync()
			=> AsyncAction.FromTask(ct => _impl.Delete(ct, StorageDeleteOption.Default));

		[NotImplemented] // The options is ignored, we implement this only to increase compatibility
		public IAsyncAction DeleteAsync(StorageDeleteOption option)
			=> AsyncAction.FromTask(ct => _impl.Delete(ct, option));
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

		private static string GetContentTypeFromFileType(string fileType)
			// https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types/Common_types
			=> fileType.ToLowerInvariant() switch
			{
				".aac" => "audio/aac",
				".abw" => "application/x-abiword",
				".arc" => "application/x-freearc",
				".avi" => "video/x-msvideo",
				".azw" => "application/vnd.amazon.ebook",
				".bin" => "application/octet-stream",
				".bmp" => "image/bmp",
				".bz" => "application/x-bzip",
				".bz2" => "application/x-bzip2",
				".csh" => "application/x-csh",
				".css" => "text/css",
				".csv" => "text/csv",
				".doc" => "application/msword",
				".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
				".eot" => "application/vnd.ms-fontobject",
				".epub" => "application/epub+zip",
				".gz" => "application/gzip",
				".gif" => "image/gif",
				".htm" => "text/html",
				".html" => "text/html",
				".ico" => "image/vnd.microsoft.icon",
				".ics" => "text/calendar",
				".jar" => "application/java-archive",
				".jpeg" => "image/jpeg",
				".jpg" => "image/jpeg",
				".js" => "text/javascript",
				".json" => "application/json",
				".jsonld" => "application/ld+json",
				".mid" => "audio/midi",
				".midi" => "audio/midi",
				".mjs" => "text/javascript",
				".mp3" => "audio/mpeg",
				".mpeg" => "video/mpeg",
				".mpkg" => "application/vnd.apple.installer+xml",
				".odp" => "application/vnd.oasis.opendocument.presentation",
				".ods" => "application/vnd.oasis.opendocument.spreadsheet",
				".odt" => "application/vnd.oasis.opendocument.text",
				".oga" => "audio/ogg",
				".ogv" => "video/ogg",
				".ogx" => "application/ogg",
				".opus" => "audio/opus",
				".otf" => "font/otf",
				".png" => "image/png",
				".pdf" => "application/pdf",
				".php" => "application/x-httpd-php",
				".ppt" => "application/vnd.ms-powerpoint",
				".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
				".rar" => "application/vnd.rar",
				".rtf" => "application/rtf",
				".sh" => "application/x-sh",
				".svg" => "image/svg+xml",
				".swf" => "application/x-shockwave-flash",
				".tar" => "application/x-tar",
				".tif" => "image/tiff",
				".tiff" => "image/tiff",
				".ts" => "video/mp2t",
				".ttf" => "font/ttf",
				".txt" => "text/plain",
				".vsd" => "application/vnd.visio",
				".wav" => "audio/wav",
				".weba" => "audio/webm",
				".webm" => "video/webm",
				".webp" => "image/webp",
				".woff" => "font/woff",
				".woff2" => "font/woff2",
				".xhtml" => "application/xhtml+xml",
				".xls" => "application/vnd.ms-excel",
				".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				".xml" => "application/xml",
				".xul" => "application/vnd.mozilla.xul+xml",
				".zip" => "application/zip",
				".3gp" => "video/3gpp",
				".3g2" => "video/3gpp2",
				".7z" => "application/x-7z-compressed",
				_ => "application/octet-stream",
			};
		#endregion
	}
}
