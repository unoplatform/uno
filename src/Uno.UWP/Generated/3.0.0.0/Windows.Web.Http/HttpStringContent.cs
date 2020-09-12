#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HttpStringContent : global::Windows.Web.Http.IHttpContent,global::System.IDisposable,global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.Headers.HttpContentHeaderCollection Headers
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpContentHeaderCollection HttpStringContent.Headers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HttpStringContent( string content) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpStringContent", "HttpStringContent.HttpStringContent(string content)");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.HttpStringContent.HttpStringContent(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HttpStringContent( string content,  global::Windows.Storage.Streams.UnicodeEncoding encoding) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpStringContent", "HttpStringContent.HttpStringContent(string content, UnicodeEncoding encoding)");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.HttpStringContent.HttpStringContent(string, Windows.Storage.Streams.UnicodeEncoding)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HttpStringContent( string content,  global::Windows.Storage.Streams.UnicodeEncoding encoding,  string mediaType) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpStringContent", "HttpStringContent.HttpStringContent(string content, UnicodeEncoding encoding, string mediaType)");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.HttpStringContent.HttpStringContent(string, Windows.Storage.Streams.UnicodeEncoding, string)
		// Forced skipping of method Windows.Web.Http.HttpStringContent.Headers.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<ulong, ulong> BufferAllAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<ulong, ulong> HttpStringContent.BufferAllAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Storage.Streams.IBuffer, ulong> ReadAsBufferAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<IBuffer, ulong> HttpStringContent.ReadAsBufferAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Storage.Streams.IInputStream, ulong> ReadAsInputStreamAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<IInputStream, ulong> HttpStringContent.ReadAsInputStreamAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<string, ulong> ReadAsStringAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<string, ulong> HttpStringContent.ReadAsStringAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool TryComputeLength(out ulong length)
		{
			throw new global::System.NotImplementedException("The member bool HttpStringContent.TryComputeLength(out ulong length) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<ulong, ulong> WriteToStreamAsync( global::Windows.Storage.Streams.IOutputStream outputStream)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<ulong, ulong> HttpStringContent.WriteToStreamAsync(IOutputStream outputStream) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpStringContent", "void HttpStringContent.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string HttpStringContent.ToString() is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Web.Http.IHttpContent
		// Processing: System.IDisposable
	}
}
