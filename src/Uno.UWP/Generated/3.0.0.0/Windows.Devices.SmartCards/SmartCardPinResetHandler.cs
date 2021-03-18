#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.SmartCards
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	public delegate void SmartCardPinResetHandler(global::Windows.Devices.SmartCards.SmartCardProvisioning @sender, global::Windows.Devices.SmartCards.SmartCardPinResetRequest @request);
	#endif
}
