#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Uno.Storage.Internal;
using Uno.Storage.Streams.Internal;
using System.IO;
using System.Linq;
using MobileCoreServices;
using SystemPath = System.IO.Path;
using PhotosUI;

namespace Windows.Storage
{
	public partial class StorageFile
	{
		internal static StorageFile GetFromSecurityScopedUrl(NSUrl nsUrl, StorageFolder? parent) =>
			new StorageFile(new SecurityScopedFile(nsUrl, parent));

		internal static StorageFile GetFromPHPickerResult(PHPickerResult result, StorageFolder? parent) =>
			new StorageFile(new PHPickerResultFile(result, parent));

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

		internal class PHPickerResultFile : ImplementationBase
		{
			private PHPickerResult _phPickerResult;
			private StorageFolder? _parent;

			public PHPickerResultFile(PHPickerResult phPickerResult, StorageFolder? parent) : base(string.Empty)
			{
				if (phPickerResult is null)
				{
					throw new ArgumentNullException(nameof(phPickerResult));
				}

				_phPickerResult = phPickerResult;
				_parent = parent;
			}

			public override StorageProvider Provider => StorageProviders.IosPHPicker;

			public override DateTimeOffset DateCreated
			{
				get
				{
					var itemProvider = _phPickerResult.ItemProvider;

					if (itemProvider.HasItemConformingTo(UTType.Image))
					{
						var fileUrl = await GetFileUrlAsync(itemProvider);

						if (fileUrl != null)
						{
							var attributes = NSFileManager.DefaultManager.GetAttributes(fileUrl.Path, out NSError error);
							if (error != null)
							{
								throw new IOException($"Error retrieving file attributes: {error.LocalizedDescription}");
							}

							var creationDate = attributes.CreationDate;
							return creationDate?.ToDateTimeOffset()
								   ?? throw new InvalidOperationException("Creation date not found.");
						}
					}

					throw new InvalidOperationException("Item does not conform to a supported type.");
				}
			}

			public override Task DeleteAsync(CancellationToken ct, StorageDeleteOption options) => throw new NotImplementedException();
			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct) => throw new NotImplementedException();
			public override Task<StorageFolder?> GetParentAsync(CancellationToken ct) => throw new NotImplementedException();
			public override Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options) => throw new NotImplementedException();
			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option) => throw new NotImplementedException();
			protected override bool IsEqual(ImplementationBase implementation)
			{
				implementation is PHPickerResultFile file && file._phPickerResult.ItemProvider.Load
			}
		}
	}
}
