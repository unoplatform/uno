#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceInformationCustomPairing 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DevicePairingResult> PairAsync( global::Windows.Devices.Enumeration.DevicePairingKinds pairingKindsSupported)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DevicePairingResult> DeviceInformationCustomPairing.PairAsync(DevicePairingKinds pairingKindsSupported) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DevicePairingResult> PairAsync( global::Windows.Devices.Enumeration.DevicePairingKinds pairingKindsSupported,  global::Windows.Devices.Enumeration.DevicePairingProtectionLevel minProtectionLevel)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DevicePairingResult> DeviceInformationCustomPairing.PairAsync(DevicePairingKinds pairingKindsSupported, DevicePairingProtectionLevel minProtectionLevel) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DevicePairingResult> PairAsync( global::Windows.Devices.Enumeration.DevicePairingKinds pairingKindsSupported,  global::Windows.Devices.Enumeration.DevicePairingProtectionLevel minProtectionLevel,  global::Windows.Devices.Enumeration.IDevicePairingSettings devicePairingSettings)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DevicePairingResult> DeviceInformationCustomPairing.PairAsync(DevicePairingKinds pairingKindsSupported, DevicePairingProtectionLevel minProtectionLevel, IDevicePairingSettings devicePairingSettings) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformationCustomPairing.PairingRequested.add
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformationCustomPairing.PairingRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Enumeration.DeviceInformationCustomPairing, global::Windows.Devices.Enumeration.DevicePairingRequestedEventArgs> PairingRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DeviceInformationCustomPairing", "event TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs> DeviceInformationCustomPairing.PairingRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DeviceInformationCustomPairing", "event TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs> DeviceInformationCustomPairing.PairingRequested");
			}
		}
		#endif
	}
}
