#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbInterruptOutPipe 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbWriteOptions WriteOptions
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbWriteOptions UsbInterruptOutPipe.WriteOptions is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UsbWriteOptions%20UsbInterruptOutPipe.WriteOptions");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbInterruptOutPipe", "UsbWriteOptions UsbInterruptOutPipe.WriteOptions");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbInterruptOutEndpointDescriptor EndpointDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbInterruptOutEndpointDescriptor UsbInterruptOutPipe.EndpointDescriptor is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UsbInterruptOutEndpointDescriptor%20UsbInterruptOutPipe.EndpointDescriptor");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream OutputStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IOutputStream UsbInterruptOutPipe.OutputStream is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IOutputStream%20UsbInterruptOutPipe.OutputStream");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbInterruptOutPipe.EndpointDescriptor.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ClearStallAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction UsbInterruptOutPipe.ClearStallAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20UsbInterruptOutPipe.ClearStallAsync%28%29");
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbInterruptOutPipe.WriteOptions.set
		// Forced skipping of method Windows.Devices.Usb.UsbInterruptOutPipe.WriteOptions.get
		// Forced skipping of method Windows.Devices.Usb.UsbInterruptOutPipe.OutputStream.get
	}
}
