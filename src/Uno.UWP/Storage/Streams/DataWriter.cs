using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public partial class DataWriter : IDataWriter, IDisposable
	{
		private readonly List<byte> _unstoredBuffer = new List<byte>();

		public DataWriter()
		{

		}

		public UnicodeEncoding UnicodeEncoding { get; set; }

		public ByteOrder ByteOrder { get; set; }

		public uint UnstoredBufferLength => (uint)_unstoredBuffer.Count;

		public void WriteByte(byte value) => _unstoredBuffer.Add(value);

		public void WriteBytes(byte[] value) => _unstoredBuffer.AddRange(value);

		public void WriteBuffer(IBuffer buffer)
		{
			if (buffer is InMemoryBuffer inMemory)
			{

			}
		}

		public void WriteBuffer(IBuffer buffer, uint start, uint count)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteBuffer(IBuffer buffer, uint start, uint count)");
		}

		public void WriteBoolean(bool value)
		{
			var bytes = BitConverter.GetBytes(value);
			AddChunkToUnstoredBuffer(bytes);
		}

		public void WriteGuid(Guid value)
		{
			var bytes = value.ToByteArray();
			AddChunkToUnstoredBuffer(bytes);
		}

		public void WriteInt16(short value)
		{
			var bytes = BitConverter.GetBytes(value);
			AddChunkToUnstoredBuffer(bytes);
		}

		public void WriteInt32(int value)
		{
			var bytes = BitConverter.GetBytes(value);
			AddChunkToUnstoredBuffer(bytes);
		}

		public void WriteInt64(long value)
		{
			var bytes = BitConverter.GetBytes(value);
			AddChunkToUnstoredBuffer(bytes);
		}

		public void WriteUInt16(ushort value)
		{
			var bytes = BitConverter.GetBytes(value);
			AddChunkToUnstoredBuffer(bytes);
		}

		public void WriteUInt32(uint value)
		{
			var bytes = BitConverter.GetBytes(value);
			AddChunkToUnstoredBuffer(bytes);
		}

		public void WriteUInt64(ulong value)
		{
			var bytes = BitConverter.GetBytes(value);
			AddChunkToUnstoredBuffer(bytes);
		}

		public void WriteSingle(float value)
		{
			var bytes = BitConverter.GetBytes(value);
			AddChunkToUnstoredBuffer(bytes);
		}

		public void WriteDouble(double value)
		{
			var bytes = BitConverter.GetBytes(value);
			AddChunkToUnstoredBuffer(bytes);
		}

		public void WriteDateTime(DateTimeOffset value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteDateTime(DateTimeOffset value)");
		}

		/// <summary>
		/// Writes a time-interval value to the output stream.
		/// </summary>
		/// <param name="value">The value.</param>
		public void WriteTimeSpan(TimeSpan value)
		{
			var ticks = value.Ticks;
			var bytes = BitConverter.GetBytes(ticks);

			AddChunkToUnstoredBuffer(bytes);
		}

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
			_unstoredBuffer.AddRange(stringBytes);
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

		public IAsyncOperation<bool> FlushAsync()
		{
			throw new NotImplementedException("The member IAsyncOperation<bool> DataWriter.FlushAsync() is not implemented in Uno.");
		}

		public IBuffer DetachBuffer()
		{
			throw new NotImplementedException("The member IBuffer DataWriter.DetachBuffer() is not implemented in Uno.");
		}

		public void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.Dispose()");
		}

		private void AddChunkToUnstoredBuffer(byte[] chunk)
		{
			var reverseOrder =
				(ByteOrder == ByteOrder.BigEndian && BitConverter.IsLittleEndian) ||
				(ByteOrder == ByteOrder.LittleEndian && !BitConverter.IsLittleEndian);

			_unstoredBuffer.AddRange(reverseOrder ? chunk.Reverse() : chunk);
		}
	}
}
