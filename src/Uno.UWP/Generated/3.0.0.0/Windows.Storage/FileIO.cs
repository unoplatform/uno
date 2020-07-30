#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FileIO 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> ReadTextAsync( global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> FileIO.ReadTextAsync(IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> ReadTextAsync( global::Windows.Storage.IStorageFile file,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> FileIO.ReadTextAsync(IStorageFile file, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteTextAsync( global::Windows.Storage.IStorageFile file,  string contents)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction FileIO.WriteTextAsync(IStorageFile file, string contents) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteTextAsync( global::Windows.Storage.IStorageFile file,  string contents,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction FileIO.WriteTextAsync(IStorageFile file, string contents, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction AppendTextAsync( global::Windows.Storage.IStorageFile file,  string contents)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction FileIO.AppendTextAsync(IStorageFile file, string contents) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction AppendTextAsync( global::Windows.Storage.IStorageFile file,  string contents,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction FileIO.AppendTextAsync(IStorageFile file, string contents, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<string>> ReadLinesAsync( global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<string>> FileIO.ReadLinesAsync(IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<string>> ReadLinesAsync( global::Windows.Storage.IStorageFile file,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<string>> FileIO.ReadLinesAsync(IStorageFile file, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteLinesAsync( global::Windows.Storage.IStorageFile file,  global::System.Collections.Generic.IEnumerable<string> lines)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction FileIO.WriteLinesAsync(IStorageFile file, IEnumerable<string> lines) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteLinesAsync( global::Windows.Storage.IStorageFile file,  global::System.Collections.Generic.IEnumerable<string> lines,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction FileIO.WriteLinesAsync(IStorageFile file, IEnumerable<string> lines, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction AppendLinesAsync( global::Windows.Storage.IStorageFile file,  global::System.Collections.Generic.IEnumerable<string> lines)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction FileIO.AppendLinesAsync(IStorageFile file, IEnumerable<string> lines) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction AppendLinesAsync( global::Windows.Storage.IStorageFile file,  global::System.Collections.Generic.IEnumerable<string> lines,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction FileIO.AppendLinesAsync(IStorageFile file, IEnumerable<string> lines, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IBuffer> ReadBufferAsync( global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IBuffer> FileIO.ReadBufferAsync(IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteBufferAsync( global::Windows.Storage.IStorageFile file,  global::Windows.Storage.Streams.IBuffer buffer)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction FileIO.WriteBufferAsync(IStorageFile file, IBuffer buffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteBytesAsync( global::Windows.Storage.IStorageFile file,  byte[] buffer)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction FileIO.WriteBytesAsync(IStorageFile file, byte[] buffer) is not implemented in Uno.");
		}
		#endif
	}
}
