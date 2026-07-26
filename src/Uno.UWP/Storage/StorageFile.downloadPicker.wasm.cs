#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Storage.Internal;
using Uno.Storage.Streams.Internal;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Windows.Storage
{
	public partial class StorageFile
	{
		internal static StorageFile GetForDownloadPicker(string fileName)
			=> new StorageFile(new DownloadPickerStorageFile(fileName));

		/// <summary>
		/// The destination file handed out by the download-based save picker fallback.
		/// Content is staged in a JS-side chunked buffer (not in the in-memory filesystem
		/// nor the WASM heap) and turned into a Blob download on CompleteUpdatesAsync.
		/// </summary>
		internal sealed class DownloadPickerStorageFile : ImplementationBase
		{
			private readonly Guid _bufferId;
			private readonly DateTimeOffset _dateCreated;

			public DownloadPickerStorageFile(string fileName)
				: base(fileName)
			{
				_bufferId = Guid.NewGuid();
				_dateCreated = DateTimeOffset.UtcNow;
				ChunkedBufferStream.CreateBuffer(_bufferId);
			}

			public override StorageProvider Provider => StorageProviders.WasmDownloadPicker;

			public override DateTimeOffset DateCreated => _dateCreated;

			protected override bool IsEqual(ImplementationBase impl)
				=> impl is DownloadPickerStorageFile other && other._bufferId == _bufferId;

			public override Task<StorageFolder?> GetParentAsync(CancellationToken ct)
				=> Task.FromResult<StorageFolder?>(null);

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct)
			{
				using var stream = ChunkedBufferStream.CreateView(_bufferId, writable: false);
				return Task.FromResult(new BasicProperties((ulong)stream.Length, DateTimeOffset.UtcNow));
			}

			public override Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> Task.FromResult<IRandomAccessStreamWithContentType>(
					new RandomAccessStreamWithContentType(
						ChunkedBufferStream.CreateView(_bufferId, writable: accessMode == FileAccessMode.ReadWrite), ContentType));

			public override Task<Stream> OpenStreamAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> Task.FromResult<Stream>(ChunkedBufferStream.CreateView(_bufferId, writable: accessMode == FileAccessMode.ReadWrite));

			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option)
				=> throw NotSupported();

			public override Task DeleteAsync(CancellationToken ct, StorageDeleteOption options)
			{
				ChunkedBufferStream.DisposeBuffer(_bufferId);
				return Task.CompletedTask;
			}

			/// <summary>Triggers the browser download of the staged content.</summary>
			public Task TriggerDownloadAsync()
				=> ChunkedBufferStream.SaveBufferAsDownloadAsync(_bufferId, Name);
		}
	}
}
