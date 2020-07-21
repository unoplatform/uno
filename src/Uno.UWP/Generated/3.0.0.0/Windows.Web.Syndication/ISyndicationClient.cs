#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Syndication
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ISyndicationClient 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool BypassCacheOnRetrieve
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint MaxResponseBufferSize
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Security.Credentials.PasswordCredential ProxyCredential
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Security.Credentials.PasswordCredential ServerCredential
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint Timeout
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Web.Syndication.ISyndicationClient.ServerCredential.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationClient.ServerCredential.set
		// Forced skipping of method Windows.Web.Syndication.ISyndicationClient.ProxyCredential.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationClient.ProxyCredential.set
		// Forced skipping of method Windows.Web.Syndication.ISyndicationClient.MaxResponseBufferSize.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationClient.MaxResponseBufferSize.set
		// Forced skipping of method Windows.Web.Syndication.ISyndicationClient.Timeout.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationClient.Timeout.set
		// Forced skipping of method Windows.Web.Syndication.ISyndicationClient.BypassCacheOnRetrieve.get
		// Forced skipping of method Windows.Web.Syndication.ISyndicationClient.BypassCacheOnRetrieve.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetRequestHeader( string name,  string value);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Web.Syndication.SyndicationFeed, global::Windows.Web.Syndication.RetrievalProgress> RetrieveFeedAsync( global::System.Uri uri);
		#endif
	}
}
