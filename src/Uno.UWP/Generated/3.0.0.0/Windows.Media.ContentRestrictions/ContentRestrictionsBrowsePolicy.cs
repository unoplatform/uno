#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.ContentRestrictions
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContentRestrictionsBrowsePolicy 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GeographicRegion
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContentRestrictionsBrowsePolicy.GeographicRegion is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint? MaxBrowsableAgeRating
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? ContentRestrictionsBrowsePolicy.MaxBrowsableAgeRating is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint? PreferredAgeRating
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? ContentRestrictionsBrowsePolicy.PreferredAgeRating is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.ContentRestrictions.ContentRestrictionsBrowsePolicy.GeographicRegion.get
		// Forced skipping of method Windows.Media.ContentRestrictions.ContentRestrictionsBrowsePolicy.MaxBrowsableAgeRating.get
		// Forced skipping of method Windows.Media.ContentRestrictions.ContentRestrictionsBrowsePolicy.PreferredAgeRating.get
	}
}
