#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2HttpResponseHeaders : global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, string>>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AppendHeader( string name,  string value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2HttpResponseHeaders", "void CoreWebView2HttpResponseHeaders.AppendHeader(string name, string value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Contains( string name)
		{
			throw new global::System.NotImplementedException("The member bool CoreWebView2HttpResponseHeaders.Contains(string name) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreWebView2HttpResponseHeaders.Contains%28string%20name%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetHeader( string name)
		{
			throw new global::System.NotImplementedException("The member string CoreWebView2HttpResponseHeaders.GetHeader(string name) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2HttpResponseHeaders.GetHeader%28string%20name%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2HttpHeadersCollectionIterator GetHeaders( string name)
		{
			throw new global::System.NotImplementedException("The member CoreWebView2HttpHeadersCollectionIterator CoreWebView2HttpResponseHeaders.GetHeaders(string name) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2HttpHeadersCollectionIterator%20CoreWebView2HttpResponseHeaders.GetHeaders%28string%20name%29");
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2HttpResponseHeaders.First()
		// Processing: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<string, string>> GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.IEnumerable
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.IEnumerable
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		 global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
	}
}
