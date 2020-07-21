#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PathIO 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> ReadTextAsync( string absolutePath)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> PathIO.ReadTextAsync(string absolutePath) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> ReadTextAsync( string absolutePath,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> PathIO.ReadTextAsync(string absolutePath, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteTextAsync( string absolutePath,  string contents)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PathIO.WriteTextAsync(string absolutePath, string contents) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteTextAsync( string absolutePath,  string contents,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PathIO.WriteTextAsync(string absolutePath, string contents, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction AppendTextAsync( string absolutePath,  string contents)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PathIO.AppendTextAsync(string absolutePath, string contents) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction AppendTextAsync( string absolutePath,  string contents,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PathIO.AppendTextAsync(string absolutePath, string contents, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<string>> ReadLinesAsync( string absolutePath)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<string>> PathIO.ReadLinesAsync(string absolutePath) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<string>> ReadLinesAsync( string absolutePath,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<string>> PathIO.ReadLinesAsync(string absolutePath, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteLinesAsync( string absolutePath,  global::System.Collections.Generic.IEnumerable<string> lines)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PathIO.WriteLinesAsync(string absolutePath, IEnumerable<string> lines) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteLinesAsync( string absolutePath,  global::System.Collections.Generic.IEnumerable<string> lines,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PathIO.WriteLinesAsync(string absolutePath, IEnumerable<string> lines, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction AppendLinesAsync( string absolutePath,  global::System.Collections.Generic.IEnumerable<string> lines)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PathIO.AppendLinesAsync(string absolutePath, IEnumerable<string> lines) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction AppendLinesAsync( string absolutePath,  global::System.Collections.Generic.IEnumerable<string> lines,  global::Windows.Storage.Streams.UnicodeEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PathIO.AppendLinesAsync(string absolutePath, IEnumerable<string> lines, UnicodeEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IBuffer> ReadBufferAsync( string absolutePath)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IBuffer> PathIO.ReadBufferAsync(string absolutePath) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteBufferAsync( string absolutePath,  global::Windows.Storage.Streams.IBuffer buffer)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PathIO.WriteBufferAsync(string absolutePath, IBuffer buffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction WriteBytesAsync( string absolutePath,  byte[] buffer)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PathIO.WriteBytesAsync(string absolutePath, byte[] buffer) is not implemented in Uno.");
		}
		#endif
	}
}
