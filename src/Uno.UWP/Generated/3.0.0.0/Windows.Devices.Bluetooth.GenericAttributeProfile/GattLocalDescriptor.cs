#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattLocalDescriptor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattProtectionLevel ReadProtectionLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattProtectionLevel GattLocalDescriptor.ReadProtectionLevel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer StaticValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer GattLocalDescriptor.StaticValue is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid Uuid
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid GattLocalDescriptor.Uuid is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattProtectionLevel WriteProtectionLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattProtectionLevel GattLocalDescriptor.WriteProtectionLevel is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor.Uuid.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor.StaticValue.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor.ReadProtectionLevel.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor.WriteProtectionLevel.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor.ReadRequested.add
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor.ReadRequested.remove
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor.WriteRequested.add
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor.WriteRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor, global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattReadRequestedEventArgs> ReadRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor", "event TypedEventHandler<GattLocalDescriptor, GattReadRequestedEventArgs> GattLocalDescriptor.ReadRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor", "event TypedEventHandler<GattLocalDescriptor, GattReadRequestedEventArgs> GattLocalDescriptor.ReadRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor, global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequestedEventArgs> WriteRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor", "event TypedEventHandler<GattLocalDescriptor, GattWriteRequestedEventArgs> GattLocalDescriptor.WriteRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalDescriptor", "event TypedEventHandler<GattLocalDescriptor, GattWriteRequestedEventArgs> GattLocalDescriptor.WriteRequested");
			}
		}
		#endif
	}
}
