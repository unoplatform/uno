#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattCharacteristic 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattProtectionLevel ProtectionLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattProtectionLevel GattCharacteristic.ProtectionLevel is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic", "GattProtectionLevel GattCharacteristic.ProtectionLevel");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort AttributeHandle
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort GattCharacteristic.AttributeHandle is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties CharacteristicProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattCharacteristicProperties GattCharacteristic.CharacteristicProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattPresentationFormat> PresentationFormats
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<GattPresentationFormat> GattCharacteristic.PresentationFormats is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string UserDescription
		{
			get
			{
				throw new global::System.NotImplementedException("The member string GattCharacteristic.UserDescription is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid Uuid
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid GattCharacteristic.Uuid is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService Service
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattDeviceService GattCharacteristic.Service is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor> GetDescriptors( global::System.Guid descriptorUuid)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<GattDescriptor> GattCharacteristic.GetDescriptors(Guid descriptorUuid) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic.CharacteristicProperties.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic.ProtectionLevel.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic.ProtectionLevel.set
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic.UserDescription.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic.Uuid.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic.AttributeHandle.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic.PresentationFormats.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattReadResult> ReadValueAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattReadResult> GattCharacteristic.ReadValueAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattReadResult> ReadValueAsync( global::Windows.Devices.Bluetooth.BluetoothCacheMode cacheMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattReadResult> GattCharacteristic.ReadValueAsync(BluetoothCacheMode cacheMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus> WriteValueAsync( global::Windows.Storage.Streams.IBuffer value)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattCommunicationStatus> GattCharacteristic.WriteValueAsync(IBuffer value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus> WriteValueAsync( global::Windows.Storage.Streams.IBuffer value,  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteOption writeOption)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattCommunicationStatus> GattCharacteristic.WriteValueAsync(IBuffer value, GattWriteOption writeOption) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattReadClientCharacteristicConfigurationDescriptorResult> ReadClientCharacteristicConfigurationDescriptorAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattReadClientCharacteristicConfigurationDescriptorResult> GattCharacteristic.ReadClientCharacteristicConfigurationDescriptorAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus> WriteClientCharacteristicConfigurationDescriptorAsync( global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattClientCharacteristicConfigurationDescriptorValue clientCharacteristicConfigurationDescriptorValue)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattCommunicationStatus> GattCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue clientCharacteristicConfigurationDescriptorValue) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic.ValueChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic.ValueChanged.remove
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic.Service.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor> GetAllDescriptors()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<GattDescriptor> GattCharacteristic.GetAllDescriptors() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptorsResult> GetDescriptorsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattDescriptorsResult> GattCharacteristic.GetDescriptorsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptorsResult> GetDescriptorsAsync( global::Windows.Devices.Bluetooth.BluetoothCacheMode cacheMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattDescriptorsResult> GattCharacteristic.GetDescriptorsAsync(BluetoothCacheMode cacheMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptorsResult> GetDescriptorsForUuidAsync( global::System.Guid descriptorUuid)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattDescriptorsResult> GattCharacteristic.GetDescriptorsForUuidAsync(Guid descriptorUuid) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptorsResult> GetDescriptorsForUuidAsync( global::System.Guid descriptorUuid,  global::Windows.Devices.Bluetooth.BluetoothCacheMode cacheMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattDescriptorsResult> GattCharacteristic.GetDescriptorsForUuidAsync(Guid descriptorUuid, BluetoothCacheMode cacheMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteResult> WriteValueWithResultAsync( global::Windows.Storage.Streams.IBuffer value)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattWriteResult> GattCharacteristic.WriteValueWithResultAsync(IBuffer value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteResult> WriteValueWithResultAsync( global::Windows.Storage.Streams.IBuffer value,  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteOption writeOption)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattWriteResult> GattCharacteristic.WriteValueWithResultAsync(IBuffer value, GattWriteOption writeOption) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteResult> WriteClientCharacteristicConfigurationDescriptorWithResultAsync( global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattClientCharacteristicConfigurationDescriptorValue clientCharacteristicConfigurationDescriptorValue)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattWriteResult> GattCharacteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(GattClientCharacteristicConfigurationDescriptorValue clientCharacteristicConfigurationDescriptorValue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Guid ConvertShortIdToUuid( ushort shortId)
		{
			throw new global::System.NotImplementedException("The member Guid GattCharacteristic.ConvertShortIdToUuid(ushort shortId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic, global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattValueChangedEventArgs> ValueChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic", "event TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> GattCharacteristic.ValueChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic", "event TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> GattCharacteristic.ValueChanged");
			}
		}
		#endif
	}
}
