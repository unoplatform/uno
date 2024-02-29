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

namespace Windows.Storage
{
	public partial class StorageFile
	{
		internal static StorageFile GetFromSecurityScopedUrl(NSUrl nsUrl, StorageFolder? parent) =>
			new StorageFile(new SecurityScopedFile(nsUrl, parent));

		internal static StorageFile GetFromItemProvider(NSItemProvider provider, StorageFolder? parent) =>
			new StorageFile(new MediaScopedFile(provider, parent));

		internal class MediaScopedFile : ImplementationBase
		{
			private readonly NSItemProvider? _provider;
			private readonly StorageFolder? _parent;
			private readonly string? _identifier;
			public MediaScopedFile(NSItemProvider provider, StorageFolder? parent) :
				base(string.Empty)
			{
				_provider = provider;
				_parent = parent;
				_identifier = GetIdentifier(provider?.RegisteredTypeIdentifiers ?? Array.Empty<string>());

				var extension = !string.IsNullOrEmpty(_identifier) ? GetExtension(_identifier) : string.Empty;
				var fileName = $"{provider?.SuggestedName}.{extension}";

				Path = SystemPath.Combine(path1: parent?.Path ?? string.Empty,
										  path2: fileName);
			}
			public override string ContentType => GetMIMEType(_identifier ?? string.Empty) ?? base.ContentType;

			public override StorageProvider Provider => StorageProviders.IosItemProvider;
			public override DateTimeOffset DateCreated => throw new NotImplementedException();
			public override Task DeleteAsync(CancellationToken ct, StorageDeleteOption options) =>
				throw new InvalidOperationException("Created date is not available from this source");

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct) =>
				throw new InvalidOperationException("File properties are not available from this source");

			public override Task<StorageFolder?> GetParentAsync(CancellationToken ct) => Task.FromResult(_parent);
			public override Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options) =>
				throw new InvalidOperationException("File properties are not available from this source");

			public override async Task<Stream> OpenStreamAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
			{
				if (_provider is null || _identifier is null)
				{
					throw new InvalidOperationException("Can't load data representation from item provider.");
				}

				var data = await _provider.LoadDataRepresentationAsync(_identifier);

				if (data is null)
				{
					throw new InvalidOperationException("Can't load data representation from item provider.");
				}

				return data.AsStream();
			}

			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option) => throw new NotImplementedException();
			protected override bool IsEqual(ImplementationBase implementation) => throw new NotImplementedException();

			private string? GetExtension(string identifier)
				=> UTType.CopyAllTags(identifier, UTType.TagClassFilenameExtension)?.FirstOrDefault();

			private string? GetMIMEType(string identifier)
				=> UTType.CopyAllTags(identifier, UTType.TagClassMIMEType)?.FirstOrDefault();

			private string? GetIdentifier(string[] identifiers)
			{
				if (!(identifiers?.Length > 0))
				{
					return null;
				}

				if (identifiers.Any(i => i.StartsWith(UTType.LivePhoto, StringComparison.InvariantCultureIgnoreCase)) && identifiers.Contains(UTType.JPEG))
				{
					return identifiers.FirstOrDefault(i => i == UTType.JPEG);
				}

				if (identifiers.Contains(UTType.QuickTimeMovie))
				{
					return identifiers.FirstOrDefault(i => i == UTType.QuickTimeMovie);
				}

				return identifiers.FirstOrDefault();
			}
		}
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
	}
}
