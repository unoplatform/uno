#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbInterfaceSetting 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbBulkInEndpointDescriptor> BulkInEndpoints
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbBulkInEndpointDescriptor> UsbInterfaceSetting.BulkInEndpoints is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbBulkOutEndpointDescriptor> BulkOutEndpoints
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbBulkOutEndpointDescriptor> UsbInterfaceSetting.BulkOutEndpoints is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbDescriptor> Descriptors
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbDescriptor> UsbInterfaceSetting.Descriptors is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbInterfaceDescriptor InterfaceDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbInterfaceDescriptor UsbInterfaceSetting.InterfaceDescriptor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbInterruptInEndpointDescriptor> InterruptInEndpoints
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbInterruptInEndpointDescriptor> UsbInterfaceSetting.InterruptInEndpoints is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbInterruptOutEndpointDescriptor> InterruptOutEndpoints
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbInterruptOutEndpointDescriptor> UsbInterfaceSetting.InterruptOutEndpoints is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Selected
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool UsbInterfaceSetting.Selected is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceSetting.BulkInEndpoints.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceSetting.InterruptInEndpoints.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceSetting.BulkOutEndpoints.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceSetting.InterruptOutEndpoints.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceSetting.Selected.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SelectSettingAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction UsbInterfaceSetting.SelectSettingAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceSetting.InterfaceDescriptor.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterfaceSetting.Descriptors.get
	}
}
