#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbDevice : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbConfiguration Configuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbConfiguration UsbDevice.Configuration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbInterface DefaultInterface
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbInterface UsbDevice.DefaultInterface is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbDeviceDescriptor DeviceDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbDeviceDescriptor UsbDevice.DeviceDescriptor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<uint> SendControlOutTransferAsync( global::Windows.Devices.Usb.UsbSetupPacket setupPacket,  global::Windows.Storage.Streams.IBuffer buffer)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<uint> UsbDevice.SendControlOutTransferAsync(UsbSetupPacket setupPacket, IBuffer buffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<uint> SendControlOutTransferAsync( global::Windows.Devices.Usb.UsbSetupPacket setupPacket)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<uint> UsbDevice.SendControlOutTransferAsync(UsbSetupPacket setupPacket) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IBuffer> SendControlInTransferAsync( global::Windows.Devices.Usb.UsbSetupPacket setupPacket,  global::Windows.Storage.Streams.IBuffer buffer)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IBuffer> UsbDevice.SendControlInTransferAsync(UsbSetupPacket setupPacket, IBuffer buffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IBuffer> SendControlInTransferAsync( global::Windows.Devices.Usb.UsbSetupPacket setupPacket)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IBuffer> UsbDevice.SendControlInTransferAsync(UsbSetupPacket setupPacket) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbDevice.DefaultInterface.get
		// Forced skipping of method Windows.Devices.Usb.UsbDevice.DeviceDescriptor.get
		// Forced skipping of method Windows.Devices.Usb.UsbDevice.Configuration.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbDevice", "void UsbDevice.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( uint vendorId,  uint productId,  global::System.Guid winUsbInterfaceClass)
		{
			throw new global::System.NotImplementedException("The member string UsbDevice.GetDeviceSelector(uint vendorId, uint productId, Guid winUsbInterfaceClass) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( global::System.Guid winUsbInterfaceClass)
		{
			throw new global::System.NotImplementedException("The member string UsbDevice.GetDeviceSelector(Guid winUsbInterfaceClass) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( uint vendorId,  uint productId)
		{
			throw new global::System.NotImplementedException("The member string UsbDevice.GetDeviceSelector(uint vendorId, uint productId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceClassSelector( global::Windows.Devices.Usb.UsbDeviceClass usbClass)
		{
			throw new global::System.NotImplementedException("The member string UsbDevice.GetDeviceClassSelector(UsbDeviceClass usbClass) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Usb.UsbDevice> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UsbDevice> UsbDevice.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		// Processing: System.IDisposable
	}
}
