#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INDRegistrationCompletedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Protection.PlayReady.INDCustomData ResponseCustomData
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool TransmitterCertificateAccepted
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Protection.PlayReady.INDTransmitterProperties TransmitterProperties
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDRegistrationCompletedEventArgs.ResponseCustomData.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDRegistrationCompletedEventArgs.TransmitterProperties.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDRegistrationCompletedEventArgs.TransmitterCertificateAccepted.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.INDRegistrationCompletedEventArgs.TransmitterCertificateAccepted.set
	}
}
