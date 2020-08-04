#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IDataReader 
	{
		// Skipping already declared property ByteOrder
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.InputStreamOptions InputStreamOptions
		{
			get;
			set;
		}
		#endif
		// Skipping already declared property UnconsumedBufferLength
		// Skipping already declared property UnicodeEncoding
		// Forced skipping of method Windows.Storage.Streams.IDataReader.UnconsumedBufferLength.get
		// Forced skipping of method Windows.Storage.Streams.IDataReader.UnicodeEncoding.get
		// Forced skipping of method Windows.Storage.Streams.IDataReader.UnicodeEncoding.set
		// Forced skipping of method Windows.Storage.Streams.IDataReader.ByteOrder.get
		// Forced skipping of method Windows.Storage.Streams.IDataReader.ByteOrder.set
		// Forced skipping of method Windows.Storage.Streams.IDataReader.InputStreamOptions.get
		// Forced skipping of method Windows.Storage.Streams.IDataReader.InputStreamOptions.set
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadByte()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadBytes(byte[])
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadBuffer(uint)
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadBoolean()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadGuid()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadInt16()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadInt32()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadInt64()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadUInt16()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadUInt32()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadUInt64()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadSingle()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadDouble()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadString(uint)
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadDateTime()
		// Skipping already declared method Windows.Storage.Streams.IDataReader.ReadTimeSpan()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.DataReaderLoadOperation LoadAsync( uint count);
		#endif
		// Skipping already declared method Windows.Storage.Streams.IDataReader.DetachBuffer()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.IInputStream DetachStream();
		#endif
	}
}
