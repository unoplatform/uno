#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbBulkInPipe 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbReadOptions ReadOptions
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbReadOptions UsbBulkInPipe.ReadOptions is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbBulkInPipe", "UsbReadOptions UsbBulkInPipe.ReadOptions");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbBulkInEndpointDescriptor EndpointDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbBulkInEndpointDescriptor UsbBulkInPipe.EndpointDescriptor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream InputStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IInputStream UsbBulkInPipe.InputStream is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MaxTransferSizeBytes
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UsbBulkInPipe.MaxTransferSizeBytes is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbBulkInPipe.MaxTransferSizeBytes.get
		// Forced skipping of method Windows.Devices.Usb.UsbBulkInPipe.EndpointDescriptor.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ClearStallAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction UsbBulkInPipe.ClearStallAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbBulkInPipe.ReadOptions.set
		// Forced skipping of method Windows.Devices.Usb.UsbBulkInPipe.ReadOptions.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void FlushBuffer()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbBulkInPipe", "void UsbBulkInPipe.FlushBuffer()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbBulkInPipe.InputStream.get
	}
}
