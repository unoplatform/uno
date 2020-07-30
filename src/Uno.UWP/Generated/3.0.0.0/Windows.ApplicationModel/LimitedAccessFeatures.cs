#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LimitedAccessFeatures 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.LimitedAccessFeatureRequestResult TryUnlockFeature( string featureId,  string token,  string attestation)
		{
			throw new global::System.NotImplementedException("The member LimitedAccessFeatureRequestResult LimitedAccessFeatures.TryUnlockFeature(string featureId, string token, string attestation) is not implemented in Uno.");
		}
		#endif
	}
}
