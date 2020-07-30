#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INDTransmitterProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Protection.PlayReady.NDCertificateType CertificateType
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		byte[] ClientID
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.DateTimeOffset ExpirationDate
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		byte[] ModelDigest
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string ModelManufacturerName
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string ModelName
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string ModelNumber
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Protection.PlayReady.NDCertificatePlatformID PlatformIdentifier
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint SecurityLevel
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint SecurityVersion
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Protection.PlayReady.NDCertificateFeature[] SupportedFeatures
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDTransmitterProperties.CertificateType.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDTransmitterProperties.PlatformIdentifier.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDTransmitterProperties.SupportedFeatures.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDTransmitterProperties.SecurityLevel.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDTransmitterProperties.SecurityVersion.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDTransmitterProperties.ExpirationDate.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDTransmitterProperties.ClientID.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDTransmitterProperties.ModelDigest.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDTransmitterProperties.ModelManufacturerName.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDTransmitterProperties.ModelName.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDTransmitterProperties.ModelNumber.get
	}
}
