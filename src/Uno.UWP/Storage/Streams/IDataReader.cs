using System;

namespace Windows.Storage.Streams
{
	public partial interface IDataReader
	{
		ByteOrder ByteOrder { get; set; }

		uint UnconsumedBufferLength { get; }

		UnicodeEncoding UnicodeEncoding { get; set; }

		byte ReadByte();

		void ReadBytes(byte[] value);

		IBuffer ReadBuffer(uint length);

		bool ReadBoolean();

		Guid ReadGuid();

		short ReadInt16();

		int ReadInt32();

		long ReadInt64();

		ushort ReadUInt16();

		uint ReadUInt32();

		ulong ReadUInt64();

		float ReadSingle();

		double ReadDouble();

		string ReadString(uint codeUnitCount);

		DateTimeOffset ReadDateTime();

		TimeSpan ReadTimeSpan();

		IBuffer DetachBuffer();
	}
}
