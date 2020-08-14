#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NDLicenseFetchDescriptor : global::Windows.Media.Protection.PlayReady.INDLicenseFetchDescriptor
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Protection.PlayReady.INDCustomData LicenseFetchChallengeCustomData
		{
			get
			{
				throw new global::System.NotImplementedException("The member INDCustomData NDLicenseFetchDescriptor.LicenseFetchChallengeCustomData is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDLicenseFetchDescriptor", "INDCustomData NDLicenseFetchDescriptor.LicenseFetchChallengeCustomData");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte[] ContentID
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte[] NDLicenseFetchDescriptor.ContentID is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Protection.PlayReady.NDContentIDType ContentIDType
		{
			get
			{
				throw new global::System.NotImplementedException("The member NDContentIDType NDLicenseFetchDescriptor.ContentIDType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public NDLicenseFetchDescriptor( global::Windows.Media.Protection.PlayReady.NDContentIDType contentIDType,  byte[] contentIDBytes,  global::Windows.Media.Protection.PlayReady.INDCustomData licenseFetchChallengeCustomData) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDLicenseFetchDescriptor", "NDLicenseFetchDescriptor.NDLicenseFetchDescriptor(NDContentIDType contentIDType, byte[] contentIDBytes, INDCustomData licenseFetchChallengeCustomData)");
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDLicenseFetchDescriptor.NDLicenseFetchDescriptor(Windows.Media.Protection.PlayReady.NDContentIDType, byte[], Windows.Media.Protection.PlayReady.INDCustomData)
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDLicenseFetchDescriptor.ContentIDType.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDLicenseFetchDescriptor.ContentID.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDLicenseFetchDescriptor.LicenseFetchChallengeCustomData.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDLicenseFetchDescriptor.LicenseFetchChallengeCustomData.set
		// Processing: Windows.Media.Protection.PlayReady.INDLicenseFetchDescriptor
	}
}
