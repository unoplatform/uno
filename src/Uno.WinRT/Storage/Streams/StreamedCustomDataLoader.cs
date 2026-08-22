#nullable enable

using System;
using System.Linq;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	internal class StreamedCustomDataLoader : IStreamedDataLoader
	{
		private uint _loaded;
		private bool _isCompleted;
		private StreamedFileFailureMode? _failure;

		public StreamedCustomDataLoader(StreamedFileDataRequestedHandler handler, TemporaryFile? tempFile = null)
		{
			File = tempFile ?? new TemporaryFile();

			handler(new StreamedFileDataRequest(this));
		}

		/// <inheritdoc />
		public event TypedEventHandler<IStreamedDataLoader, object?>? DataUpdated;

		/// <inheritdoc />
		public TemporaryFile File { get; }

		/// <inheritdoc />
		public string? ContentType { get; } = RandomAccessStreamWithContentType.DefaultContentType;

		/// <inheritdoc />
		public void CheckState()
		{
			if (_failure.HasValue)
			{
				throw new InvalidOperationException($"The async load of the data has failed ('{_failure.Value}')");
			}
		}

		/// <inheritdoc />
		public bool CanRead(ulong position)
			=> _isCompleted || position < _loaded;

		internal void ReportDataWritten(uint length)
		{
			_loaded += length;
			DataUpdated?.Invoke(this, default);
		}

		internal void ReportLoadCompleted(StreamedFileFailureMode? failure = null)
		{
			// Note: this is expected to be invoke more than once!

			_failure ??= failure;

			if (!_isCompleted)
			{
				_isCompleted = true;

				DataUpdated?.Invoke(this, default);
			}
		}
	}
}
