#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbDeviceClass 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte? SubclassCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte? UsbDeviceClass.SubclassCode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbDeviceClass", "byte? UsbDeviceClass.SubclassCode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte? ProtocolCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte? UsbDeviceClass.ProtocolCode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbDeviceClass", "byte? UsbDeviceClass.ProtocolCode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte ClassCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UsbDeviceClass.ClassCode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbDeviceClass", "byte UsbDeviceClass.ClassCode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public UsbDeviceClass() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbDeviceClass", "UsbDeviceClass.UsbDeviceClass()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceClass.UsbDeviceClass()
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceClass.ClassCode.get
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceClass.ClassCode.set
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceClass.SubclassCode.get
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceClass.SubclassCode.set
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceClass.ProtocolCode.get
		// Forced skipping of method Windows.Devices.Usb.UsbDeviceClass.ProtocolCode.set
	}
}
