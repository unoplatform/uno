using System;
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

		internal Span<byte> Span => _data.Span;

		internal byte GetByte(int index)
		{
			return _data.Span[index];
		}

		/// <summary>
		/// Retrieve the underlying data array
		/// </summary>
		internal ArraySegment<byte> GetSegment()
		{
			if (MemoryMarshal.TryGetArray<byte>(_data, out var array))
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
		internal byte[] ToArray(int start, int count)
		{
			return _data.Slice(start, count).ToArray();
		}

		internal void CopyTo(int index, byte[] destination, int destinationIndex, int count)
		{
			var src = _data.Slice(index, count);
			var dst = new Memory<byte>(destination, destinationIndex, count);

			src.CopyTo(dst);
		}

		public uint Capacity => (uint)_data.Length;

		public uint Length { get; set; }
	}
}
