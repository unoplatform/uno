#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http.Headers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HttpChallengeHeaderValue : global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Web.Http.Headers.HttpNameValueHeaderValue> Parameters
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<HttpNameValueHeaderValue> HttpChallengeHeaderValue.Parameters is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Scheme
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HttpChallengeHeaderValue.Scheme is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Token
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HttpChallengeHeaderValue.Token is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HttpChallengeHeaderValue( string scheme) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.Headers.HttpChallengeHeaderValue", "HttpChallengeHeaderValue.HttpChallengeHeaderValue(string scheme)");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.Headers.HttpChallengeHeaderValue.HttpChallengeHeaderValue(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HttpChallengeHeaderValue( string scheme,  string token) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.Headers.HttpChallengeHeaderValue", "HttpChallengeHeaderValue.HttpChallengeHeaderValue(string scheme, string token)");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.Headers.HttpChallengeHeaderValue.HttpChallengeHeaderValue(string, string)
		// Forced skipping of method Windows.Web.Http.Headers.HttpChallengeHeaderValue.Parameters.get
		// Forced skipping of method Windows.Web.Http.Headers.HttpChallengeHeaderValue.Scheme.get
		// Forced skipping of method Windows.Web.Http.Headers.HttpChallengeHeaderValue.Token.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string HttpChallengeHeaderValue.ToString() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Web.Http.Headers.HttpChallengeHeaderValue Parse( string input)
		{
			throw new global::System.NotImplementedException("The member HttpChallengeHeaderValue HttpChallengeHeaderValue.Parse(string input) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool TryParse( string input, out global::Windows.Web.Http.Headers.HttpChallengeHeaderValue challengeHeaderValue)
		{
			throw new global::System.NotImplementedException("The member bool HttpChallengeHeaderValue.TryParse(string input, out HttpChallengeHeaderValue challengeHeaderValue) is not implemented in Uno.");
		}
		#endif
	}
}
