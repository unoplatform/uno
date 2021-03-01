using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;

namespace Windows.Storage
{
	public partial class StorageFolder
	{
		internal static StorageFolder GetFolderFromSecurityScopedUrl(NSUrl nsUrl) =>
			new StorageFolder(new SecurityScopedFolder(nsUrl));

		internal class SecurityScopedFolder : ImplementationBase
		{
			private readonly NSUrl _nsUrl;
			private readonly UIDocument _document;

			public SecurityScopedFolder(NSUrl nsUrl)
			{
				if (nsUrl is null)
				{
					throw new ArgumentNullException(nameof(nsUrl));
				}

				_nsUrl = nsUrl;
				_document = new UIDocument(_nsUrl);
				Path = _document.FileUrl?.Path ?? string.Empty;
			}

			public override string Name => _document.FileUrl?.LastPathComponent ?? string.Empty;

			public override string DisplayName => _document.LocalizedName ?? Name;

			public override StorageProvider Provider => new StorageProvider("iOSSecurityScopedUrl", "iOS Security Scoped URL");

			public override Task<StorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options, CancellationToken cancellationToken) => throw new global::System.NotImplementedException();

			public override Task<StorageFolder> CreateFolderAsync(string folderName, CreationCollisionOption option, CancellationToken token) => throw new global::System.NotImplementedException();

			public override Task DeleteAsync(CancellationToken ct) => throw new global::System.NotImplementedException();

			public override Task<StorageFile> GetFileAsync(string name, CancellationToken token) => throw new global::System.NotImplementedException();

			public override Task<IReadOnlyList<StorageFile>> GetFilesAsync(CancellationToken ct) => throw new global::System.NotImplementedException();

			public override Task<StorageFolder> GetFolderAsync(string name, CancellationToken token) => throw new global::System.NotImplementedException();

			public override Task<IReadOnlyList<StorageFolder>> GetFoldersAsync(CancellationToken ct) => throw new global::System.NotImplementedException();

			public override Task<IStorageItem> GetItemAsync(string name, CancellationToken token) => throw new global::System.NotImplementedException();

			public override Task<IReadOnlyList<IStorageItem>> GetItemsAsync(CancellationToken ct) => throw new global::System.NotImplementedException();

			public override Task<StorageFolder> GetParentAsync(CancellationToken token) => throw new global::System.NotImplementedException();

			public override Task<IStorageItem> TryGetItemAsync(string name, CancellationToken token) => throw new global::System.NotImplementedException();

			protected override bool IsEqual(ImplementationBase implementation) => throw new global::System.NotImplementedException();
		}
	}
}
