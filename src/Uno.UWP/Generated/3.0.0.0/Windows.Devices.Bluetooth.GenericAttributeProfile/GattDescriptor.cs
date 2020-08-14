#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattDescriptor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattProtectionLevel ProtectionLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattProtectionLevel GattDescriptor.ProtectionLevel is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor", "GattProtectionLevel GattDescriptor.ProtectionLevel");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort AttributeHandle
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort GattDescriptor.AttributeHandle is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid Uuid
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid GattDescriptor.Uuid is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor.ProtectionLevel.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor.ProtectionLevel.set
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor.Uuid.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor.AttributeHandle.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattReadResult> ReadValueAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattReadResult> GattDescriptor.ReadValueAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattReadResult> ReadValueAsync( global::Windows.Devices.Bluetooth.BluetoothCacheMode cacheMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattReadResult> GattDescriptor.ReadValueAsync(BluetoothCacheMode cacheMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus> WriteValueAsync( global::Windows.Storage.Streams.IBuffer value)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattCommunicationStatus> GattDescriptor.WriteValueAsync(IBuffer value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteResult> WriteValueWithResultAsync( global::Windows.Storage.Streams.IBuffer value)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattWriteResult> GattDescriptor.WriteValueWithResultAsync(IBuffer value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Guid ConvertShortIdToUuid( ushort shortId)
		{
			throw new global::System.NotImplementedException("The member Guid GattDescriptor.ConvertShortIdToUuid(ushort shortId) is not implemented in Uno.");
		}
		#endif
	}
}
