#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IPlayReadyLicenseSession2 : global::Windows.Media.Protection.PlayReady.IPlayReadyLicenseSession
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Protection.PlayReady.PlayReadyLicenseIterable CreateLicenseIterable( global::Windows.Media.Protection.PlayReady.PlayReadyContentHeader contentHeader,  bool fullyEvaluated);
		#endif
	}
}
