#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEDevice : global::System.IDisposable
	{
		// Skipping already declared property BluetoothAddress
		// Skipping already declared property ConnectionStatus
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string BluetoothLEDevice.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService> GattServices
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<GattDeviceService> BluetoothLEDevice.GattServices is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property Name
		// Skipping already declared property Appearance
		// Skipping already declared property BluetoothAddressType
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceInformation DeviceInformation
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformation BluetoothLEDevice.DeviceInformation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceAccessInformation DeviceAccessInformation
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceAccessInformation BluetoothLEDevice.DeviceAccessInformation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothDeviceId BluetoothDeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothDeviceId BluetoothLEDevice.BluetoothDeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool WasSecureConnectionUsedForPairing
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool BluetoothLEDevice.WasSecureConnectionUsedForPairing is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.DeviceId.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.Name.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.GattServices.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.ConnectionStatus.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.BluetoothAddress.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService GetGattService( global::System.Guid serviceUuid)
		{
			throw new global::System.NotImplementedException("The member GattDeviceService BluetoothLEDevice.GetGattService(Guid serviceUuid) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.NameChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.NameChanged.remove
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.GattServicesChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.GattServicesChanged.remove
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.ConnectionStatusChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.ConnectionStatusChanged.remove
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.DeviceInformation.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.Appearance.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.BluetoothAddressType.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.DeviceAccessInformation.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceAccessStatus> RequestAccessAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceAccessStatus> BluetoothLEDevice.RequestAccessAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceServicesResult> GetGattServicesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattDeviceServicesResult> BluetoothLEDevice.GetGattServicesAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceServicesResult> GetGattServicesAsync( global::Windows.Devices.Bluetooth.BluetoothCacheMode cacheMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattDeviceServicesResult> BluetoothLEDevice.GetGattServicesAsync(BluetoothCacheMode cacheMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceServicesResult> GetGattServicesForUuidAsync( global::System.Guid serviceUuid)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattDeviceServicesResult> BluetoothLEDevice.GetGattServicesForUuidAsync(Guid serviceUuid) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceServicesResult> GetGattServicesForUuidAsync( global::System.Guid serviceUuid,  global::Windows.Devices.Bluetooth.BluetoothCacheMode cacheMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattDeviceServicesResult> BluetoothLEDevice.GetGattServicesForUuidAsync(Guid serviceUuid, BluetoothCacheMode cacheMode) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.BluetoothDeviceId.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.WasSecureConnectionUsedForPairing.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothLEConnectionParameters GetConnectionParameters()
		{
			throw new global::System.NotImplementedException("The member BluetoothLEConnectionParameters BluetoothLEDevice.GetConnectionParameters() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothLEConnectionPhy GetConnectionPhy()
		{
			throw new global::System.NotImplementedException("The member BluetoothLEConnectionPhy BluetoothLEDevice.GetConnectionPhy() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothLEPreferredConnectionParametersRequest RequestPreferredConnectionParameters( global::Windows.Devices.Bluetooth.BluetoothLEPreferredConnectionParameters preferredConnectionParameters)
		{
			throw new global::System.NotImplementedException("The member BluetoothLEPreferredConnectionParametersRequest BluetoothLEDevice.RequestPreferredConnectionParameters(BluetoothLEPreferredConnectionParameters preferredConnectionParameters) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.ConnectionParametersChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.ConnectionParametersChanged.remove
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.ConnectionPhyChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEDevice.ConnectionPhyChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEDevice", "void BluetoothLEDevice.Dispose()");
		}
		#endif
		// Skipping already declared method Windows.Devices.Bluetooth.BluetoothLEDevice.GetDeviceSelectorFromPairingState(bool)
		// Skipping already declared method Windows.Devices.Bluetooth.BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(Windows.Devices.Bluetooth.BluetoothConnectionStatus)
		// Skipping already declared method Windows.Devices.Bluetooth.BluetoothLEDevice.GetDeviceSelectorFromDeviceName(string)
		// Skipping already declared method Windows.Devices.Bluetooth.BluetoothLEDevice.GetDeviceSelectorFromBluetoothAddress(ulong)
		// Skipping already declared method Windows.Devices.Bluetooth.BluetoothLEDevice.GetDeviceSelectorFromBluetoothAddress(ulong, Windows.Devices.Bluetooth.BluetoothAddressType)
		// Skipping already declared method Windows.Devices.Bluetooth.BluetoothLEDevice.GetDeviceSelectorFromAppearance(Windows.Devices.Bluetooth.BluetoothLEAppearance)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.BluetoothLEDevice> FromBluetoothAddressAsync( ulong bluetoothAddress,  global::Windows.Devices.Bluetooth.BluetoothAddressType bluetoothAddressType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<BluetoothLEDevice> BluetoothLEDevice.FromBluetoothAddressAsync(ulong bluetoothAddress, BluetoothAddressType bluetoothAddressType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.BluetoothLEDevice> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<BluetoothLEDevice> BluetoothLEDevice.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.BluetoothLEDevice> FromBluetoothAddressAsync( ulong bluetoothAddress)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<BluetoothLEDevice> BluetoothLEDevice.FromBluetoothAddressAsync(ulong bluetoothAddress) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Devices.Bluetooth.BluetoothLEDevice.GetDeviceSelector()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.BluetoothLEDevice, object> ConnectionStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEDevice", "event TypedEventHandler<BluetoothLEDevice, object> BluetoothLEDevice.ConnectionStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEDevice", "event TypedEventHandler<BluetoothLEDevice, object> BluetoothLEDevice.ConnectionStatusChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.BluetoothLEDevice, object> GattServicesChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEDevice", "event TypedEventHandler<BluetoothLEDevice, object> BluetoothLEDevice.GattServicesChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEDevice", "event TypedEventHandler<BluetoothLEDevice, object> BluetoothLEDevice.GattServicesChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.BluetoothLEDevice, object> NameChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEDevice", "event TypedEventHandler<BluetoothLEDevice, object> BluetoothLEDevice.NameChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEDevice", "event TypedEventHandler<BluetoothLEDevice, object> BluetoothLEDevice.NameChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.BluetoothLEDevice, object> ConnectionParametersChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEDevice", "event TypedEventHandler<BluetoothLEDevice, object> BluetoothLEDevice.ConnectionParametersChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEDevice", "event TypedEventHandler<BluetoothLEDevice, object> BluetoothLEDevice.ConnectionParametersChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.BluetoothLEDevice, object> ConnectionPhyChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEDevice", "event TypedEventHandler<BluetoothLEDevice, object> BluetoothLEDevice.ConnectionPhyChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEDevice", "event TypedEventHandler<BluetoothLEDevice, object> BluetoothLEDevice.ConnectionPhyChanged");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
