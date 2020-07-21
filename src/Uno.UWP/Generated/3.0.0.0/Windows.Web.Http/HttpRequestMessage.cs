#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HttpRequestMessage : global::System.IDisposable,global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri RequestUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri HttpRequestMessage.RequestUri is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpRequestMessage", "Uri HttpRequestMessage.RequestUri");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.HttpMethod Method
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpMethod HttpRequestMessage.Method is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpRequestMessage", "HttpMethod HttpRequestMessage.Method");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.IHttpContent Content
		{
			get
			{
				throw new global::System.NotImplementedException("The member IHttpContent HttpRequestMessage.Content is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpRequestMessage", "IHttpContent HttpRequestMessage.Content");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.Headers.HttpRequestHeaderCollection Headers
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpRequestHeaderCollection HttpRequestMessage.Headers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<string, object> HttpRequestMessage.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.HttpTransportInformation TransportInformation
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpTransportInformation HttpRequestMessage.TransportInformation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HttpRequestMessage( global::Windows.Web.Http.HttpMethod method,  global::System.Uri uri) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpRequestMessage", "HttpRequestMessage.HttpRequestMessage(HttpMethod method, Uri uri)");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.HttpRequestMessage.HttpRequestMessage(Windows.Web.Http.HttpMethod, System.Uri)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HttpRequestMessage() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpRequestMessage", "HttpRequestMessage.HttpRequestMessage()");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.HttpRequestMessage.HttpRequestMessage()
		// Forced skipping of method Windows.Web.Http.HttpRequestMessage.Content.get
		// Forced skipping of method Windows.Web.Http.HttpRequestMessage.Content.set
		// Forced skipping of method Windows.Web.Http.HttpRequestMessage.Headers.get
		// Forced skipping of method Windows.Web.Http.HttpRequestMessage.Method.get
		// Forced skipping of method Windows.Web.Http.HttpRequestMessage.Method.set
		// Forced skipping of method Windows.Web.Http.HttpRequestMessage.Properties.get
		// Forced skipping of method Windows.Web.Http.HttpRequestMessage.RequestUri.get
		// Forced skipping of method Windows.Web.Http.HttpRequestMessage.RequestUri.set
		// Forced skipping of method Windows.Web.Http.HttpRequestMessage.TransportInformation.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpRequestMessage", "void HttpRequestMessage.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string HttpRequestMessage.ToString() is not implemented in Uno.");
		}
		#endif
		// Processing: System.IDisposable
	}
}
