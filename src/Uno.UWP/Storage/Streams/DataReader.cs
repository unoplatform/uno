using System;
using Uno.Buffers;
using System.Text;
using Uno.Disposables;

namespace Windows.Storage.Streams
{
	/// <summary>
	/// Reads data from an input stream.
	/// </summary>
	public sealed partial class DataReader : IDataReader, IDisposable
	{
		private readonly static ArrayPool<byte> _pool = ArrayPool<byte>.Create();
		private readonly Buffer _buffer;

		private int _bufferPosition = 0;

		private DataReader(IBuffer buffer)
		{
			_buffer = Buffer.Cast(buffer ?? throw new ArgumentNullException(nameof(buffer)));
		}

		/// <summary>
		/// Creates a new instance of the data reader with data from the specified buffer.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <returns>The data reader.</returns>
		public static DataReader FromBuffer(IBuffer buffer) => new DataReader(buffer);

		/// <summary>
		/// Gets or sets the Unicode character encoding for the input stream.
		/// </summary>
		public UnicodeEncoding UnicodeEncoding { get; set; }

		/// <summary>
		/// Gets or sets the byte order of the data in the input stream.
		/// </summary>
		public ByteOrder ByteOrder { get; set; }

		/// <summary>
		/// Gets the size of the buffer that has not been read.
		/// </summary>
		public uint UnconsumedBufferLength => (uint)(_buffer.Length - _bufferPosition);

		/// <summary>
		/// Reads a byte value from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public byte ReadByte()
		{
			VerifyRead(1);
			return ReadByteFromBuffer();
		}

		/// <summary>
		/// Reads an array of byte values from the input stream.
		/// </summary>
		/// <param name="value">The array that receives the byte values.</param>
		public void ReadBytes(byte[] value)
		{
			if (value is null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			VerifyRead(value.Length);
			ReadBytesFromBuffer(value, value.Length);
		}

		/// <summary>
		/// Reads a buffer from the input stream.
		/// </summary>
		/// <param name="length">The length of the buffer, in bytes.</param>
		/// <returns>The buffer.</returns>
		public IBuffer ReadBuffer(uint length)
		{
			VerifyRead((int)length);
			var bufferData = new byte[length];
			ReadBytes(bufferData);
			return new Buffer(bufferData);
		}

		/// <summary>
		/// Reads a date and time value from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public DateTimeOffset ReadDateTime()
		{
			// UWP reads the date and returns the result in local timezone
			long ticks = ReadInt64();
			var date = new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero);
			date = date.AddTicks(ticks);
			
			return date.ToLocalTime();
		}

		/// <summary>
		/// Reads a Boolean value from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public bool ReadBoolean()
		{
			VerifyRead(1);
			var nextByte = ReadByteFromBuffer();
			return nextByte != 0;
		}

		/// <summary>
		/// Reads a GUID value from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public Guid ReadGuid()
		{
			var u32 = ReadInt32();
			var u16a = ReadInt16();
			var u16b = ReadInt16();
			var u64 = new byte[8];
			ReadChunkFromBuffer(u64, 8);
			return new Guid(u32, u16a, u16b, u64);
		}

		/// <summary>
		/// Reads a 16-bit integer value from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public short ReadInt16()
		{
			VerifyRead(2);
			using (RentArray(2, out var bytes))
			{
				ReadChunkFromBuffer(bytes, 2);
				return BitConverter.ToInt16(bytes, 0);
			}
		}

		/// <summary>
		/// Reads a 32-bit integer value from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public int ReadInt32()
		{
			VerifyRead(4);
			using (RentArray(4, out var bytes))
			{
				ReadChunkFromBuffer(bytes, 4);
				return BitConverter.ToInt32(bytes, 0);
			}
		}

		/// <summary>
		/// Reads a 64-bit integer value from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public long ReadInt64()
		{
			VerifyRead(8);
			using (RentArray(8, out var bytes))
			{
				ReadChunkFromBuffer(bytes, 8);
				return BitConverter.ToInt64(bytes, 0);
			}
		}

		/// <summary>
		/// Reads a 16-bit unsigned integer from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public ushort ReadUInt16()
		{
			VerifyRead(2);
			using (RentArray(2, out var bytes))
			{
				ReadChunkFromBuffer(bytes, 2);
				return BitConverter.ToUInt16(bytes, 0);
			}
		}

		/// <summary>
		/// Reads a 32-bit unsigned integer from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public uint ReadUInt32()
		{
			VerifyRead(4);
			using (RentArray(4, out var bytes))
			{
				ReadChunkFromBuffer(bytes, 4);
				return BitConverter.ToUInt32(bytes, 0);
			}
		}

		/// <summary>
		/// Reads a 64-bit unsigned integer from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public ulong ReadUInt64()
		{
			VerifyRead(8);
			using (RentArray(8, out var bytes))
			{
				ReadChunkFromBuffer(bytes, 8);
				return BitConverter.ToUInt64(bytes, 0);
			}
		}

		/// <summary>
		/// Reads a floating-point value from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public float ReadSingle()
		{
			VerifyRead(4);
			using (RentArray(4, out var bytes))
			{
				ReadChunkFromBuffer(bytes, 4);
				var value = BitConverter.ToSingle(bytes, 0);
				return value;
			}
		}

		/// <summary>
		/// Reads a floating-point value from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public double ReadDouble()
		{
			VerifyRead(8);
			using (RentArray(8, out var bytes))
			{
				ReadChunkFromBuffer(bytes, 8);
				return BitConverter.ToDouble(bytes, 0);
			}
		}

		/// <summary>
		/// Reads a string value from the input stream.
		/// </summary>
		/// <param name="codeUnitCount">The length of the string.</param>
		/// <returns>The value.</returns>
		public string ReadString(uint codeUnitCount)
		{
			// although docs says that input parameter is "codeUnitCount", sample
			// https://docs.microsoft.com/en-us/uwp/api/Windows.Storage.Streams.DataReader?view=winrt-19041
			// shows that it is BYTE count, not CODEUNIT count.
			// codepoint in UTF-8 can be encoded in anything from 1 to 6 bytes.

			int length = (int)codeUnitCount;
			VerifyRead(length);

			var data = _buffer.GetSegment();

			string result;
			switch (UnicodeEncoding)
			{
				case UnicodeEncoding.Utf8:
					result = Encoding.UTF8.GetString(data.Array!, data.Offset + _bufferPosition, length);
					break;
				case UnicodeEncoding.Utf16LE:
					result = Encoding.Unicode.GetString(data.Array!, data.Offset + _bufferPosition, length * 2);
					break;
				case UnicodeEncoding.Utf16BE:
					result = Encoding.BigEndianUnicode.GetString(data.Array!, data.Offset + _bufferPosition, length * 2);
					break;
				default:
					throw new InvalidOperationException("Unsupported UnicodeEncoding value.");
			}
			_bufferPosition += length;
			return result;
		}

		/// <summary>
		/// Reads a time-interval value from the input stream.
		/// </summary>
		/// <returns>The value.</returns>
		public TimeSpan ReadTimeSpan() => TimeSpan.FromTicks(ReadInt64());

		/// <summary>
		/// Detaches the buffer that is associated with the data reader.
		/// This is useful if you want to retain the buffer after you dispose the data reader.
		/// </summary>
		/// <returns>The detached buffer.</returns>
		public IBuffer DetachBuffer() => _buffer;

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
		}

		private void VerifyRead(int count)
		{
			if (_bufferPosition + count > _buffer.Length)
			{
				throw new InvalidOperationException($"Buffer too short to accomodate for {count} requested bytes.");
			}
		}

		private byte ReadByteFromBuffer()
		{
			return _buffer.GetByte((uint)(_bufferPosition++));
		}

		private void ReadBytesFromBuffer(byte[] data, int length)
		{
			_buffer.CopyTo((uint)_bufferPosition, data, 0, length);
			_bufferPosition += length;
		}

		private void ReadChunkFromBuffer(byte[] chunk, int length)
		{
			var reverseOrder =
				(ByteOrder == ByteOrder.BigEndian && BitConverter.IsLittleEndian) ||
				(ByteOrder == ByteOrder.LittleEndian && !BitConverter.IsLittleEndian);

			ReadBytesFromBuffer(chunk, length);

			if (reverseOrder)
			{
				Array.Reverse(chunk);

				if (chunk.Length > length)
				{
					// reversing moved the chunk to the end of the array
					// move the relevant bytes to the start
					for (int index = 0; index < length; index++)
					{
						var source = chunk.Length - length + index;
						chunk[index] = chunk[source];
						chunk[source] = 0;
					}
				}
			}
		}

		private IDisposable RentArray(int length, out byte[] rentedArray)
		{
			var poolArray = _pool.Rent(length);
			rentedArray = poolArray;
			return Disposable.Create(() => _pool.Return(poolArray));
		}
	}
}
