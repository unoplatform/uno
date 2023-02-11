#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbInterface 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbBulkInPipe> BulkInPipes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbBulkInPipe> UsbInterface.BulkInPipes is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CUsbBulkInPipe%3E%20UsbInterface.BulkInPipes");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbBulkOutPipe> BulkOutPipes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbBulkOutPipe> UsbInterface.BulkOutPipes is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CUsbBulkOutPipe%3E%20UsbInterface.BulkOutPipes");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbDescriptor> Descriptors
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbDescriptor> UsbInterface.Descriptors is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CUsbDescriptor%3E%20UsbInterface.Descriptors");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte InterfaceNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UsbInterface.InterfaceNumber is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=byte%20UsbInterface.InterfaceNumber");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbInterfaceSetting> InterfaceSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbInterfaceSetting> UsbInterface.InterfaceSettings is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CUsbInterfaceSetting%3E%20UsbInterface.InterfaceSettings");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbInterruptInPipe> InterruptInPipes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbInterruptInPipe> UsbInterface.InterruptInPipes is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CUsbInterruptInPipe%3E%20UsbInterface.InterruptInPipes");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbInterruptOutPipe> InterruptOutPipes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbInterruptOutPipe> UsbInterface.InterruptOutPipes is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CUsbInterruptOutPipe%3E%20UsbInterface.InterruptOutPipes");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbInterface.BulkInPipes.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterface.InterruptInPipes.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterface.BulkOutPipes.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterface.InterruptOutPipes.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterface.InterfaceSettings.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterface.InterfaceNumber.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterface.Descriptors.get
	}
}
