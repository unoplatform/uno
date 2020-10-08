#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	internal class StreamedInputStream : IInputStream
	{
		private readonly IStreamedDataLoader _loader;
		private readonly Stream _stream;
		private readonly bool _hasStreamOwnership;

		private ulong? _initialPosition;

		public StreamedInputStream(IStreamedDataLoader loader, ulong position)
		{
			_loader = loader;
			_stream = loader.File.Open(FileAccess.Read);
			_hasStreamOwnership = true;
			_initialPosition = position;
		}

		public StreamedInputStream(Stream stream, IStreamedDataLoader loader)
		{
			_loader = loader;
			_stream = stream;
			_hasStreamOwnership = false;
		}

		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
			=> new AsyncOperationWithProgress<IBuffer, uint>(async (ct, op) =>
			{
				var endPosition = (ulong)_stream.Position + (_initialPosition ?? 0) + count;
				if (!_loader.CanRead(endPosition))
				{
					// The data is not ready yet.
					// We have to wait for the data to be written into the temporary file before reading the value.

					var asyncLoad = new TaskCompletionSource<object?>();
					try
					{
						_loader.DataUpdated += OnDataLoaded;
						if (!_loader.CanRead(endPosition))
						{
							using var ctReg = ct.Register(() => asyncLoad.TrySetCanceled(ct));
							await asyncLoad.Task;
						}
					}
					finally
					{
						_loader.DataUpdated -= OnDataLoaded;
					}

					void OnDataLoaded(IStreamedDataLoader loader, object? _)
					{
						if (_loader.CanRead(endPosition))
						{
							asyncLoad.TrySetResult(_);
						}
					}
				}

				if (_initialPosition.HasValue)
				{
					_stream.Seek((long)_initialPosition.Value, SeekOrigin.Begin);
					_initialPosition = null;
				}

				var read = _stream.ReadAsyncOperation(buffer, count, options);
				read.Progress = (_, progress) => op.NotifyProgress(progress);

				return await read.AsTask(ct);
			});

		public void Dispose()
		{
			// Note: We DO NOT dispose the _loader as it might been used by some other stream (it's designed to be self-disposable)

			if (_hasStreamOwnership)
			{
				_stream.Dispose();
			}
		}

		~StreamedInputStream()
			=> Dispose();
	}
}
