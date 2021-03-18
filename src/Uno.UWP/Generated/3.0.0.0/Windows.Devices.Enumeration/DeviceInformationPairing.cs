#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceInformationPairing 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanPair
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DeviceInformationPairing.CanPair is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPaired
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DeviceInformationPairing.IsPaired is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceInformationCustomPairing Custom
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformationCustomPairing DeviceInformationPairing.Custom is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DevicePairingProtectionLevel ProtectionLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member DevicePairingProtectionLevel DeviceInformationPairing.ProtectionLevel is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformationPairing.IsPaired.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformationPairing.CanPair.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DevicePairingResult> PairAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DevicePairingResult> DeviceInformationPairing.PairAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DevicePairingResult> PairAsync( global::Windows.Devices.Enumeration.DevicePairingProtectionLevel minProtectionLevel)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DevicePairingResult> DeviceInformationPairing.PairAsync(DevicePairingProtectionLevel minProtectionLevel) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformationPairing.ProtectionLevel.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformationPairing.Custom.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DevicePairingResult> PairAsync( global::Windows.Devices.Enumeration.DevicePairingProtectionLevel minProtectionLevel,  global::Windows.Devices.Enumeration.IDevicePairingSettings devicePairingSettings)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DevicePairingResult> DeviceInformationPairing.PairAsync(DevicePairingProtectionLevel minProtectionLevel, IDevicePairingSettings devicePairingSettings) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceUnpairingResult> UnpairAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceUnpairingResult> DeviceInformationPairing.UnpairAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool TryRegisterForAllInboundPairingRequestsWithProtectionLevel( global::Windows.Devices.Enumeration.DevicePairingKinds pairingKindsSupported,  global::Windows.Devices.Enumeration.DevicePairingProtectionLevel minProtectionLevel)
		{
			throw new global::System.NotImplementedException("The member bool DeviceInformationPairing.TryRegisterForAllInboundPairingRequestsWithProtectionLevel(DevicePairingKinds pairingKindsSupported, DevicePairingProtectionLevel minProtectionLevel) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool TryRegisterForAllInboundPairingRequests( global::Windows.Devices.Enumeration.DevicePairingKinds pairingKindsSupported)
		{
			throw new global::System.NotImplementedException("The member bool DeviceInformationPairing.TryRegisterForAllInboundPairingRequests(DevicePairingKinds pairingKindsSupported) is not implemented in Uno.");
		}
		#endif
	}
}
