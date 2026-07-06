
#if NET10_0_OR_GREATER
using System;
using System.IO;
using System.Runtime.InteropServices.Marshalling;

using Windows.Storage.Streams;

using DirectN;

namespace Uno.UI.Runtime.Skia.Win32;

internal static class AotStreamHelpers
{
	internal static unsafe IRandomAccessStream ConvertIStream(IStream stream)
	{
		var ms = new MemoryStream();
		var buffer = new byte[4096];
		fixed (byte* pBuffer = buffer)
		{
			while (true)
			{
				uint bytesRead = 0;
				stream.Read((nint)pBuffer, (uint)buffer.Length, (nint)(&bytesRead)).ThrowOnError();
				if (bytesRead == 0) break;
				ms.Write(buffer, 0, (int)bytesRead);
			}
		}
		var ras = new InMemoryRandomAccessStream();
		var dataWriter = new DataWriter(ras);
		dataWriter.WriteBytes(ms.ToArray());
		dataWriter.StoreAsync().AsTask().Wait();
		ras.Seek(0);
		return ras;
	}

	internal static byte[] ReadIRandomAccessStream(IRandomAccessStream? stream)
	{
		if (stream is null) return Array.Empty<byte>();
		using var ms = new MemoryStream();
		stream.AsStreamForRead().CopyTo(ms);
		return ms.ToArray();
	}
}


[GeneratedComClass]
internal sealed partial class ByteArrayIStream : IStream, ISequentialStream
{
	private readonly byte[] _data;
	private long _position;

	public ByteArrayIStream(byte[] data) => _data = data;

	public unsafe HRESULT Read(nint pv, uint cb, nint pcbRead)
	{
		int count = (int)Math.Min((long)cb, _data.Length - _position);
		if (count <= 0)
		{
			if (pcbRead != nint.Zero) *(uint*)pcbRead = 0;
			return Constants.S_OK;
		}
		fixed (byte* src = _data)
		{
			System.Runtime.CompilerServices.Unsafe.CopyBlock((byte*)pv, src + _position, (uint)count);
		}
		_position += count;
		if (pcbRead != nint.Zero) *(uint*)pcbRead = (uint)count;
		return Constants.S_OK;
	}

	public unsafe HRESULT Write(nint pv, uint cb, nint pcbWritten)
	{
		if (pcbWritten != nint.Zero) *(uint*)pcbWritten = 0;
		return Constants.E_NOTIMPL;
	}

	public unsafe HRESULT Seek(long dlibMove, STREAM_SEEK dwOrigin, nint plibNewPosition)
	{
		_position = dwOrigin switch
		{
			STREAM_SEEK.STREAM_SEEK_SET => dlibMove,
			STREAM_SEEK.STREAM_SEEK_CUR => _position + dlibMove,
			STREAM_SEEK.STREAM_SEEK_END => _data.Length + dlibMove,
			_ => _position
		};
		_position = Math.Clamp(_position, 0, _data.Length);
		if (plibNewPosition != nint.Zero) *(ulong*)plibNewPosition = (ulong)_position;
		return Constants.S_OK;
	}

	public HRESULT SetSize(ulong libNewSize) => Constants.E_NOTIMPL;

	public HRESULT CopyTo(IStream pstm, ulong cb, nint pcbRead, nint pcbWritten) => Constants.E_NOTIMPL;

	public HRESULT Commit(uint grfCommitFlags) => Constants.S_OK;

	public HRESULT Revert() => Constants.S_OK;

	public HRESULT LockRegion(ulong libOffset, ulong cb, uint dwLockType) => Constants.E_NOTIMPL;

	public HRESULT UnlockRegion(ulong libOffset, ulong cb, uint dwLockType) => Constants.E_NOTIMPL;

	public HRESULT Stat(out STATSTG pstatstg, uint grfStatFlag)
	{
		pstatstg = new STATSTG { cbSize = (ulong)_data.Length };
		return Constants.S_OK;
	}

	public HRESULT Clone(out IStream ppstm)
	{
		ppstm = null!;
		return Constants.E_NOTIMPL;
	}
}

#endif // NET10_0_OR_GREATER
