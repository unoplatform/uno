#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlayReadyLicenseSession : global::Windows.Media.Protection.PlayReady.IPlayReadyLicenseSession,global::Windows.Media.Protection.PlayReady.IPlayReadyLicenseSession2
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PlayReadyLicenseSession( global::Windows.Foundation.Collections.IPropertySet configuration) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.PlayReadyLicenseSession", "PlayReadyLicenseSession.PlayReadyLicenseSession(IPropertySet configuration)");
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.PlayReadyLicenseSession.PlayReadyLicenseSession(Windows.Foundation.Collections.IPropertySet)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Protection.PlayReady.IPlayReadyLicenseAcquisitionServiceRequest CreateLAServiceRequest()
		{
			throw new global::System.NotImplementedException("The member IPlayReadyLicenseAcquisitionServiceRequest PlayReadyLicenseSession.CreateLAServiceRequest() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ConfigureMediaProtectionManager( global::Windows.Media.Protection.MediaProtectionManager mpm)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.PlayReadyLicenseSession", "void PlayReadyLicenseSession.ConfigureMediaProtectionManager(MediaProtectionManager mpm)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Protection.PlayReady.PlayReadyLicenseIterable CreateLicenseIterable( global::Windows.Media.Protection.PlayReady.PlayReadyContentHeader contentHeader,  bool fullyEvaluated)
		{
			throw new global::System.NotImplementedException("The member PlayReadyLicenseIterable PlayReadyLicenseSession.CreateLicenseIterable(PlayReadyContentHeader contentHeader, bool fullyEvaluated) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Media.Protection.PlayReady.IPlayReadyLicenseSession
		// Processing: Windows.Media.Protection.PlayReady.IPlayReadyLicenseSession2
	}
}
