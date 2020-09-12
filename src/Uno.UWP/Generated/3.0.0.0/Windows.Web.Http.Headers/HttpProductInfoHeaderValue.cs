#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http.Headers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HttpProductInfoHeaderValue : global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Comment
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HttpProductInfoHeaderValue.Comment is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.Headers.HttpProductHeaderValue Product
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpProductHeaderValue HttpProductInfoHeaderValue.Product is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HttpProductInfoHeaderValue( string productComment) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.Headers.HttpProductInfoHeaderValue", "HttpProductInfoHeaderValue.HttpProductInfoHeaderValue(string productComment)");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.Headers.HttpProductInfoHeaderValue.HttpProductInfoHeaderValue(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HttpProductInfoHeaderValue( string productName,  string productVersion) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.Headers.HttpProductInfoHeaderValue", "HttpProductInfoHeaderValue.HttpProductInfoHeaderValue(string productName, string productVersion)");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.Headers.HttpProductInfoHeaderValue.HttpProductInfoHeaderValue(string, string)
		// Forced skipping of method Windows.Web.Http.Headers.HttpProductInfoHeaderValue.Product.get
		// Forced skipping of method Windows.Web.Http.Headers.HttpProductInfoHeaderValue.Comment.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string HttpProductInfoHeaderValue.ToString() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Web.Http.Headers.HttpProductInfoHeaderValue Parse( string input)
		{
			throw new global::System.NotImplementedException("The member HttpProductInfoHeaderValue HttpProductInfoHeaderValue.Parse(string input) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool TryParse( string input, out global::Windows.Web.Http.Headers.HttpProductInfoHeaderValue productInfoHeaderValue)
		{
			throw new global::System.NotImplementedException("The member bool HttpProductInfoHeaderValue.TryParse(string input, out HttpProductInfoHeaderValue productInfoHeaderValue) is not implemented in Uno.");
		}
		#endif
	}
}
