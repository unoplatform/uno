#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IDataWriter 
	{
		// Skipping already declared property ByteOrder
		// Skipping already declared property UnicodeEncoding
		// Skipping already declared property UnstoredBufferLength
		// Forced skipping of method Windows.Storage.Streams.IDataWriter.UnstoredBufferLength.get
		// Forced skipping of method Windows.Storage.Streams.IDataWriter.UnicodeEncoding.get
		// Forced skipping of method Windows.Storage.Streams.IDataWriter.UnicodeEncoding.set
		// Forced skipping of method Windows.Storage.Streams.IDataWriter.ByteOrder.get
		// Forced skipping of method Windows.Storage.Streams.IDataWriter.ByteOrder.set
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteByte(byte)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteBytes(byte[])
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteBuffer(Windows.Storage.Streams.IBuffer)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteBuffer(Windows.Storage.Streams.IBuffer, uint, uint)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteBoolean(bool)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteGuid(System.Guid)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteInt16(short)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteInt32(int)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteInt64(long)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteUInt16(ushort)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteUInt32(uint)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteUInt64(ulong)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteSingle(float)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteDouble(double)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteDateTime(System.DateTimeOffset)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteTimeSpan(System.TimeSpan)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.WriteString(string)
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.MeasureString(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.DataWriterStoreOperation StoreAsync();
		#endif
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.FlushAsync()
		// Skipping already declared method Windows.Storage.Streams.IDataWriter.DetachBuffer()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.IOutputStream DetachStream();
		#endif
	}
}
