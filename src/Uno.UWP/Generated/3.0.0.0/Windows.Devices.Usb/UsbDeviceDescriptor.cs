#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbDeviceDescriptor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint BcdDeviceRevision
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UsbDeviceDescriptor.BcdDeviceRevision is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint BcdUsb
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UsbDeviceDescriptor.BcdUsb is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte MaxPacketSize0
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UsbDeviceDescriptor.MaxPacketSize0 is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte NumberOfConfigurations
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UsbDeviceDescriptor.NumberOfConfigurations is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ProductId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UsbDeviceDescriptor.ProductId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint VendorId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UsbDeviceDescriptor.VendorId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceDescriptor.BcdUsb.get
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceDescriptor.MaxPacketSize0.get
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceDescriptor.VendorId.get
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceDescriptor.ProductId.get
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceDescriptor.BcdDeviceRevision.get
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceDescriptor.NumberOfConfigurations.get
	}
}
