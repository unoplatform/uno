using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;

namespace Uno.Storage.Streams
{
	internal class NativeWriteStreamAdapter : Stream, INativeStreamAdapter
	{
		private const string JsType = "Uno.Storage.Streams.NativeFileWriteStream";

		private readonly Guid _streamId;

		private int _rentCount = 0;

		public static async Task<NativeWriteStreamAdapter> CreateAsync(Guid fileId)
		{
			var streamId = Guid.NewGuid();
			var result = await WebAssemblyRuntime.InvokeAsync($"{JsType}.createAsync({fileId}, {streamId})");
			if (result == null)
			{
				throw new InvalidOperationException("Could not create a writable stream.");
			}

			return new NativeWriteStreamAdapter(streamId);
		}

		private NativeWriteStreamAdapter(Guid streamId)
		{
			_streamId = streamId;
		}

		public override bool CanRead => false;

		public override bool CanSeek => true;

		public override bool CanWrite => true;

		public override long Length => throw new global::System.NotImplementedException();

		public override long Position { get => throw new global::System.NotImplementedException(); set => throw new global::System.NotImplementedException(); }

		public override void Flush() => throw new global::System.NotImplementedException();

		public override int Read(byte[] buffer, int offset, int count) => throw new global::System.NotImplementedException();

		public override long Seek(long offset, SeekOrigin origin) => throw new global::System.NotImplementedException();

		public override void SetLength(long value) => throw new global::System.NotImplementedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new global::System.NotImplementedException();

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => base.WriteAsync(buffer, offset, count, cancellationToken);

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => base.ReadAsync(buffer, offset, count, cancellationToken);

		protected override void Dispose(bool disposing)
		{
			if (Interlocked.Decrement(ref _rentCount) == 0)
			{
				// Close and dispose.
			}
		}

		public void Rent() => Interlocked.Increment(ref _rentCount);
	}
}
