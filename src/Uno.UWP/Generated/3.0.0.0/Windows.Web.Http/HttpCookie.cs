#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HttpCookie : global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HttpCookie.Value is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpCookie", "string HttpCookie.Value");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Secure
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HttpCookie.Secure is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpCookie", "bool HttpCookie.Secure");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HttpOnly
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HttpCookie.HttpOnly is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpCookie", "bool HttpCookie.HttpOnly");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset? Expires
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset? HttpCookie.Expires is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpCookie", "DateTimeOffset? HttpCookie.Expires");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Domain
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HttpCookie.Domain is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HttpCookie.Name is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Path
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HttpCookie.Path is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HttpCookie( string name,  string domain,  string path) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpCookie", "HttpCookie.HttpCookie(string name, string domain, string path)");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.HttpCookie.HttpCookie(string, string, string)
		// Forced skipping of method Windows.Web.Http.HttpCookie.Name.get
		// Forced skipping of method Windows.Web.Http.HttpCookie.Domain.get
		// Forced skipping of method Windows.Web.Http.HttpCookie.Path.get
		// Forced skipping of method Windows.Web.Http.HttpCookie.Expires.get
		// Forced skipping of method Windows.Web.Http.HttpCookie.Expires.set
		// Forced skipping of method Windows.Web.Http.HttpCookie.HttpOnly.get
		// Forced skipping of method Windows.Web.Http.HttpCookie.HttpOnly.set
		// Forced skipping of method Windows.Web.Http.HttpCookie.Secure.get
		// Forced skipping of method Windows.Web.Http.HttpCookie.Secure.set
		// Forced skipping of method Windows.Web.Http.HttpCookie.Value.get
		// Forced skipping of method Windows.Web.Http.HttpCookie.Value.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string HttpCookie.ToString() is not implemented in Uno.");
		}
		#endif
	}
}
