#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http.Filters
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HttpCacheControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.Filters.HttpCacheWriteBehavior WriteBehavior
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpCacheWriteBehavior HttpCacheControl.WriteBehavior is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.Filters.HttpCacheControl", "HttpCacheWriteBehavior HttpCacheControl.WriteBehavior");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.Http.Filters.HttpCacheReadBehavior ReadBehavior
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpCacheReadBehavior HttpCacheControl.ReadBehavior is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.Filters.HttpCacheControl", "HttpCacheReadBehavior HttpCacheControl.ReadBehavior");
			}
		}
		#endif
		// Forced skipping of method Windows.Web.Http.Filters.HttpCacheControl.ReadBehavior.get
		// Forced skipping of method Windows.Web.Http.Filters.HttpCacheControl.ReadBehavior.set
		// Forced skipping of method Windows.Web.Http.Filters.HttpCacheControl.WriteBehavior.get
		// Forced skipping of method Windows.Web.Http.Filters.HttpCacheControl.WriteBehavior.set
	}
}
