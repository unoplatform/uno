#nullable enable

using System;
using System.IO;
using System.Threading;
using Windows.ApplicationModel.Appointments;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Windows.Storage
{
	public sealed partial class StreamedFileDataRequest : IOutputStream, IDisposable, IStreamedFileDataRequest
	{
		private readonly StreamedRandomAccessStream _owner;
		private readonly Stream _tempFile;
		private readonly CancellationTokenSource _ct;

		internal StreamedFileDataRequest(StreamedRandomAccessStream owner, Stream tempFile)
		{
			_owner = owner;
			_tempFile = tempFile;
			_ct = new CancellationTokenSource();
		}

		internal CancellationToken CancellationToken => _ct.Token;

		internal void Abort() => _ct.Cancel();

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
			=> new AsyncOperationWithProgress<uint, uint>(async (ct, op) =>
			{
				var write = _tempFile.WriteAsync(buffer);
				write.Progress = (snd, p) => op.NotifyProgress(p);

				var written = await write.AsTask(ct);

				// We make sure to write the data to the disk before allow read to access it
				_tempFile.FlushAsync(ct);
				//_owner.OnDataLoadProgress(written);

				return written;
			});

		public IAsyncOperation<bool> FlushAsync()
			=> _tempFile.FlushAsyncOp(); // This is actually useless as we flush on each Write.

		public void FailAndClose(StreamedFileFailureMode failureMode)
		{
			//_owner.OnDataLoadCompleted(failureMode);
		}

		public void Dispose()
		{
			_ct.Dispose();
			//_owner.OnDataLoadCompleted(default);
		}
	}
}
