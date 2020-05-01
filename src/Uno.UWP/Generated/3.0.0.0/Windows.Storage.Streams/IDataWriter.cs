#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IDataWriter 
	{
		#if false
		global::Windows.Storage.Streams.ByteOrder ByteOrder
		{
			get;
			set;
		}
		#endif
		#if false
		global::Windows.Storage.Streams.UnicodeEncoding UnicodeEncoding
		{
			get;
			set;
		}
		#endif
		#if false
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
		#if false
		void WriteByte( byte value);
		#endif
		#if false
		void WriteBytes( byte[] value);
		#endif
		#if false
		void WriteBuffer( global::Windows.Storage.Streams.IBuffer buffer);
		#endif
		#if false
		void WriteBuffer( global::Windows.Storage.Streams.IBuffer buffer,  uint start,  uint count);
		#endif
		#if false
		void WriteBoolean( bool value);
		#endif
		#if false
		void WriteGuid( global::System.Guid value);
		#endif
		#if false
		void WriteInt16( short value);
		#endif
		#if false
		void WriteInt32( int value);
		#endif
		#if false
		void WriteInt64( long value);
		#endif
		#if false
		void WriteUInt16( ushort value);
		#endif
		#if false
		void WriteUInt32( uint value);
		#endif
		#if false
		void WriteUInt64( ulong value);
		#endif
		#if false
		void WriteSingle( float value);
		#endif
		#if false
		void WriteDouble( double value);
		#endif
		#if false
		void WriteDateTime( global::System.DateTimeOffset value);
		#endif
		#if false
		void WriteTimeSpan( global::System.TimeSpan value);
		#endif
		#if false
		uint WriteString( string value);
		#endif
		#if false
		uint MeasureString( string value);
		#endif
		#if false
		global::Windows.Storage.Streams.DataWriterStoreOperation StoreAsync();
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<bool> FlushAsync();
		#endif
		#if false
		global::Windows.Storage.Streams.IBuffer DetachBuffer();
		#endif
		#if false
		global::Windows.Storage.Streams.IOutputStream DetachStream();
		#endif
	}
}
