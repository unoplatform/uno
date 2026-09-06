#nullable enable

using System;
using System.IO;
using System.Linq;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	internal class StreamedOutputStream : IOutputStream
	{
		private readonly IStreamedDataUploader _uploader;
		private readonly Stream _stream;
		private readonly bool _hasOwnership;
		private (ulong start, ulong end)? _editedRange;

		/// <summary>
		/// Creates a new StreamedOutputStream.
		/// This will self-open the temporary file and will take ownership of the opened stream.
		/// </summary>
		public StreamedOutputStream(IStreamedDataUploader uploader, ulong position = 0)
		{
			_uploader = uploader;
			_stream = uploader.File.Open(FileAccess.Write);
			_hasOwnership = true;

			_stream.Seek((long)position, SeekOrigin.Begin);
		}

		/// <summary>
		/// Creates a new StreamedOutputStream using a **shared** file stream.
		/// This stream won't have ownership of the file and won't close/dispose it.
		/// </summary>
		public StreamedOutputStream(Stream stream, IStreamedDataUploader uploader)
		{
			_uploader = uploader;
			_stream = stream;
			_hasOwnership = false;
		}

		/// <inheritdoc />
		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
			=> new AsyncOperationWithProgress<uint, uint>(async (ct, op) =>
			{
				var from = _stream.Position;
				var to = from + buffer.Length;

				await _stream.WriteAsync(buffer, ct);

				UpdateEditedRange(from, to);

				return buffer.Length;
			});

		/// <inheritdoc />
		public IAsyncOperation<bool> FlushAsync()
			=> AsyncOperation.FromTask<bool>(async ct =>
			{
				if (!_editedRange.HasValue)
				{
					return true;
				}

				await _stream.FlushAsync(ct);
				await _uploader.Push(_editedRange.Value.start, _editedRange.Value.end, ct);
				_editedRange = null;

				return false;
			});

		private void UpdateEditedRange(long from, long to)
		{
			var (start, end) = _editedRange.GetValueOrDefault((ulong.MaxValue, ulong.MinValue));
			start = Math.Min(start, (ulong)from);
			end = Math.Max(end, (ulong)to);
			_editedRange = (start, end);
		}

		public void Dispose()
		{
			if (_hasOwnership)
			{
				_stream.Dispose();
			}
		}

		~StreamedOutputStream()
			=> Dispose();
	}
}
