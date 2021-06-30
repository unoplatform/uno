using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;

namespace Uno.Storage.Streams.Internal
{
	internal class NativeWriteStreamAdapter : Stream
	{
		private const string JsType = "Uno.Storage.Streams.NativeFileWriteStream";

		private readonly Guid _streamId;

		private long _length = 0;
		private long _position = 0;

		private ConcurrentQueue<Func<Task>> _pendingTasks = new ConcurrentQueue<Func<Task>>();

		public static async Task<NativeWriteStreamAdapter> CreateAsync(Guid fileId)
		{
			var streamId = Guid.NewGuid();
			var result = await WebAssemblyRuntime.InvokeAsync($"{JsType}.openAsync('{streamId}', '{fileId}')");
			if (result == null || !long.TryParse(result, out var length))
			{
				throw new InvalidOperationException("Could not create a writable stream.");
			}

			return new NativeWriteStreamAdapter(streamId, length);
		}

		private NativeWriteStreamAdapter(Guid streamId, long length)
		{
			_streamId = streamId;
			_length = length;
		}

		public override bool CanRead => false;

		public override bool CanSeek => true;

		public override bool CanWrite => true;

		public override long Length => _length;

		public override long Position
		{
			get => _position;
			set => _position = value;
		}

		public override void Flush()
		{
			// No-op - flush is done when stream is fully closed.
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("This stream is write-only.");

		public override long Seek(long offset, SeekOrigin origin) =>
			origin switch
			{
				SeekOrigin.Begin => Position = offset,
				SeekOrigin.Current => Position += offset,
				SeekOrigin.End => Position = Length + offset,
				_ => throw new ArgumentException("Invalid SeekOrigin value.", nameof(origin)),
			};

		public override void SetLength(long value)
		{
			if (value < Length)
			{
				_pendingTasks.Enqueue(async () =>
				{
					await TruncateAsync(value);
				});
			}
			else
			{
				_pendingTasks.Enqueue(async () =>
				{
					await WriteAsync(new byte[] { 0 }, 0, 1);
				});
			}

			_length = value;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_pendingTasks.Enqueue(async () =>
			{
				await WriteAsync(buffer, offset, count);
			});
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			await ProcessPendingAsync();
			var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			try
			{
				var pinnedData = handle.AddrOfPinnedObject();
				await WebAssemblyRuntime.InvokeAsync($"{JsType}.writeAsync('{_streamId}', {pinnedData}, {offset}, {count}, {Position})");
				_length += Math.Max(Position + count, Length);
				Position += count;
			}
			finally
			{
				handle.Free();
			}
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException("This stream is write-only.");

		protected override async void Dispose(bool disposing)
		{
			await ProcessPendingAsync();
			// Close and dispose.
			await WebAssemblyRuntime.InvokeAsync($"{JsType}.closeAsync('{_streamId}')");
		}

		public async Task TruncateAsync(long length)
		{
			await ProcessPendingAsync();
			await WebAssemblyRuntime.InvokeAsync($"{JsType}.truncateAsync('{_streamId}',{length})");
		}

		private async Task ProcessPendingAsync()
		{
			var currentTasks = _pendingTasks.ToArray();
			foreach (var pendingTask in currentTasks)
			{
				await pendingTask();
			}
		}

		public async Task CloseAsync()
		{
			await ProcessPendingAsync();
			await WebAssemblyRuntime.InvokeAsync($"{JsType}.closeAsync('{_streamId}')");
		}
	}
}
