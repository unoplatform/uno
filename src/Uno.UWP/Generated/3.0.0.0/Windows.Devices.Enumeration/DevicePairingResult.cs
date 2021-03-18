#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DevicePairingResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DevicePairingProtectionLevel ProtectionLevelUsed
		{
			get
			{
				throw new global::System.NotImplementedException("The member DevicePairingProtectionLevel DevicePairingResult.ProtectionLevelUsed is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DevicePairingResultStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member DevicePairingResultStatus DevicePairingResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DevicePairingResult.Status.get
		// Forced skipping of method Windows.Devices.Enumeration.DevicePairingResult.ProtectionLevelUsed.get
	}
}
