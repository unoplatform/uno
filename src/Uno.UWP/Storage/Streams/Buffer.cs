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
		private uint _length;

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

		public uint Length
		{
			get => _length;
			set
			{
				if (value > Capacity) throw new ArgumentOutOfRangeException(nameof(value), "Length cannot be greater than capacity.");
				_length = value;
			}
		}

		/// <summary>
		/// Retrieve the underlying data array.
		/// WARNING: DANGEROUS PROPERTY cf. remarks
		/// </summary>
		/// <remarks>
		/// This property gives direct access to the underlying data, which means that if it is being modified,
		/// this buffer won't automatically reflect the change.
		/// **It's your responsibility to update it.**
		/// For instance if you write some data in the array,
		/// you have to make sure to update the <see cref="Length"/> of this buffer accordingly.
		/// </remarks>
		internal Span<byte> Span => _data.Span;

		/// <summary>
		/// Retrieve the underlying data array.
		/// WARNING: DANGEROUS METHOD cf. remarks
		/// </summary>
		/// <remarks>
		/// The <see cref="ArraySegment{T}.Array"/> of the return cannot be null.
		/// This method will throw an <see cref="InvalidOperationException"/> in that case.
		/// </remarks>
		/// <remarks>
		/// This method gives direct access to the underlying data, which means that if it is being modified,
		/// this buffer won't automatically reflect the change.
		/// **It's your responsibility to update it.**
		/// For instance if you write some data in the array,
		/// you have to make sure to update the <see cref="Length"/> of this buffer accordingly.
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

		internal byte GetByte(uint index)
		{
			if (index >= Capacity)
			{
				throw new ArgumentOutOfRangeException(nameof(index), "Cannot get byte at index greater than capacity.");
			}

			return _data.Span[(int)index];
		}

		/// <summary>
		/// **CLONES** the content of this buffer into a new byte[]
		/// </summary>
		internal byte[] ToArray()
		{
			return _data.Slice(0, (int)Length).ToArray();
		}

		/// <summary>
		/// Gets a <see cref="ReadOnlyMemory{T}"/> of the current buffer
		/// </summary>
		internal ReadOnlyMemory<byte> AsReadOnlyMemory()
			=> _data.Slice(0, (int)Length);

		/// <summary>
		/// **CLONES** a part of the content of this buffer into a new byte[]
		/// </summary>
		internal byte[] ToArray(uint start, int count)
		{
			return _data.Slice((int)start, count).ToArray();
		}

		internal void Write(uint index, byte[] source, int sourceIndex, int count)
		{
			var src = new Span<byte>(source, sourceIndex, count);
			var dst = Span.Slice((int)index, count);

			src.CopyTo(dst);

			Length = (uint)Math.Max(Length, index + count);
		}

		internal void CopyTo(uint sourceIndex, byte[] destination, int destinationIndex, int count)
		{
			var src = Span.Slice((int)sourceIndex, count);
			var dst = new Span<byte>(destination, destinationIndex, count);

			src.CopyTo(dst);
		}

		internal void CopyTo(uint sourceIndex, IBuffer destination, uint destinationIndex, uint count)
		{
			var dstBuffer = Cast(destination);

			var src = _data.Slice((int)sourceIndex, (int)count);
			var dst = dstBuffer._data.Slice((int)destinationIndex, (int)count);

			src.CopyTo(dst);

			dstBuffer.Length = (uint)Math.Max(dstBuffer.Length, destinationIndex + count);
		}
	}
}
