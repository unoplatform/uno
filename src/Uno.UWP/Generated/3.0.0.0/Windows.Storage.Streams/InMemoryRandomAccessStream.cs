#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InMemoryRandomAccessStream : global::Windows.Storage.Streams.IRandomAccessStream,global::Windows.Storage.Streams.IOutputStream,global::System.IDisposable,global::Windows.Storage.Streams.IInputStream
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong InMemoryRandomAccessStream.Size is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.InMemoryRandomAccessStream", "ulong InMemoryRandomAccessStream.Size");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanRead
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InMemoryRandomAccessStream.CanRead is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanWrite
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InMemoryRandomAccessStream.CanWrite is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong InMemoryRandomAccessStream.Position is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public InMemoryRandomAccessStream() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.InMemoryRandomAccessStream", "InMemoryRandomAccessStream.InMemoryRandomAccessStream()");
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.InMemoryRandomAccessStream.InMemoryRandomAccessStream()
		// Forced skipping of method Windows.Storage.Streams.InMemoryRandomAccessStream.Size.get
		// Forced skipping of method Windows.Storage.Streams.InMemoryRandomAccessStream.Size.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream GetInputStreamAt( ulong position)
		{
			throw new global::System.NotImplementedException("The member IInputStream InMemoryRandomAccessStream.GetInputStreamAt(ulong position) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream GetOutputStreamAt( ulong position)
		{
			throw new global::System.NotImplementedException("The member IOutputStream InMemoryRandomAccessStream.GetOutputStreamAt(ulong position) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.InMemoryRandomAccessStream.Position.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Seek( ulong position)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.InMemoryRandomAccessStream", "void InMemoryRandomAccessStream.Seek(ulong position)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStream CloneStream()
		{
			throw new global::System.NotImplementedException("The member IRandomAccessStream InMemoryRandomAccessStream.CloneStream() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.InMemoryRandomAccessStream.CanRead.get
		// Forced skipping of method Windows.Storage.Streams.InMemoryRandomAccessStream.CanWrite.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.InMemoryRandomAccessStream", "void InMemoryRandomAccessStream.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Storage.Streams.IBuffer, uint> ReadAsync( global::Windows.Storage.Streams.IBuffer buffer,  uint count,  global::Windows.Storage.Streams.InputStreamOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<IBuffer, uint> InMemoryRandomAccessStream.ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync( global::Windows.Storage.Streams.IBuffer buffer)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<uint, uint> InMemoryRandomAccessStream.WriteAsync(IBuffer buffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> FlushAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> InMemoryRandomAccessStream.FlushAsync() is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Storage.Streams.IRandomAccessStream
		// Processing: System.IDisposable
		// Processing: Windows.Storage.Streams.IInputStream
		// Processing: Windows.Storage.Streams.IOutputStream
	}
}
