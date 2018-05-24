#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IDataReader 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.Storage.Streams.ByteOrder ByteOrder
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.Storage.Streams.InputStreamOptions InputStreamOptions
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint UnconsumedBufferLength
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		byte ReadByte();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void ReadBytes( byte[] value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.Storage.Streams.IBuffer ReadBuffer( uint length);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool ReadBoolean();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::System.Guid ReadGuid();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		short ReadInt16();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		int ReadInt32();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		long ReadInt64();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ushort ReadUInt16();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint ReadUInt32();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ulong ReadUInt64();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		float ReadSingle();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		double ReadDouble();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		string ReadString( uint codeUnitCount);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::System.DateTimeOffset ReadDateTime();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::System.TimeSpan ReadTimeSpan();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.Storage.Streams.DataReaderLoadOperation LoadAsync( uint count);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.Storage.Streams.IBuffer DetachBuffer();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.Storage.Streams.IInputStream DetachStream();
		#endif
	}
}
