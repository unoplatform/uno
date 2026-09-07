#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Windows.Storage
{
	public sealed partial class StreamedFileDataRequest : IOutputStream, IDisposable, IStreamedFileDataRequest
	{
		private readonly StreamedCustomDataLoader _owner;
		private readonly Stream _tempFile;
		private readonly CancellationTokenSource _ct;

		internal StreamedFileDataRequest(StreamedCustomDataLoader owner)
		{
			_owner = owner;
			_tempFile = owner.File.OpenWeak(FileAccess.Write);
			_ct = new CancellationTokenSource();
		}

		internal CancellationToken CancellationToken => _ct.Token;

		internal void Abort() => _ct.Cancel();

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
			=> new AsyncOperationWithProgress<uint, uint>(async (ct, op) =>
			{
				try
				{
					await _tempFile.WriteAsync(buffer, ct);
					await _tempFile.FlushAsync(ct); // We make sure to write the data to the disk before allow read to access it
				}
				catch (IOException e)
				{
					throw new OperationCanceledException("The download of this file has been cancelled", e);
				}

				_owner.ReportDataWritten(buffer.Capacity);

				return buffer.Capacity;
			});

		public IAsyncOperation<bool> FlushAsync()
			=> _tempFile.FlushAsyncOperation(); // This is actually useless as we flush on each Write.

		public void FailAndClose(StreamedFileFailureMode failureMode)
		{
			_ct.Dispose();
			_tempFile.Dispose();
			_owner.ReportLoadCompleted(failureMode);
		}

		public void Dispose()
		{
			_ct.Dispose();
			_tempFile.Dispose();
			_owner.ReportLoadCompleted();
		}

		~StreamedFileDataRequest()
			=> Dispose();
	}
}
