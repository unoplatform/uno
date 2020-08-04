#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IPlayReadyLicenseAcquisitionServiceRequest : global::Windows.Media.Protection.PlayReady.IPlayReadyServiceRequest,global::Windows.Media.Protection.IMediaProtectionServiceRequest
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Protection.PlayReady.PlayReadyContentHeader ContentHeader
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Guid DomainServiceId
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyLicenseAcquisitionServiceRequest.ContentHeader.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyLicenseAcquisitionServiceRequest.ContentHeader.set
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyLicenseAcquisitionServiceRequest.DomainServiceId.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyLicenseAcquisitionServiceRequest.DomainServiceId.set
	}
}
