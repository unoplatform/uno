#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IRandomAccessStream : global::System.IDisposable,global::Windows.Storage.Streams.IInputStream,global::Windows.Storage.Streams.IOutputStream
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool CanRead
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool CanWrite
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		ulong Position
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		ulong Size
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.IRandomAccessStream.Size.get
		// Forced skipping of method Windows.Storage.Streams.IRandomAccessStream.Size.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.IInputStream GetInputStreamAt( ulong position);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.IOutputStream GetOutputStreamAt( ulong position);
		#endif
		// Forced skipping of method Windows.Storage.Streams.IRandomAccessStream.Position.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Seek( ulong position);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.IRandomAccessStream CloneStream();
		#endif
		// Forced skipping of method Windows.Storage.Streams.IRandomAccessStream.CanRead.get
		// Forced skipping of method Windows.Storage.Streams.IRandomAccessStream.CanWrite.get
	}
}
