#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattSession : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool MaintainConnection
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool GattSession.MaintainConnection is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession", "bool GattSession.MaintainConnection");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanMaintainConnection
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool GattSession.CanMaintainConnection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothDeviceId DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothDeviceId GattSession.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort MaxPduSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort GattSession.MaxPduSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattSessionStatus SessionStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattSessionStatus GattSession.SessionStatus is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession.DeviceId.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession.CanMaintainConnection.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession.MaintainConnection.set
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession.MaintainConnection.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession.MaxPduSize.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession.SessionStatus.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession.MaxPduSizeChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession.MaxPduSizeChanged.remove
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession.SessionStatusChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession.SessionStatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession", "void GattSession.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession> FromDeviceIdAsync( global::Windows.Devices.Bluetooth.BluetoothDeviceId deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattSession> GattSession.FromDeviceIdAsync(BluetoothDeviceId deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession, object> MaxPduSizeChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession", "event TypedEventHandler<GattSession, object> GattSession.MaxPduSizeChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession", "event TypedEventHandler<GattSession, object> GattSession.MaxPduSizeChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession, global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattSessionStatusChangedEventArgs> SessionStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession", "event TypedEventHandler<GattSession, GattSessionStatusChangedEventArgs> GattSession.SessionStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession", "event TypedEventHandler<GattSession, GattSessionStatusChangedEventArgs> GattSession.SessionStatusChanged");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
