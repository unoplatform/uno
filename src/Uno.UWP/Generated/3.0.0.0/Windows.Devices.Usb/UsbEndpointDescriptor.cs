#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbEndpointDescriptor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbBulkInEndpointDescriptor AsBulkInEndpointDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbBulkInEndpointDescriptor UsbEndpointDescriptor.AsBulkInEndpointDescriptor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbBulkOutEndpointDescriptor AsBulkOutEndpointDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbBulkOutEndpointDescriptor UsbEndpointDescriptor.AsBulkOutEndpointDescriptor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbInterruptInEndpointDescriptor AsInterruptInEndpointDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbInterruptInEndpointDescriptor UsbEndpointDescriptor.AsInterruptInEndpointDescriptor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbInterruptOutEndpointDescriptor AsInterruptOutEndpointDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbInterruptOutEndpointDescriptor UsbEndpointDescriptor.AsInterruptOutEndpointDescriptor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbTransferDirection Direction
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbTransferDirection UsbEndpointDescriptor.Direction is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte EndpointNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UsbEndpointDescriptor.EndpointNumber is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbEndpointType EndpointType
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbEndpointType UsbEndpointDescriptor.EndpointType is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbEndpointDescriptor.EndpointNumber.get
		// Forced skipping of method Windows.Devices.Usb.UsbEndpointDescriptor.Direction.get
		// Forced skipping of method Windows.Devices.Usb.UsbEndpointDescriptor.EndpointType.get
		// Forced skipping of method Windows.Devices.Usb.UsbEndpointDescriptor.AsBulkInEndpointDescriptor.get
		// Forced skipping of method Windows.Devices.Usb.UsbEndpointDescriptor.AsInterruptInEndpointDescriptor.get
		// Forced skipping of method Windows.Devices.Usb.UsbEndpointDescriptor.AsBulkOutEndpointDescriptor.get
		// Forced skipping of method Windows.Devices.Usb.UsbEndpointDescriptor.AsInterruptOutEndpointDescriptor.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool TryParse( global::Windows.Devices.Usb.UsbDescriptor descriptor, out global::Windows.Devices.Usb.UsbEndpointDescriptor parsed)
		{
			throw new global::System.NotImplementedException("The member bool UsbEndpointDescriptor.TryParse(UsbDescriptor descriptor, out UsbEndpointDescriptor parsed) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Usb.UsbEndpointDescriptor Parse( global::Windows.Devices.Usb.UsbDescriptor descriptor)
		{
			throw new global::System.NotImplementedException("The member UsbEndpointDescriptor UsbEndpointDescriptor.Parse(UsbDescriptor descriptor) is not implemented in Uno.");
		}
		#endif
	}
}
