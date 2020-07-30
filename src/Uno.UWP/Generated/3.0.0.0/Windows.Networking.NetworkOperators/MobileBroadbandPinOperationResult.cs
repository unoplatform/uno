#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandPinOperationResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint AttemptsRemaining
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint MobileBroadbandPinOperationResult.AttemptsRemaining is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSuccessful
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MobileBroadbandPinOperationResult.IsSuccessful is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandPinOperationResult.IsSuccessful.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandPinOperationResult.AttemptsRemaining.get
	}
}
