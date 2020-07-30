#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataWriter : global::Windows.Storage.Streams.IDataWriter,global::System.IDisposable
	{
		// Skipping already declared property UnicodeEncoding
		// Skipping already declared property ByteOrder
		// Skipping already declared property UnstoredBufferLength
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public DataWriter( global::Windows.Storage.Streams.IOutputStream outputStream) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriter", "DataWriter.DataWriter(IOutputStream outputStream)");
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.DataWriter.DataWriter(Windows.Storage.Streams.IOutputStream)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.DataWriter()
		// Forced skipping of method Windows.Storage.Streams.DataWriter.DataWriter()
		// Forced skipping of method Windows.Storage.Streams.DataWriter.UnstoredBufferLength.get
		// Forced skipping of method Windows.Storage.Streams.DataWriter.UnicodeEncoding.get
		// Forced skipping of method Windows.Storage.Streams.DataWriter.UnicodeEncoding.set
		// Forced skipping of method Windows.Storage.Streams.DataWriter.ByteOrder.get
		// Forced skipping of method Windows.Storage.Streams.DataWriter.ByteOrder.set
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteByte(byte)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteBytes(byte[])
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteBuffer(Windows.Storage.Streams.IBuffer)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteBuffer(Windows.Storage.Streams.IBuffer, uint, uint)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteBoolean(bool)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteGuid(System.Guid)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteInt16(short)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteInt32(int)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteInt64(long)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteUInt16(ushort)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteUInt32(uint)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteUInt64(ulong)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteSingle(float)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteDouble(double)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteDateTime(System.DateTimeOffset)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteTimeSpan(System.TimeSpan)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.WriteString(string)
		// Skipping already declared method Windows.Storage.Streams.DataWriter.MeasureString(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.DataWriterStoreOperation StoreAsync()
		{
			throw new global::System.NotImplementedException("The member DataWriterStoreOperation DataWriter.StoreAsync() is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Storage.Streams.DataWriter.FlushAsync()
		// Skipping already declared method Windows.Storage.Streams.DataWriter.DetachBuffer()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream DetachStream()
		{
			throw new global::System.NotImplementedException("The member IOutputStream DataWriter.DetachStream() is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Storage.Streams.DataWriter.Dispose()
		// Processing: Windows.Storage.Streams.IDataWriter
		// Processing: System.IDisposable
	}
}
