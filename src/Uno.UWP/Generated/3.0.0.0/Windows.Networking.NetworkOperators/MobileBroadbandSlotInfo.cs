#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandSlotInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Index
		{
			get
			{
				throw new global::System.NotImplementedException("The member int MobileBroadbandSlotInfo.Index is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20MobileBroadbandSlotInfo.Index");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.MobileBroadbandSlotState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member MobileBroadbandSlotState MobileBroadbandSlotInfo.State is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MobileBroadbandSlotState%20MobileBroadbandSlotInfo.State");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandSlotInfo.Index.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandSlotInfo.State.get
	}
}
