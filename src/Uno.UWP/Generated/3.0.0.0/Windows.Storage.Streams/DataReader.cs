#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataReader : global::Windows.Storage.Streams.IDataReader,global::System.IDisposable
	{
		// Skipping already declared property UnicodeEncoding
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.InputStreamOptions InputStreamOptions
		{
			get
			{
				throw new global::System.NotImplementedException("The member InputStreamOptions DataReader.InputStreamOptions is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataReader", "InputStreamOptions DataReader.InputStreamOptions");
			}
		}
		#endif
		// Skipping already declared property ByteOrder
		// Skipping already declared property UnconsumedBufferLength
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public DataReader( global::Windows.Storage.Streams.IInputStream inputStream) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataReader", "DataReader.DataReader(IInputStream inputStream)");
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.DataReader.DataReader(Windows.Storage.Streams.IInputStream)
		// Forced skipping of method Windows.Storage.Streams.DataReader.UnconsumedBufferLength.get
		// Forced skipping of method Windows.Storage.Streams.DataReader.UnicodeEncoding.get
		// Forced skipping of method Windows.Storage.Streams.DataReader.UnicodeEncoding.set
		// Forced skipping of method Windows.Storage.Streams.DataReader.ByteOrder.get
		// Forced skipping of method Windows.Storage.Streams.DataReader.ByteOrder.set
		// Forced skipping of method Windows.Storage.Streams.DataReader.InputStreamOptions.get
		// Forced skipping of method Windows.Storage.Streams.DataReader.InputStreamOptions.set
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadByte()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadBytes(byte[])
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadBuffer(uint)
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadBoolean()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadGuid()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadInt16()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadInt32()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadInt64()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadUInt16()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadUInt32()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadUInt64()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadSingle()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadDouble()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadString(uint)
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadDateTime()
		// Skipping already declared method Windows.Storage.Streams.DataReader.ReadTimeSpan()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.DataReaderLoadOperation LoadAsync( uint count)
		{
			throw new global::System.NotImplementedException("The member DataReaderLoadOperation DataReader.LoadAsync(uint count) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Storage.Streams.DataReader.DetachBuffer()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream DetachStream()
		{
			throw new global::System.NotImplementedException("The member IInputStream DataReader.DetachStream() is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Storage.Streams.DataReader.Dispose()
		// Skipping already declared method Windows.Storage.Streams.DataReader.FromBuffer(Windows.Storage.Streams.IBuffer)
		// Processing: Windows.Storage.Streams.IDataReader
		// Processing: System.IDisposable
	}
}
