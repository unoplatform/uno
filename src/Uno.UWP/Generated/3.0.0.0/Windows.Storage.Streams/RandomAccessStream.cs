#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RandomAccessStream 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<ulong, ulong> CopyAsync( global::Windows.Storage.Streams.IInputStream source,  global::Windows.Storage.Streams.IOutputStream destination)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<ulong, ulong> RandomAccessStream.CopyAsync(IInputStream source, IOutputStream destination) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<ulong, ulong> CopyAsync( global::Windows.Storage.Streams.IInputStream source,  global::Windows.Storage.Streams.IOutputStream destination,  ulong bytesToCopy)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<ulong, ulong> RandomAccessStream.CopyAsync(IInputStream source, IOutputStream destination, ulong bytesToCopy) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<ulong, ulong> CopyAndCloseAsync( global::Windows.Storage.Streams.IInputStream source,  global::Windows.Storage.Streams.IOutputStream destination)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<ulong, ulong> RandomAccessStream.CopyAndCloseAsync(IInputStream source, IOutputStream destination) is not implemented in Uno.");
		}
		#endif
	}
}
