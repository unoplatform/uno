#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using AndroidX.DocumentFile.Provider;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Windows.Storage
{
    public partial class StorageFile
    {
		public static StorageFile GetFromSafDocument(DocumentFile document) =>
			new StorageFile(new SafFile(document));

		public static StorageFile GetFromSafUri(Android.Net.Uri uri) =>
			new StorageFile(new SafFile(uri));

		internal class SafFile : ImplementationBase
		{
			private static readonly StorageProvider _provider = new StorageProvider("Android.StorageAccessFramework", "Android Storage Access Framework");

			private readonly Android.Net.Uri _folderUri;
			private readonly DocumentFile _directoryDocument;

			internal SafFile(Android.Net.Uri uri)
			{
				_folderUri = uri ?? throw new ArgumentNullException(nameof(uri));
				_directoryDocument = DocumentFile.FromTreeUri(Application.Context, uri);
			}

			internal SafFile(DocumentFile directoryDocument)
			{
				_directoryDocument = directoryDocument ?? throw new ArgumentNullException(nameof(directoryDocument));
				_folderUri = _directoryDocument.Uri;
			}

			public override StorageProvider Provider => _provider;

			//TODO: Display name can be queried - https://developer.android.com/training/data-storage/shared/documents-files#examine-metadata
			public override string Name => _directoryDocument.Name;

			public override DateTimeOffset DateCreated => throw new NotImplementedException();

			public override Task DeleteAsync(CancellationToken ct, StorageDeleteOption options) => throw new NotImplementedException();
			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct) => throw new NotImplementedException();
			public override Task<StorageFolder?> GetParentAsync(CancellationToken ct) => throw new NotImplementedException();
			public override Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options) => throw new NotImplementedException();
			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option) => throw new NotImplementedException();
			protected override bool IsEqual(ImplementationBase implementation) => throw new NotImplementedException();
		}
	}
}
