using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using System.IO;

namespace Windows.Storage.Streams
{

	// currently only ctor without parameters is implemented,
	// but some methods are already in form for both ctors in API


	public partial class DataWriter : IDataWriter, IDisposable
	{
		// only one will be not null - depends on constructor used
		private readonly MemoryStream _memStream = null;
		private IOutputStream _outputStream = null;		// can be changed in DetachStream

		public UnicodeEncoding UnicodeEncoding { get; set; }

		public ByteOrder ByteOrder { get; set; }

		public uint UnstoredBufferLength
		{
			get => (uint)(_memStream.Length - _memStream.Position);
		}

		// when someone will implement StoreAsync() method:
		//public DataWriterStoreOperation StoreAsync()
		//{
		//	// when constructor was without paramaters, and this method is called, UWP throws:
		//	// System.Runtime.InteropServices.COMException: 'The operation identifier is not valid.
		//}

		public IAsyncOperation<bool> FlushAsync() => FlushAsyncTask().AsAsyncOperation<bool>();

		private async Task<bool> FlushAsyncTask()
		{
			if (_memStream != null)
			{
				await _memStream.FlushAsync();
				return true;
			}

			if (_outputStream != null)
			{
				return await _outputStream.FlushAsync();
			}
			return true;
		}

		public IOutputStream DetachStream()
		{
			// when constructor was without IOutputStream, simply return null
			if (_memStream != null)
			{
				return null;
			}

			// we don't want this stream anymore
			var temp = _outputStream;
			_outputStream=null;
			return temp;
		}


		public DataWriter()
		{
			_memStream = new MemoryStream();
		}

		public void WriteByte(byte value)
		{
			_memStream.WriteByte(value);
		}

		public void WriteBytes(byte[] value)
		{
			if(value is null)
			{
				throw new ArgumentNullException("Windows.Storage.Streams.DataWriter.WriteBytes(byte[]) called with null argument");
			}
			_memStream.Write(value, 0, value.Length);
		}

		public void WriteBoolean(bool value)
		{
			// same as System.BitConverter, but without call to it
			if (value)
			{
				_memStream.WriteByte(1);
			}
			else
			{
				_memStream.WriteByte(0);
			}
		}

		public void WriteGuid(Guid value)
		{
			// GUID is written as int32, int16, int16, int64; endianness is per every integer part, not for whole GUID
			byte[] guidBytes = value.ToByteArray();
			WriteInt32(BitConverter.ToInt32(guidBytes, 0));
			WriteInt16(BitConverter.ToInt16(guidBytes, 4));
			WriteInt16(BitConverter.ToInt16(guidBytes, 6));
			WriteInt64(BitConverter.ToInt64(guidBytes, 8));
		}

		private void WriteBytes(byte[] bytes, int size)
		{
			// maybe we should reverse array (if platform endianess is different than requested)
			if ((ByteOrder == ByteOrder.LittleEndian && !BitConverter.IsLittleEndian)
				|| (ByteOrder == ByteOrder.BigEndian && BitConverter.IsLittleEndian))
			{
				Array.Reverse(bytes);
			}

			_memStream.Write(bytes, 0, size);
		}

		public void WriteInt16(short value) => WriteBytes(BitConverter.GetBytes(value), 2);
		public void WriteInt32(int value) => WriteBytes(BitConverter.GetBytes(value), 4);
		public void WriteInt64(long value) => WriteBytes(BitConverter.GetBytes(value), 8);
		public void WriteUInt16(ushort value) => WriteBytes(BitConverter.GetBytes(value), 2);
		public void WriteUInt32(uint value) => WriteBytes(BitConverter.GetBytes(value), 4);
		public void WriteUInt64(ulong value) => WriteBytes(BitConverter.GetBytes(value), 8);
		public void WriteSingle(float value)
		{   // yes, it is endianness dependant
			WriteBytes(BitConverter.GetBytes(value), 4);
		}
		public void WriteDouble(double value)
		{   // yes, it is endianness dependant
			WriteBytes(BitConverter.GetBytes(value), 8);
		}
		public void WriteDateTime(DateTimeOffset value)
		{ // it seems like documentation error...
		  // implementation here is based on testing how it works on UWP
			var epoch1600 = new DateTime(1601, 1, 1, 0, 0, 0);
			long ticks = (value - epoch1600.ToLocalTime()).Ticks;
			WriteInt64(ticks);
		}
		public void WriteTimeSpan(TimeSpan value) => WriteInt64(value.Ticks);

		public uint MeasureString(string value)
		{
			switch (this.UnicodeEncoding)
			{
				case UnicodeEncoding.Utf16BE:
					return (uint)(Encoding.Unicode.GetBytes(value).Length);
				case UnicodeEncoding.Utf16LE:
					return (uint)(Encoding.BigEndianUnicode.GetBytes(value).Length);
				default: // UnicodeEncoding.Utf8
					return (uint)(Encoding.UTF8.GetBytes(value).Length);
			}
		}

		public uint WriteString(string value)
		{
			byte[] dump;

			switch (this.UnicodeEncoding)
			{
				case UnicodeEncoding.Utf16BE:
					dump = Encoding.Unicode.GetBytes(value);
					break;
				case UnicodeEncoding.Utf16LE:
					dump = Encoding.BigEndianUnicode.GetBytes(value);
					break;
				default: // UnicodeEncoding.Utf8
					dump = Encoding.UTF8.GetBytes(value);
					break;
			}
			WriteBytes(dump);
			return (uint)dump.Length;
		}

		public void WriteBuffer(IBuffer buffer)
		{
			if (buffer is null)
			{
				throw new ArgumentNullException("Windows.Storage.Streams.DataWriter.WriteBuffer(IBuffer) called with null argument");
			}

			WriteBytes(buffer.Data);
		}

		public void WriteBuffer(IBuffer buffer, uint start, uint count)
		{
			if (buffer is null)
			{
				throw new ArgumentNullException("Windows.Storage.Streams.DataWriter.WriteBuffer(IBuffer,uint,uint) called with null argument");
			}

			if(start >= buffer.Length)
			{
				throw new ArgumentException("Windows.Storage.Streams.DataWriter.WriteBuffer(IBuffer,uint,uint) called with start index greater than buffer length");
			}

			if (start+count > buffer.Length)
			{	// copy not more than there is data in buffer
				count = buffer.Length - start;
			}

			byte[] partBuffer = new byte[count];
			for (int i = 0; i < count; i++)
			{
				partBuffer[i] = buffer.Data[start + i];
			}
			WriteBytes(partBuffer);
		}


		public IBuffer DetachBuffer()
		{
			byte[] array = _memStream.ToArray();    // makes copy, and truncate on length (GetBuffer doesn't make copy, but also doesn't truncate)
			var buffer = new InMemoryBuffer(array);
			return buffer;
		}

		public void Dispose()
		{
			_memStream?.Dispose();
			_outputStream?.Dispose();
		}


	}
}
