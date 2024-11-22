#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Photos;
using PhotosUI;
using UIKit;
using Uno.Storage.Internal;
using Uno.Storage.Streams.Internal;
using Windows.Extensions;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Windows.Storage
{
	public partial class StorageFile
	{
		internal static StorageFile GetFromSecurityScopedUrl(NSUrl nsUrl, StorageFolder? parent) =>
			new StorageFile(new SecurityScopedFile(nsUrl, parent));

		internal static StorageFile GetFromPHPickerResult(PHPickerResult result, PHAsset phAsset, StorageFolder? parent) =>
			new StorageFile(new PHPickerResultFile(result, phAsset, parent));

		internal class SecurityScopedFile : ImplementationBase
		{
			private readonly NSUrl _nsUrl;
			private readonly StorageFolder? _parent;
			private readonly UIDocument _document;
			private DateTimeOffset? _dateCreated;

			public SecurityScopedFile(NSUrl nsUrl, StorageFolder? parent) : base(string.Empty)
			{
				if (nsUrl is null)
				{
					throw new ArgumentNullException(nameof(nsUrl));
				}

				_nsUrl = nsUrl;
				_parent = parent;
				_document = new UIDocument(_nsUrl);
				Path = _document.FileUrl?.Path ?? string.Empty;
			}

			public override StorageProvider Provider => StorageProviders.IosSecurityScoped;

			public override DateTimeOffset DateCreated => _dateCreated ?? (_dateCreated = GetDateCreated()).Value;

			public override async Task DeleteAsync(CancellationToken ct, StorageDeleteOption options)
			{
				var intent = NSFileAccessIntent.CreateWritingIntent(_nsUrl, NSFileCoordinatorWritingOptions.ForDeleting);

				using var coordinator = new NSFileCoordinator();
				await coordinator.CoordinateAsync(new[] { intent }, new NSOperationQueue(), () =>
				{
					using var _ = _nsUrl.BeginSecurityScopedAccess();
					NSError deleteError;

					NSFileManager.DefaultManager.Remove(_nsUrl, out deleteError);

					if (deleteError != null)
					{
						throw new UnauthorizedAccessException($"Can't delete file. {deleteError}");
					}
				});
			}

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct)
			{
				using var _ = _nsUrl.BeginSecurityScopedAccess();
				var fileInfo = new FileInfo(Path);
				return Task.FromResult(new BasicProperties((ulong)fileInfo.Length, fileInfo.LastWriteTimeUtc));
			}

			private DateTimeOffset GetDateCreated()
			{
				using var _ = _nsUrl.BeginSecurityScopedAccess();
				return new FileInfo(Path).CreationTimeUtc;
			}

			public override Task<StorageFolder?> GetParentAsync(CancellationToken ct) => Task.FromResult(_parent);

			public override Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> Task.FromResult<IRandomAccessStreamWithContentType>(new RandomAccessStreamWithContentType(FileRandomAccessStream.CreateSecurityScoped(_nsUrl, ToFileAccess(accessMode), ToFileShare(options)), ContentType));

			public override Task<Stream> OpenStreamAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
			{
				Func<Stream> streamBuilder = () => File.Open(Path, FileMode.Open, ToFileAccess(accessMode), ToFileShare(options));
				var streamWrapper = new SecurityScopeStreamWrapper(_nsUrl, streamBuilder);
				return Task.FromResult<Stream>(streamWrapper);
			}

			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option) => throw new NotImplementedException();

			protected override bool IsEqual(ImplementationBase implementation) =>
				implementation is SecurityScopedFile file &&
				file._nsUrl.FilePathUrl?.Path == _nsUrl.FilePathUrl?.Path;
		}

		internal class PHAssetFile : ImplementationBase
		{
			private readonly PHAsset _phAsset;
			private StorageFolder? _parent;

			public PHAssetFile(PHAsset phAsset, StorageFolder? parent) : base(string.Empty)
			{
				_phAsset = phAsset;
				_parent = parent;
				Path = phAsset.
			}

			public override StorageProvider Provider => StorageProviders.IosPHPicker;

			public override DateTimeOffset DateCreated => _phAsset.CreationDate.ToDateTimeOffset();

			public override Task DeleteAsync(CancellationToken ct, StorageDeleteOption options)
			{
				TaskCompletionSource resultCompletionSource = new TaskCompletionSource();
				PHPhotoLibrary.SharedPhotoLibrary.PerformChanges(
					() =>
					{
						PHAssetChangeRequest.DeleteAssets(new PHAsset[] { _phAsset });
					},
					(success, error) =>
					{
						if (success)
						{
							resultCompletionSource.SetResult();
						}
						else
						{
							resultCompletionSource.SetException(new Exception(error.LocalizedDescription));
						}
					}
				);

				return resultCompletionSource.Task;
			}

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct)
			{
				var resources = PHAssetResource.GetAssetResources(_phAsset);
				var imageSizeBytes = (resources.FirstOrDefault()?.ValueForKey(new NSString("fileSize")) as NSNumber) ?? 0;
				var dateModified = _phAsset.ModificationDate.ToDateTimeOffset();
				return Task.FromResult(new BasicProperties((ulong)imageSizeBytes, dateModified));
			}

			public override Task<StorageFolder?> GetParentAsync(CancellationToken ct) => Task.FromResult(_parent);

			public override Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> Task.FromResult<IRandomAccessStreamWithContentType>(new RandomAccessStreamWithContentType(FileRandomAccessStream.CreateSecurityScoped(_phAsset.Rea, ToFileAccess(accessMode), ToFileShare(options)), ContentType));
			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option)
			{

			}

			protected override bool IsEqual(ImplementationBase implementation) => implementation is PHAssetFile file && file._phAsset.LocalIdentifier == _phAsset.LocalIdentifier;
		}
	}
}
