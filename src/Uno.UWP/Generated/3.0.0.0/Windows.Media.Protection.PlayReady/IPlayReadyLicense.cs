#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IPlayReadyLicense 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint ChainDepth
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::System.Guid DomainAccountID
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::System.DateTimeOffset? ExpirationDate
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		uint ExpireAfterFirstPlay
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool FullyEvaluated
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool UsableForPlay
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyLicense.FullyEvaluated.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyLicense.UsableForPlay.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyLicense.ExpirationDate.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyLicense.ExpireAfterFirstPlay.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyLicense.DomainAccountID.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyLicense.ChainDepth.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::System.Guid GetKIDAtChainDepth( uint chainDepth);
		#endif
	}
}
