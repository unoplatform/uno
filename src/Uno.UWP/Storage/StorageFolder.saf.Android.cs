#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using AndroidX.DocumentFile.Provider;
using Uno.UI;

namespace Windows.Storage
{
	public partial class StorageFolder
	{
		internal static StorageFolder GetFromSafUri(Android.Net.Uri safUri) =>
			new StorageFolder(new SafFolder(safUri));

		internal static StorageFolder GetFromSafDocument(DocumentFile document) =>
				new StorageFolder(new SafFolder(document));

		internal class SafFolder : ImplementationBase
		{
			private static readonly StorageProvider _provider = new StorageProvider("Android.StorageAccessFramework", "Android Storage Access Framework");

			private readonly Android.Net.Uri _folderUri;
			private readonly DocumentFile _directoryDocument;

			internal SafFolder(Android.Net.Uri uri)
			{
				_folderUri = uri ?? throw new ArgumentNullException(nameof(uri));
				_directoryDocument = DocumentFile.FromTreeUri(Application.Context, uri);
			}

			internal SafFolder(DocumentFile directoryDocument)
			{
				_directoryDocument = directoryDocument ?? throw new ArgumentNullException(nameof(directoryDocument));
				_folderUri = _directoryDocument.Uri;
			}

			public override StorageProvider Provider => _provider;

			//TODO: Display name can be queried - https://developer.android.com/training/data-storage/shared/documents-files#examine-metadata
			public override string Name => _directoryDocument?.Name ?? string.Empty;

			public override Task<StorageFile> CreateFileAsync(string desiredName, Windows.Storage.CreationCollisionOption options, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public override Task<StorageFolder> CreateFolderAsync(string folderName, Windows.Storage.CreationCollisionOption option, CancellationToken token)
			{
				var directoryDocument = _directoryDocument.CreateDirectory(folderName);
				return Task.FromResult(new StorageFolder(new SafFolder(directoryDocument)));
			}

			public override Task DeleteAsync(CancellationToken ct)
			{
				_directoryDocument.Delete();
				return Task.CompletedTask;
			}

			public override Task<StorageFile> GetFileAsync(string name, CancellationToken token)
			{
				throw new NotImplementedException();
			}

			public override Task<IReadOnlyList<StorageFile>> GetFilesAsync(CancellationToken ct)
			{
				var contents = _directoryDocument
					.ListFiles()
					.Where(f => f.IsFile)
					.Select(d => StorageFile.GetFromSafDocument(d))
					.ToArray();
				return Task.FromResult((IReadOnlyList<StorageFile>)contents);
			}

			public override Task<StorageFolder> GetFolderAsync(string name, CancellationToken token)
			{
				var file = _directoryDocument.FindFile(name);
				if (file == null || !file.IsDirectory)
				{
					throw new FileNotFoundException("Not found");
				}

				return Task.FromResult(new StorageFolder(new SafFolder(file)));
			}

			public override Task<IReadOnlyList<StorageFolder>> GetFoldersAsync(CancellationToken ct)
			{
				var contents = _directoryDocument
					.ListFiles()
					.Where(f => f.IsDirectory)
					.Select(d => GetFromSafDocument(d))
					.ToArray();
				return Task.FromResult((IReadOnlyList<StorageFolder>)contents);
			}

			public override Task<IStorageItem> GetItemAsync(string name, CancellationToken token)
			{
				throw new NotImplementedException();
			}

			public override Task<IReadOnlyList<IStorageItem>> GetItemsAsync(CancellationToken ct)
			{
				var items = _directoryDocument.ListFiles();
				var folders = items.Where(i => i.IsDirectory).Select(i => GetFromSafDocument(i));
				var files = items.Where(i => i.IsFile).Select(i => StorageFile.GetFromSafDocument(i));
				return Task.FromResult((IReadOnlyList<IStorageItem>)folders.OfType<IStorageItem>().Union(files).ToArray());
			}

			public override Task<StorageFolder?> GetParentAsync(CancellationToken token)
			{
				if (_directoryDocument.ParentFile == null)
				{
					// TODO: Should we throw?
					return Task.FromResult<StorageFolder?>(null);
				}

				var parentFolder = new StorageFolder(new SafFolder(_directoryDocument.ParentFile));
				return Task.FromResult<StorageFolder?>(parentFolder);
			}

			public override Task<IStorageItem?> TryGetItemAsync(string name, CancellationToken token)
			{
				throw new NotImplementedException();
			}

			protected override bool IsEqual(ImplementationBase implementation)
			{
				throw new NotImplementedException();
			}
		}
	}
}
