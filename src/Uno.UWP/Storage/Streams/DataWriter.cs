namespace Windows.Storage.Streams
{
	public  partial class DataWriter : global::Windows.Storage.Streams.IDataWriter,global::System.IDisposable
	{
		public  global::Windows.Storage.Streams.UnicodeEncoding UnicodeEncoding
		{
			get
			{
				throw new global::System.NotImplementedException("The member UnicodeEncoding DataWriter.UnicodeEncoding is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "UnicodeEncoding DataWriter.UnicodeEncoding");
			}
		}

		public  global::Windows.Storage.Streams.ByteOrder ByteOrder
		{
			get
			{
				throw new global::System.NotImplementedException("The member ByteOrder DataWriter.ByteOrder is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "ByteOrder DataWriter.ByteOrder");
			}
		}

		public  uint UnstoredBufferLength
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DataWriter.UnstoredBufferLength is not implemented in Uno.");
			}
		}

		public DataWriter( global::Windows.Storage.Streams.IOutputStream outputStream) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "DataWriter.DataWriter(IOutputStream outputStream)");
		}

		public DataWriter() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "DataWriter.DataWriter()");
		}

		public  void WriteByte( byte value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteByte(byte value)");
		}

		public  void WriteBytes( byte[] value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteBytes(byte[] value)");
		}

		public  void WriteBuffer( global::Windows.Storage.Streams.IBuffer buffer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteBuffer(IBuffer buffer)");
		}

		public  void WriteBuffer( global::Windows.Storage.Streams.IBuffer buffer,  uint start,  uint count)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteBuffer(IBuffer buffer, uint start, uint count)");
		}

		public  void WriteBoolean( bool value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteBoolean(bool value)");
		}

		public  void WriteGuid( global::System.Guid value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteGuid(Guid value)");
		}

		public  void WriteInt16( short value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteInt16(short value)");
		}

		public  void WriteInt32( int value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteInt32(int value)");
		}

		public  void WriteInt64( long value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteInt64(long value)");
		}

		public  void WriteUInt16( ushort value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteUInt16(ushort value)");
		}

		public  void WriteUInt32( uint value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteUInt32(uint value)");
		}

		public  void WriteUInt64( ulong value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteUInt64(ulong value)");
		}

		public  void WriteSingle( float value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteSingle(float value)");
		}

		public  void WriteDouble( double value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteDouble(double value)");
		}

		public  void WriteDateTime( global::System.DateTimeOffset value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteDateTime(DateTimeOffset value)");
		}

		public  void WriteTimeSpan( global::System.TimeSpan value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.WriteTimeSpan(TimeSpan value)");
		}

		public  uint WriteString( string value)
		{
			throw new global::System.NotImplementedException("The member uint DataWriter.WriteString(string value) is not implemented in Uno.");
		}

		public  uint MeasureString( string value)
		{
			throw new global::System.NotImplementedException("The member uint DataWriter.MeasureString(string value) is not implemented in Uno.");
		}

		public  global::Windows.Storage.Streams.DataWriterStoreOperation StoreAsync()
		{
			throw new global::System.NotImplementedException("The member DataWriterStoreOperation DataWriter.StoreAsync() is not implemented in Uno.");
		}
		public  global::Windows.Foundation.IAsyncOperation<bool> FlushAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> DataWriter.FlushAsync() is not implemented in Uno.");
		}

		public  global::Windows.Storage.Streams.IBuffer DetachBuffer()
		{
			throw new global::System.NotImplementedException("The member IBuffer DataWriter.DetachBuffer() is not implemented in Uno.");
		}

		public  global::Windows.Storage.Streams.IOutputStream DetachStream()
		{
			throw new global::System.NotImplementedException("The member IOutputStream DataWriter.DetachStream() is not implemented in Uno.");
		}

		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "void DataWriter.Dispose()");
		}
	}
}
