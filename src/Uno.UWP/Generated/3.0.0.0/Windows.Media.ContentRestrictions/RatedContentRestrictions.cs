#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.ContentRestrictions
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RatedContentRestrictions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RatedContentRestrictions( uint maxAgeRating) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.ContentRestrictions.RatedContentRestrictions", "RatedContentRestrictions.RatedContentRestrictions(uint maxAgeRating)");
		}
		#endif
		// Forced skipping of method Windows.Media.ContentRestrictions.RatedContentRestrictions.RatedContentRestrictions(uint)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RatedContentRestrictions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.ContentRestrictions.RatedContentRestrictions", "RatedContentRestrictions.RatedContentRestrictions()");
		}
		#endif
		// Forced skipping of method Windows.Media.ContentRestrictions.RatedContentRestrictions.RatedContentRestrictions()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.ContentRestrictions.ContentRestrictionsBrowsePolicy> GetBrowsePolicyAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ContentRestrictionsBrowsePolicy> RatedContentRestrictions.GetBrowsePolicyAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.ContentRestrictions.ContentAccessRestrictionLevel> GetRestrictionLevelAsync( global::Windows.Media.ContentRestrictions.RatedContentDescription RatedContentDescription)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ContentAccessRestrictionLevel> RatedContentRestrictions.GetRestrictionLevelAsync(RatedContentDescription RatedContentDescription) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestContentAccessAsync( global::Windows.Media.ContentRestrictions.RatedContentDescription RatedContentDescription)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> RatedContentRestrictions.RequestContentAccessAsync(RatedContentDescription RatedContentDescription) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.ContentRestrictions.RatedContentRestrictions.RestrictionsChanged.add
		// Forced skipping of method Windows.Media.ContentRestrictions.RatedContentRestrictions.RestrictionsChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::System.EventHandler<object> RestrictionsChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.ContentRestrictions.RatedContentRestrictions", "event EventHandler<object> RatedContentRestrictions.RestrictionsChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.ContentRestrictions.RatedContentRestrictions", "event EventHandler<object> RatedContentRestrictions.RestrictionsChanged");
			}
		}
		#endif
	}
}
