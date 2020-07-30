#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INDLicenseFetchDescriptor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		byte[] ContentID
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Protection.PlayReady.NDContentIDType ContentIDType
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Protection.PlayReady.INDCustomData LicenseFetchChallengeCustomData
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDLicenseFetchDescriptor.ContentIDType.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDLicenseFetchDescriptor.ContentID.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDLicenseFetchDescriptor.LicenseFetchChallengeCustomData.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDLicenseFetchDescriptor.LicenseFetchChallengeCustomData.set
	}
}
