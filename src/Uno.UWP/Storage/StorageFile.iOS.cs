using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Uno.Storage.Internal;

namespace Windows.Storage
{
    public partial class StorageFile
    {
		internal static StorageFile GetFromSecurityScopedUrl(NSUrl nsUrl) =>
			new StorageFile(new SecurityScopedFile(nsUrl));

		internal class SecurityScopedFile : ImplementationBase
		{
			private readonly NSUrl _nsUrl;
			private readonly UIDocument _document;

			public SecurityScopedFile(NSUrl nsUrl)
			{
				if (nsUrl is null)
				{
					throw new ArgumentNullException(nameof(nsUrl));
				}

				_nsUrl = nsUrl;
				_document = new UIDocument(_nsUrl);
				Path = _document.FileUrl?.Path ?? string.Empty;
			}

			public override StorageProvider Provider => new StorageProvider("iOSSecurityScopedUrl", "iOS Security Scoped URL");

			public override DateTimeOffset DateCreated => throw new NotImplementedException();

			public override async Task DeleteAsync(CancellationToken ct, StorageDeleteOption options)
			{
				var intent = NSFileAccessIntent.CreateWritingIntent(_nsUrl, NSFileCoordinatorWritingOptions.ForDeleting);

				using var coordinator = new NSFileCoordinator();
				await coordinator.CoordinateAsync(new[] { intent }, new NSOperationQueue(), () =>
				{
					using (_nsUrl.BeginSecurityScopedAccess())
					{
						NSError deleteError;
						if (options == StorageDeleteOption.Default)
						{
							NSFileManager.DefaultManager.TrashItem(_nsUrl, out var _, out deleteError);
						}
						else
						{
							NSFileManager.DefaultManager.Remove(_nsUrl, out deleteError);
						}

						if (deleteError != null)
						{
							throw new UnauthorizedAccessException($"Can't delete file. {deleteError}");
						}
					}
				});
			}

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct) => throw new NotImplementedException();
			public override Task<StorageFolder> GetParentAsync(CancellationToken ct) => throw new NotImplementedException();
			public override Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options) => throw new NotImplementedException();
			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option) => throw new NotImplementedException();
			protected override bool IsEqual(ImplementationBase implementation) => throw new NotImplementedException();
		}
	}
}
