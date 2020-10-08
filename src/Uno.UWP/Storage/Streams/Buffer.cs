using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public partial class Buffer : IBuffer
	{
		/// <summary>
		/// A default length to use for buffer copy when none specified
		/// </summary>
		internal const int DefaultCapacity = 1024 * 1024; // 1M

		private readonly Memory<byte> _data;

		internal static Buffer Cast(IBuffer impl)
		{
			if (impl is Buffer buffer)
			{
				return buffer;
			}
			else if (impl is null)
			{
				throw new ArgumentNullException(nameof(impl));
			}
			else
			{
				throw new NotSupportedException("This type of buffer is not supported.");
			}
		}

		public Buffer(uint capacity)
		{
			_data = new Memory<byte>(new byte[capacity]);
		}

		internal Buffer(byte[] data)
		{
			_data = new Memory<byte>(data);
			Length = Capacity;
		}

		internal Buffer(Memory<byte> data)
		{
			_data = data;
			Length = (uint)data.Length;
		}

		public uint Capacity => (uint)_data.Length;

		public uint Length { get; set; }

		internal Span<byte> Span => _data.Span;

		internal byte GetByte(uint index)
		{
			return _data.Span[(int)index];
		}

		/// <summary>
		/// Retrieve the underlying data array
		/// </summary>
		/// <remarks>
		/// The <see cref="ArraySegment{T}.Array"/> of the return cannot be null.
		/// This method will throw an <see cref="InvalidOperationException"/> in that case.
		/// </remarks>
		internal ArraySegment<byte> GetSegment()
		{
			if (MemoryMarshal.TryGetArray<byte>(_data, out var array)
				&& array.Array != null)
			{
				return array;
			}
			else
			{
				throw new InvalidOperationException("Cannot get the segment from buffer.");
			}
		}

		/// <summary>
		/// **CLONES** the content of this buffer into a new byte[]
		/// </summary>
		internal byte[] ToArray()
		{
			return _data.ToArray();
		}

		/// <summary>
		/// **CLONES** a part of the content of this buffer into a new byte[]
		/// </summary>
		internal byte[] ToArray(uint start, int count)
		{
			return _data.Slice((int)start, count).ToArray();
		}

		internal void CopyTo(uint sourceIndex, byte[] destination, int destinationIndex, int count)
		{
			var src = _data.Slice((int)sourceIndex, count);
			var dst = new Memory<byte>(destination, destinationIndex, count);

			src.CopyTo(dst);
		}

		internal void CopyTo(uint sourceIndex, IBuffer destination, uint destinationIndex, uint count)
		{
			var dst = Cast(destination);

			var srcMemory = _data.Slice((int)sourceIndex, (int)count);
			var dstMemory = dst._data.Slice((int)destinationIndex, (int)count);

			srcMemory.CopyTo(dstMemory);
		}
	}
}
