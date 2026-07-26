#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Storage.Streams.Internal;

/// <summary>
/// A seekable read/write stream over a JS-side chunked byte buffer.
/// The payload lives outside the 32-bit WASM linear memory, in small
/// non-contiguous chunks, so it is not subject to the browser's per-ArrayBuffer
/// contiguous allocation ceiling nor to reallocate-and-copy growth spikes.
/// </summary>
internal sealed partial class ChunkedBufferStream : Stream
{
	private readonly string _bufferId;
	private readonly bool _ownsBuffer;
	private readonly bool _canWrite;
	private long _position;
	private bool _disposed;

	private ChunkedBufferStream(Guid bufferId, bool ownsBuffer, bool canWrite)
	{
		_bufferId = bufferId.ToString();
		_ownsBuffer = ownsBuffer;
		_canWrite = canWrite;
	}

	/// <summary>Creates a new JS-side buffer owned (and disposed) by this stream.</summary>
	public static ChunkedBufferStream Create()
	{
		var bufferId = Guid.NewGuid();
		CreateBuffer(bufferId);
		return new ChunkedBufferStream(bufferId, ownsBuffer: true, canWrite: true);
	}

	/// <summary>
	/// Creates a view over an existing JS-side buffer whose lifetime is managed by the caller.
	/// Ownership and writability are independent: views never dispose the buffer, and
	/// <paramref name="writable"/> reflects the access mode the view was opened with.
	/// </summary>
	public static ChunkedBufferStream CreateView(Guid bufferId, bool writable)
		=> new ChunkedBufferStream(bufferId, ownsBuffer: false, canWrite: writable);

	/// <summary>Allocates a JS-side buffer whose lifetime is managed by the caller.</summary>
	public static void CreateBuffer(Guid bufferId)
		=> NativeMethods.Create(bufferId.ToString());

	/// <summary>Releases a caller-managed JS-side buffer.</summary>
	public static void DisposeBuffer(Guid bufferId)
		=> NativeMethods.DisposeBuffer(bufferId.ToString());

	/// <summary>Triggers a browser download of the buffer content.</summary>
	public static Task SaveBufferAsDownloadAsync(Guid bufferId, string fileName)
		=> NativeMethods.SaveAsDownloadAsync(bufferId.ToString(), fileName);

	public override bool CanRead => true;

	public override bool CanSeek => true;

	public override bool CanWrite => _canWrite;

	public override long Length
	{
		get
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
			return (long)NativeMethods.GetLength(_bufferId);
		}
	}

	public override long Position
	{
		get => _position;
		set => _position = value;
	}

	public override void Flush()
	{
		// No-op - the buffer is memory-backed.
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		ValidateArguments(buffer, offset, count);

		var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		try
		{
			var read = NativeMethods.Read(_bufferId, handle.AddrOfPinnedObject() + offset, count, _position);
			_position += read;
			return read;
		}
		finally
		{
			handle.Free();
		}
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		=> Task.FromResult(Read(buffer, offset, count));

	public override void Write(byte[] buffer, int offset, int count)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (!_canWrite)
		{
			throw new NotSupportedException("This stream was opened for read access only.");
		}

		ValidateArguments(buffer, offset, count);

		var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		try
		{
			NativeMethods.Write(_bufferId, handle.AddrOfPinnedObject() + offset, count, _position);
			_position += count;
		}
		finally
		{
			handle.Free();
		}
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Write(buffer, offset, count);
		return Task.CompletedTask;
	}

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
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (!_canWrite)
		{
			throw new NotSupportedException("This stream was opened for read access only.");
		}

		ArgumentOutOfRangeException.ThrowIfNegative(value);
		NativeMethods.Truncate(_bufferId, value);
	}

	protected override void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			_disposed = true;
			if (_ownsBuffer)
			{
				NativeMethods.DisposeBuffer(_bufferId);
			}
		}
	}

	private static void ValidateArguments(byte[] buffer, int offset, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer);
		ArgumentOutOfRangeException.ThrowIfNegative(offset);
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		if (buffer.Length - offset < count)
		{
			throw new ArgumentException("The buffer is too small for the requested operation.");
		}
	}

	internal static partial class NativeMethods
	{
		private const string JsType = "globalThis.Uno.Storage.Streams.NativeChunkedBuffer";

		[JSImport($"{JsType}.create")]
		internal static partial void Create(string bufferId);

		[JSImport($"{JsType}.dispose")]
		internal static partial void DisposeBuffer(string bufferId);

		[JSImport($"{JsType}.getLength")]
		internal static partial double GetLength(string bufferId);

		[JSImport($"{JsType}.write")]
		internal static partial void Write(string bufferId, nint data, int count, double position);

		[JSImport($"{JsType}.read")]
		internal static partial int Read(string bufferId, nint data, int count, double position);

		[JSImport($"{JsType}.truncate")]
		internal static partial void Truncate(string bufferId, double length);

		[JSImport($"{JsType}.saveAsDownloadAsync")]
		internal static partial Task SaveAsDownloadAsync(string bufferId, string fileName);
	}
}
