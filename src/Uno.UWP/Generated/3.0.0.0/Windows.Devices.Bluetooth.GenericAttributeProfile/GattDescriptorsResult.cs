#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattDescriptorsResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor> Descriptors
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<GattDescriptor> GattDescriptorsResult.Descriptors is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte? ProtocolError
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte? GattDescriptorsResult.ProtocolError is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattCommunicationStatus GattDescriptorsResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptorsResult.Status.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptorsResult.ProtocolError.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptorsResult.Descriptors.get
	}
}
