using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	/// <summary>
	/// Writes data to an output stream.
	/// </summary>
	public sealed partial class DataWriter : IDataWriter, IDisposable
	{
		private readonly IOutputStream _outputStream = null!; // TODO: Fix nullability annotation.
		private MemoryStream _memoryStream;

		/// <summary>
		/// Creates and initializes a new instance of the data writer.
		/// </summary>
		public DataWriter()
		{
			_memoryStream = new MemoryStream();
		}

		public DataWriter(IOutputStream outputStream) : this()
		{
			_outputStream = outputStream;
		}

		/// <summary>
		/// Gets or sets the Unicode character encoding for the output stream.
		/// </summary>
		public UnicodeEncoding UnicodeEncoding { get; set; }

		/// <summary>
		/// Gets or sets the byte order of the data in the output stream.
		/// </summary>
		public ByteOrder ByteOrder { get; set; }

		/// <summary>
		/// Gets the size of the buffer that has not been used.
		/// </summary>
		public uint UnstoredBufferLength => (uint)(_memoryStream.Length - _memoryStream.Position);

		/// <summary>
		/// Flushes data asynchronously.
		/// </summary>
		/// <returns>The stream flush operation.</returns>
		public IAsyncOperation<bool> FlushAsync() => FlushInternalAsync().AsAsyncOperation<bool>();

		/// <summary>
		/// Writes a byte value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteByte(byte value) => _memoryStream.WriteByte(value);

		/// <summary>
		/// Writes an array of byte values to the output stream.
		/// </summary>
		/// <param name="value">The array of values.</param>
		public void WriteBytes(byte[] value)
		{
			if (value is null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			_memoryStream.Write(value, 0, value.Length);
		}

		/// <summary>
		/// Writes the contents of the specified buffer to the output stream.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		public void WriteBuffer(IBuffer buffer)
		{
			if (buffer is null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			var data = Buffer.Cast(buffer).GetSegment();
			_memoryStream.Write(data.Array!, data.Offset, data.Count);
		}

		/// <summary>
		/// Writes the specified bytes from a buffer to the output stream.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <param name="start">The starting byte.</param>
		/// <param name="count">The number of bytes to write.</param>
		public void WriteBuffer(IBuffer buffer, uint start, uint count)
		{
			if (buffer is null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			if (start >= buffer.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(start));
			}

			var data = Buffer.Cast(buffer).GetSegment();
			if (count > data.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			_memoryStream.Write(data.Array!, data.Offset + (int)start, (int)count);
		}

		/// <summary>
		/// Writes a Boolean value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteBoolean(bool value) => WriteChunkToUnstoredBuffer(BitConverter.GetBytes(value));

		/// <summary>
		/// Writes a GUID value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteGuid(Guid value)
		{
			// GUID is written as int32, int16, int16, int64; endianness is per every integer part, not for whole GUID
			byte[] guidBytes = value.ToByteArray();
			WriteInt32(BitConverter.ToInt32(guidBytes, 0));
			WriteInt16(BitConverter.ToInt16(guidBytes, 4));
			WriteInt16(BitConverter.ToInt16(guidBytes, 6));
			WriteInt64(BitConverter.ToInt64(guidBytes, 8));
		}

		/// <summary>
		/// Writes a 16-bit integer value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteInt16(short value) => WriteChunkToUnstoredBuffer(BitConverter.GetBytes(value));

		/// <summary>
		/// Writes a 32-bit integer value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteInt32(int value) => WriteChunkToUnstoredBuffer(BitConverter.GetBytes(value));

		/// <summary>
		/// Writes a 64-bit integer value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteInt64(long value) => WriteChunkToUnstoredBuffer(BitConverter.GetBytes(value));

		/// <summary>
		/// Writes a 16-bit unsigned integer value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteUInt16(ushort value) => WriteChunkToUnstoredBuffer(BitConverter.GetBytes(value));

		/// <summary>
		/// Writes a 32-bit unsigned integer value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteUInt32(uint value) => WriteChunkToUnstoredBuffer(BitConverter.GetBytes(value));

		/// <summary>
		/// Writes a 64-bit unsigned integer value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteUInt64(ulong value) => WriteChunkToUnstoredBuffer(BitConverter.GetBytes(value));

		/// <summary>
		/// Writes a floating-point value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteSingle(float value) => WriteChunkToUnstoredBuffer(BitConverter.GetBytes(value));

		/// <summary>
		/// Writes a floating-point value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteDouble(double value) => WriteChunkToUnstoredBuffer(BitConverter.GetBytes(value));

		/// <summary>
		/// Writes a date and time value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteDateTime(DateTimeOffset value)
		{
			// Implementation in UWP is using Win32 FileTime format (starting on 1st January 1601)
			// As contrary to Win32 however also supports pre-1601 dates
			var epoch1601 = new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero);
			long ticks = (value - epoch1601).Ticks;
			WriteInt64(ticks);
		}

		/// <summary>
		/// Writes a time-interval value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteTimeSpan(TimeSpan value) => WriteChunkToUnstoredBuffer(BitConverter.GetBytes(value.Ticks));

		/// <summary>
		/// Writes a string value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The length of the string, in bytes.</returns>
		public uint WriteString(string value)
		{
			byte[] stringBytes;
			switch (UnicodeEncoding)
			{
				case UnicodeEncoding.Utf8:
					stringBytes = Encoding.UTF8.GetBytes(value);
					break;
				case UnicodeEncoding.Utf16LE:
					stringBytes = Encoding.Unicode.GetBytes(value);
					break;
				case UnicodeEncoding.Utf16BE:
					stringBytes = Encoding.BigEndianUnicode.GetBytes(value);
					break;
				default:
					throw new InvalidOperationException("Unsupported UnicodeEncoding value.");
			}
			_memoryStream.Write(stringBytes, 0, stringBytes.Length);
			return (uint)stringBytes.Length;
		}

		/// <summary>
		/// Gets the size of a string.
		/// </summary>
		/// <param name="value">The string.</param>
		/// <returns>The size of the string, in bytes.</returns>
		public uint MeasureString(string value)
		{
			switch (UnicodeEncoding)
			{
				case UnicodeEncoding.Utf8:
					return (uint)Encoding.UTF8.GetByteCount(value);
				case UnicodeEncoding.Utf16LE:
					return (uint)Encoding.Unicode.GetByteCount(value);
				case UnicodeEncoding.Utf16BE:
					return (uint)Encoding.BigEndianUnicode.GetByteCount(value);
				default:
					throw new InvalidOperationException("Unsupported UnicodeEncoding value.");
			}
		}

		/// <summary>
		/// Detaches the buffer that is associated with the data writer.
		/// </summary>
		/// <returns>The detached buffer.</returns>
		public IBuffer DetachBuffer()
		{
			var result = new Buffer(_memoryStream.ToArray());
			_memoryStream.Dispose();
			_memoryStream = new MemoryStream();
			return result;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() => _memoryStream?.Dispose();

		private async Task<bool> FlushInternalAsync()
		{
			if (_memoryStream != null)
			{
				await _memoryStream.FlushAsync();
			}

			return true;
		}

		private void WriteChunkToUnstoredBuffer(byte[] chunk)
		{
			var reverseOrder =
				(ByteOrder == ByteOrder.BigEndian && BitConverter.IsLittleEndian) ||
				(ByteOrder == ByteOrder.LittleEndian && !BitConverter.IsLittleEndian);

			if (reverseOrder)
			{
				Array.Reverse(chunk);
			}
			_memoryStream.Write(chunk, 0, chunk.Length);
		}


		public DataWriterStoreOperation StoreAsync() =>
			new DataWriterStoreOperation(StoreTaskAsync().AsAsyncOperation<uint>());

		private async Task<uint> StoreTaskAsync()
		{
			var cancelToken = new CancellationTokenSource();
			var inBuffer = _memoryStream.GetBuffer();
			await _outputStream.WriteAsync(inBuffer, 0, (int)_memoryStream.Length, cancelToken.Token);
			await _outputStream.FlushAsync();
			return (uint)inBuffer.Length;
		}
	}
}
