using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Storage.Streams
{
	public partial interface IDataWriter
	{
		ByteOrder ByteOrder
		{
			get;
			set;
		}
		uint UnstoredBufferLength
		{
			get;
		}
		UnicodeEncoding UnicodeEncoding
		{
			get;
			set;
		}

		void WriteByte(byte value);
		void WriteBytes(byte[] value);
		void WriteBuffer(IBuffer buffer);
		void WriteBuffer(IBuffer buffer, uint start, uint count);
		void WriteBoolean(bool value);
		void WriteGuid(global::System.Guid value);
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
		DataWriterStoreOperation StoreAsync();
		Foundation.IAsyncOperation<bool> FlushAsync();
		IBuffer DetachBuffer();
		IOutputStream DetachStream();
	}
}
