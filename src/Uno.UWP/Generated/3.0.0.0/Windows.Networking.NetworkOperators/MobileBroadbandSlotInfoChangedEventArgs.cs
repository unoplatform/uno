#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandSlotInfoChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.MobileBroadbandSlotInfo SlotInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member MobileBroadbandSlotInfo MobileBroadbandSlotInfoChangedEventArgs.SlotInfo is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MobileBroadbandSlotInfo%20MobileBroadbandSlotInfoChangedEventArgs.SlotInfo");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandSlotInfoChangedEventArgs.SlotInfo.get
	}
}
