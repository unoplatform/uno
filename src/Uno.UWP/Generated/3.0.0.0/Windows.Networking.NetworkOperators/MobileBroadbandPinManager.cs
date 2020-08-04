#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandPinManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.NetworkOperators.MobileBroadbandPinType> SupportedPins
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MobileBroadbandPinType> MobileBroadbandPinManager.SupportedPins is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandPinManager.SupportedPins.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.MobileBroadbandPin GetPin( global::Windows.Networking.NetworkOperators.MobileBroadbandPinType pinType)
		{
			throw new global::System.NotImplementedException("The member MobileBroadbandPin MobileBroadbandPinManager.GetPin(MobileBroadbandPinType pinType) is not implemented in Uno.");
		}
		#endif
	}
}
