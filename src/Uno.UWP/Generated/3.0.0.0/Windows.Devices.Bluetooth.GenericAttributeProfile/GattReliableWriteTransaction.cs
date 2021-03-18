#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattReliableWriteTransaction 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public GattReliableWriteTransaction() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattReliableWriteTransaction", "GattReliableWriteTransaction.GattReliableWriteTransaction()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattReliableWriteTransaction.GattReliableWriteTransaction()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void WriteValue( global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic characteristic,  global::Windows.Storage.Streams.IBuffer value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattReliableWriteTransaction", "void GattReliableWriteTransaction.WriteValue(GattCharacteristic characteristic, IBuffer value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus> CommitAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattCommunicationStatus> GattReliableWriteTransaction.CommitAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteResult> CommitWithResultAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattWriteResult> GattReliableWriteTransaction.CommitWithResultAsync() is not implemented in Uno.");
		}
		#endif
	}
}
