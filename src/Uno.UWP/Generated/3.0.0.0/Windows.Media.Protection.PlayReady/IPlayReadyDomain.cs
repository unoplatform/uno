#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IPlayReadyDomain 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Guid AccountId
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Uri DomainJoinUrl
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string FriendlyName
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint Revision
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Guid ServiceId
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyDomain.AccountId.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyDomain.ServiceId.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyDomain.Revision.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyDomain.FriendlyName.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyDomain.DomainJoinUrl.get
	}
}
