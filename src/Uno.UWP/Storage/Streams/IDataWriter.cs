using System;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public partial interface IDataWriter
	{
		ByteOrder ByteOrder { get; set; }

		UnicodeEncoding UnicodeEncoding { get; set; }

		uint UnstoredBufferLength { get; }

		void WriteByte(byte value);

		void WriteBytes(byte[] value);

		void WriteBuffer(IBuffer buffer);

		void WriteBuffer(IBuffer buffer, uint start, uint count);

		void WriteBoolean(bool value);

		void WriteGuid(Guid value);

		void WriteInt16(short value);

		void WriteInt32(int value);

		void WriteInt64(long value);

		void WriteUInt16(ushort value);

		void WriteUInt32(uint value);

		void WriteUInt64(ulong value);

		void WriteSingle(float value);

		void WriteDouble(double value);

		void WriteDateTime(DateTimeOffset value);

		void WriteTimeSpan(TimeSpan value);

		uint WriteString(string value);

		uint MeasureString(string value);

		IAsyncOperation<bool> FlushAsync();

		IBuffer DetachBuffer();
	}
}
