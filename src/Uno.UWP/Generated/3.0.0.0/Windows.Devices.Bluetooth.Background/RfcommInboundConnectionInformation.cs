#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RfcommInboundConnectionInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothServiceCapabilities ServiceCapabilities
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothServiceCapabilities RfcommInboundConnectionInformation.ServiceCapabilities is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Background.RfcommInboundConnectionInformation", "BluetoothServiceCapabilities RfcommInboundConnectionInformation.ServiceCapabilities");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer SdpRecord
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer RfcommInboundConnectionInformation.SdpRecord is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Background.RfcommInboundConnectionInformation", "IBuffer RfcommInboundConnectionInformation.SdpRecord");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.Rfcomm.RfcommServiceId LocalServiceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member RfcommServiceId RfcommInboundConnectionInformation.LocalServiceId is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Background.RfcommInboundConnectionInformation", "RfcommServiceId RfcommInboundConnectionInformation.LocalServiceId");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Background.RfcommInboundConnectionInformation.SdpRecord.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.RfcommInboundConnectionInformation.SdpRecord.set
		// Forced skipping of method Windows.Devices.Bluetooth.Background.RfcommInboundConnectionInformation.LocalServiceId.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.RfcommInboundConnectionInformation.LocalServiceId.set
		// Forced skipping of method Windows.Devices.Bluetooth.Background.RfcommInboundConnectionInformation.ServiceCapabilities.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.RfcommInboundConnectionInformation.ServiceCapabilities.set
	}
}
