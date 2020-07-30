#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IDataWriter 
	{
		#if false || false || false || false || false
		global::Windows.Storage.Streams.ByteOrder ByteOrder
		{
			get;
			set;
		}
		#endif
		#if false || false || false || false || false
		global::Windows.Storage.Streams.UnicodeEncoding UnicodeEncoding
		{
			get;
			set;
		}
		#endif
		#if false || false || false || false || false
		uint UnstoredBufferLength
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.IDataWriter.UnstoredBufferLength.get
		// Forced skipping of method Windows.Storage.Streams.IDataWriter.UnicodeEncoding.get
		// Forced skipping of method Windows.Storage.Streams.IDataWriter.UnicodeEncoding.set
		// Forced skipping of method Windows.Storage.Streams.IDataWriter.ByteOrder.get
		// Forced skipping of method Windows.Storage.Streams.IDataWriter.ByteOrder.set
		#if false || false || false || false || false
		void WriteByte( byte value);
		#endif
		#if false || false || false || false || false
		void WriteBytes( byte[] value);
		#endif
		#if false || false || false || false || false
		void WriteBuffer( global::Windows.Storage.Streams.IBuffer buffer);
		#endif
		#if false || false || false || false || false
		void WriteBuffer( global::Windows.Storage.Streams.IBuffer buffer,  uint start,  uint count);
		#endif
		#if false || false || false || false || false
		void WriteBoolean( bool value);
		#endif
		#if false || false || false || false || false
		void WriteGuid( global::System.Guid value);
		#endif
		#if false || false || false || false || false
		void WriteInt16( short value);
		#endif
		#if false || false || false || false || false
		void WriteInt32( int value);
		#endif
		#if false || false || false || false || false
		void WriteInt64( long value);
		#endif
		#if false || false || false || false || false
		void WriteUInt16( ushort value);
		#endif
		#if false || false || false || false || false
		void WriteUInt32( uint value);
		#endif
		#if false || false || false || false || false
		void WriteUInt64( ulong value);
		#endif
		#if false || false || false || false || false
		void WriteSingle( float value);
		#endif
		#if false || false || false || false || false
		void WriteDouble( double value);
		#endif
		#if false || false || false || false || false
		void WriteDateTime( global::System.DateTimeOffset value);
		#endif
		#if false || false || false || false || false
		void WriteTimeSpan( global::System.TimeSpan value);
		#endif
		#if false || false || false || false || false
		uint WriteString( string value);
		#endif
		#if false || false || false || false || false
		uint MeasureString( string value);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.DataWriterStoreOperation StoreAsync();
		#endif
		#if false || false || false || false || false
		global::Windows.Foundation.IAsyncOperation<bool> FlushAsync();
		#endif
		#if false || false || false || false || false
		global::Windows.Storage.Streams.IBuffer DetachBuffer();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.IOutputStream DetachStream();
		#endif
	}
}
