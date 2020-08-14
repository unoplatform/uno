#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http.Headers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HttpTransferCodingHeaderValue : global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Web.Http.Headers.HttpNameValueHeaderValue> Parameters
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<HttpNameValueHeaderValue> HttpTransferCodingHeaderValue.Parameters is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HttpTransferCodingHeaderValue.Value is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HttpTransferCodingHeaderValue( string input) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.Headers.HttpTransferCodingHeaderValue", "HttpTransferCodingHeaderValue.HttpTransferCodingHeaderValue(string input)");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.Headers.HttpTransferCodingHeaderValue.HttpTransferCodingHeaderValue(string)
		// Forced skipping of method Windows.Web.Http.Headers.HttpTransferCodingHeaderValue.Parameters.get
		// Forced skipping of method Windows.Web.Http.Headers.HttpTransferCodingHeaderValue.Value.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string HttpTransferCodingHeaderValue.ToString() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Web.Http.Headers.HttpTransferCodingHeaderValue Parse( string input)
		{
			throw new global::System.NotImplementedException("The member HttpTransferCodingHeaderValue HttpTransferCodingHeaderValue.Parse(string input) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool TryParse( string input, out global::Windows.Web.Http.Headers.HttpTransferCodingHeaderValue transferCodingHeaderValue)
		{
			throw new global::System.NotImplementedException("The member bool HttpTransferCodingHeaderValue.TryParse(string input, out HttpTransferCodingHeaderValue transferCodingHeaderValue) is not implemented in Uno.");
		}
		#endif
	}
}
