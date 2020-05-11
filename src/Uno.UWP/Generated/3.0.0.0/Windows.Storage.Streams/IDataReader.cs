#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IDataReader 
	{
		#if false || false || false || false || false
		global::Windows.Storage.Streams.ByteOrder ByteOrder
		{
			get;
			set;
		}
		#endif
		#if false || false || false || false || false
		global::Windows.Storage.Streams.InputStreamOptions InputStreamOptions
		{
			get;
			set;
		}
		#endif
		#if false || false || false || false || false
		uint UnconsumedBufferLength
		{
			get;
		}
		#endif
		#if false || false || false || false || false
		global::Windows.Storage.Streams.UnicodeEncoding UnicodeEncoding
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.IDataReader.UnconsumedBufferLength.get
		// Forced skipping of method Windows.Storage.Streams.IDataReader.UnicodeEncoding.get
		// Forced skipping of method Windows.Storage.Streams.IDataReader.UnicodeEncoding.set
		// Forced skipping of method Windows.Storage.Streams.IDataReader.ByteOrder.get
		// Forced skipping of method Windows.Storage.Streams.IDataReader.ByteOrder.set
		// Forced skipping of method Windows.Storage.Streams.IDataReader.InputStreamOptions.get
		// Forced skipping of method Windows.Storage.Streams.IDataReader.InputStreamOptions.set
		#if false || false || false || false || false
		byte ReadByte();
		#endif
		#if false || false || false || false || false
		void ReadBytes( byte[] value);
		#endif
		#if false || false || false || false || false
		global::Windows.Storage.Streams.IBuffer ReadBuffer( uint length);
		#endif
		#if false || false || false || false || false
		bool ReadBoolean();
		#endif
		#if false || false || false || false || false
		global::System.Guid ReadGuid();
		#endif
		#if false || false || false || false || false
		short ReadInt16();
		#endif
		#if false || false || false || false || false
		int ReadInt32();
		#endif
		#if false || false || false || false || false
		long ReadInt64();
		#endif
		#if false || false || false || false || false
		ushort ReadUInt16();
		#endif
		#if false || false || false || false || false
		uint ReadUInt32();
		#endif
		#if false || false || false || false || false
		ulong ReadUInt64();
		#endif
		#if false || false || false || false || false
		float ReadSingle();
		#endif
		#if false || false || false || false || false
		double ReadDouble();
		#endif
		#if false || false || false || false || false
		string ReadString( uint codeUnitCount);
		#endif
		#if false || false || false || false || false
		global::System.DateTimeOffset ReadDateTime();
		#endif
		#if false || false || false || false || false
		global::System.TimeSpan ReadTimeSpan();
		#endif
		#if false || false || false || false || false
		global::Windows.Storage.Streams.DataReaderLoadOperation LoadAsync( uint count);
		#endif
		#if false || false || false || false || false
		global::Windows.Storage.Streams.IBuffer DetachBuffer();
		#endif
		#if false || false || false || false || false
		global::Windows.Storage.Streams.IInputStream DetachStream();
		#endif
	}
}
