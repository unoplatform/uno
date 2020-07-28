#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbInterfaceDescriptor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte AlternateSettingNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UsbInterfaceDescriptor.AlternateSettingNumber is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte ClassCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UsbInterfaceDescriptor.ClassCode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte InterfaceNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UsbInterfaceDescriptor.InterfaceNumber is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte ProtocolCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UsbInterfaceDescriptor.ProtocolCode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte SubclassCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UsbInterfaceDescriptor.SubclassCode is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceDescriptor.ClassCode.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceDescriptor.SubclassCode.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceDescriptor.ProtocolCode.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceDescriptor.AlternateSettingNumber.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceDescriptor.InterfaceNumber.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool TryParse( global::Windows.Devices.Usb.UsbDescriptor descriptor, out global::Windows.Devices.Usb.UsbInterfaceDescriptor parsed)
		{
			throw new global::System.NotImplementedException("The member bool UsbInterfaceDescriptor.TryParse(UsbDescriptor descriptor, out UsbInterfaceDescriptor parsed) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Usb.UsbInterfaceDescriptor Parse( global::Windows.Devices.Usb.UsbDescriptor descriptor)
		{
			throw new global::System.NotImplementedException("The member UsbInterfaceDescriptor UsbInterfaceDescriptor.Parse(UsbDescriptor descriptor) is not implemented in Uno.");
		}
		#endif
	}
}
