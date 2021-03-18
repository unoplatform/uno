#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbInterruptInPipe 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbInterruptInEndpointDescriptor EndpointDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbInterruptInEndpointDescriptor UsbInterruptInPipe.EndpointDescriptor is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbInterruptInPipe.EndpointDescriptor.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ClearStallAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction UsbInterruptInPipe.ClearStallAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbInterruptInPipe.DataReceived.add
		// Forced skipping of method Windows.Devices.Usb.UsbInterruptInPipe.DataReceived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Usb.UsbInterruptInPipe, global::Windows.Devices.Usb.UsbInterruptInEventArgs> DataReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbInterruptInPipe", "event TypedEventHandler<UsbInterruptInPipe, UsbInterruptInEventArgs> UsbInterruptInPipe.DataReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbInterruptInPipe", "event TypedEventHandler<UsbInterruptInPipe, UsbInterruptInEventArgs> UsbInterruptInPipe.DataReceived");
			}
		}
		#endif
	}
}
