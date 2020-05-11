using System;
using System.Buffers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Uno.Disposables;

namespace Windows.Storage.Streams
{
	public partial class DataReader : IDataReader, IDisposable
	{
		private readonly static ArrayPool<byte> _pool = ArrayPool<byte>.Create();
		private readonly byte[] _bytes;

		private int _bufferPosition = 0;

		private DataReader(IBuffer buffer)
		{
			_bytes = buffer.ToArray();
		}

		public static DataReader FromBuffer(IBuffer buffer)
		{
			return new DataReader(buffer);
		}

		public UnicodeEncoding UnicodeEncoding { get; set; }

		public ByteOrder ByteOrder { get; set; }

		public uint UnconsumedBufferLength => (uint)(_bytes.Length - _bufferPosition);

		public byte ReadByte()
		{
			VerifyRead(1);
			var value = _bytes[_bufferPosition];
			_bufferPosition++;
			return value;
		}

		public void ReadBytes(byte[] value)
		{
			VerifyRead(value.Length);
			Array.Copy(_bytes, value, value.Length);
			_bufferPosition += value.Length;
		}

		public IBuffer ReadBuffer(uint length)
		{
			VerifyRead((int)length);
			var bufferData = new byte[length];
			ReadBytes(bufferData);
			return new Buffer(bufferData);
		}

		public bool ReadBoolean()
		{
			VerifyRead(1);
			var result = BitConverter.ToBoolean(_bytes, _bufferPosition);
			_bufferPosition++;
			return result;
		}

		public Guid ReadGuid()
		{
			VerifyRead(16);
			using (RentArray(16, out var bytes))
			{
				ReadChunkFromBuffer(bytes);
				return new Guid(bytes);
			}
		}

		public short ReadInt16()
		{
			VerifyRead(2);
			using (RentArray(2, out var bytes))
			{
				ReadChunkFromBuffer(bytes);
				var value = BitConverter.ToInt16(bytes, 0);
				return value;
			}
		}

		public int ReadInt32()
		{
			VerifyRead(4);
			using (RentArray(4, out var bytes))
			{
				ReadChunkFromBuffer(bytes);
				var value = BitConverter.ToInt32(bytes, 0);
				return value;
			}
		}

		public long ReadInt64()
		{
			VerifyRead(8);
			using (RentArray(8, out var bytes))
			{
				ReadChunkFromBuffer(bytes);
				var value = BitConverter.ToInt64(bytes, 0);
				return value;
			}
		}

		public ushort ReadUInt16()
		{
			VerifyRead(2);
			using (RentArray(2, out var bytes))
			{
				ReadChunkFromBuffer(bytes);
				var value = BitConverter.ToUInt16(bytes, 0);
				return value;
			}
		}

		public uint ReadUInt32()
		{
			VerifyRead(4);
			using (RentArray(4, out var bytes))
			{
				ReadChunkFromBuffer(bytes);
				var value = BitConverter.ToUInt32(bytes, 0);
				return value;
			}
		}

		public ulong ReadUInt64()
		{
			VerifyRead(8);
			using (RentArray(8, out var bytes))
			{
				ReadChunkFromBuffer(bytes);
				var value = BitConverter.ToUInt64(bytes, 0);
				return value;
			}
		}

		public float ReadSingle()
		{
			VerifyRead(4);
			using (RentArray(4, out var bytes))
			{
				ReadChunkFromBuffer(bytes);
				var value = BitConverter.ToSingle(bytes, 0);
				return value;
			}
		}

		public double ReadDouble()
		{
			VerifyRead(8);
			using (RentArray(8, out var bytes))
			{
				ReadChunkFromBuffer(bytes);
				var value = BitConverter.ToDouble(bytes, 0);
				return value;
			}
		}

		public string ReadString(uint codeUnitCount)
		{
			VerifyRead((int)codeUnitCount);
			string result;
			switch (UnicodeEncoding)
			{
				case UnicodeEncoding.Utf8:
					result = Encoding.UTF8.GetString(_bytes, _bufferPosition, (int)codeUnitCount);
					break;
				case UnicodeEncoding.Utf16LE:
					result = Encoding.Unicode.GetString(_bytes, _bufferPosition, (int)codeUnitCount);
					break;
				case UnicodeEncoding.Utf16BE:
					result = Encoding.BigEndianUnicode.GetString(_bytes, _bufferPosition, (int)codeUnitCount);
					break;
				default:
					throw new InvalidOperationException("Unsupported UnicodeEncoding value.");
			}
			_bufferPosition += (int)codeUnitCount;
			return result;
		}

		public TimeSpan ReadTimeSpan()
		{
			var longValue = ReadInt64();
			return TimeSpan.FromTicks(longValue);
		}

		public void Dispose()
		{
		}

		private void VerifyRead(int count)
		{
			if (_bufferPosition + count >= _bytes.Length)
			{
				throw new InvalidOperationException($"Buffer too short to accomodate for {count} requested bytes.");
			}
		}

		private void ReadChunkFromBuffer(byte[] chunk)
		{
			var reverseOrder =
				(ByteOrder == ByteOrder.BigEndian && BitConverter.IsLittleEndian) ||
				(ByteOrder == ByteOrder.LittleEndian && !BitConverter.IsLittleEndian);

			Array.Copy(_bytes, chunk, chunk.Length);
			_bufferPosition += chunk.Length;
			if (reverseOrder)
			{
				Array.Reverse(chunk);
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
