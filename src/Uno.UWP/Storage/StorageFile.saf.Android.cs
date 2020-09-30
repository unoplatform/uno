#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using AndroidX.DocumentFile.Provider;
using Uno.Storage.Internal;
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
			private readonly Android.Net.Uri _fileUri;
			private readonly DocumentFile _directoryDocument;

			internal SafFile(Android.Net.Uri uri) : base(uri.Path ?? string.Empty)
			{
				_fileUri = uri ?? throw new ArgumentNullException(nameof(uri));
				_directoryDocument = DocumentFile.FromSingleUri(Application.Context, uri);
				if (string.IsNullOrEmpty(Path))
				{
					Path = _directoryDocument.Name;
				}
			}

			internal SafFile(DocumentFile directoryDocument) : base(directoryDocument.Uri.Path ?? directoryDocument.Name)
			{
				_directoryDocument = directoryDocument ?? throw new ArgumentNullException(nameof(directoryDocument));
				_fileUri = _directoryDocument.Uri;
			}

			public override StorageProvider Provider => StorageProviders.AndroidSaf;

			public override string Name => _directoryDocument?.Name ?? string.Empty;

			public override DateTimeOffset DateCreated => throw new NotImplementedException();

			public override async Task DeleteAsync(CancellationToken ct, StorageDeleteOption options)
			{
				await Task.Run(() =>
				{
					_directoryDocument.Delete();
				}, ct);
			}

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct) =>
				SafHelpers.GetBasicPropertiesAsync(_fileUri, _directoryDocument, true, ct);

			public override Task<StorageFolder?> GetParentAsync(CancellationToken ct)
			{
				if (_directoryDocument.ParentFile == null)
				{
					return Task.FromResult<StorageFolder?>(null);
				}

				var parentFolder = StorageFolder.GetFromSafDocument(_directoryDocument.ParentFile);
				return Task.FromResult<StorageFolder?>(parentFolder);
			}

			public override async Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options) =>
				new RandomAccessStreamWithContentType(await FileRandomAccessStream.CreateFromSafUriAsync(_fileUri, accessMode), ContentType);

			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option) => throw new NotImplementedException();

			protected override bool IsEqual(ImplementationBase implementation)
			{
				if (implementation is SafFile otherFile)
				{
					var path = _directoryDocument.Uri?.ToString() ?? string.Empty;
					var otherPath = otherFile._directoryDocument.Uri?.ToString() ?? string.Empty;
					return path.Equals(otherPath, StringComparison.InvariantCulture);
				}

				return false;
			}
		}
	}
}
