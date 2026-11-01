#nullable enable
using System;
using System.IO;
using Windows.Foundation;

namespace Windows.Storage.Streams;

public partial class InMemoryRandomAccessStream : IStreamWrapper
{
	private readonly MemoryStream _stream;
	public InMemoryRandomAccessStream() =>
		_stream = new();

	private InMemoryRandomAccessStream(MemoryStream stream) =>
		_stream = stream;

	public ulong Size
	{
		get => (ulong)_stream.Length;
		set => _stream.SetLength((long)value);
	}

	public bool CanRead => _stream.CanRead;

	public bool CanWrite => _stream.CanWrite;

	public ulong Position => (ulong)_stream.Position;

	public void Seek(ulong position) => _stream.Position = (long)position;

	public IRandomAccessStream CloneStream()
	{
		// As on WinUI, a disposed stream can't be cloned, and the clone starts at 0. Copying the buffer leaves this stream's position alone.
		ObjectDisposedException.ThrowIf(!_stream.CanRead, this);
		var content = _stream.TryGetBuffer(out var buffer) ? buffer : new ArraySegment<byte>(_stream.ToArray());
		var destination = new MemoryStream(content.Count);
		destination.Write(content.Array!, content.Offset, content.Count);
		destination.Position = 0;
		return new InMemoryRandomAccessStream(destination);
	}

	public void Dispose() => _stream.Dispose();

	Stream IStreamWrapper.FindStream() => _stream;

	public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) =>
		_stream.ReadAsyncOperation(buffer, count, options);

	public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) =>
		_stream.WriteAsyncOperation(buffer);

	public IAsyncOperation<bool> FlushAsync() =>
		_stream.FlushAsyncOperation();

	public IInputStream GetInputStreamAt(ulong position)
	{
		if (!CanRead)
		{
			throw new NotSupportedException("It has not been opened for read.");
		}

		_stream.Seek((long)position, SeekOrigin.Begin);

		return new InputStreamOverStream(_stream);
	}

	public IOutputStream GetOutputStreamAt(ulong position)
	{
		if (!CanWrite)
		{
			throw new NotSupportedException("It has not been opened for write.");
		}

		_stream.Seek((long)position, SeekOrigin.Begin);

		return new OutputStreamOverStream(_stream);
	}
}
