#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbSetupPacket 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UsbSetupPacket.Value is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbSetupPacket", "uint UsbSetupPacket.Value");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbControlRequestType RequestType
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbControlRequestType UsbSetupPacket.RequestType is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbSetupPacket", "UsbControlRequestType UsbSetupPacket.RequestType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte Request
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UsbSetupPacket.Request is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbSetupPacket", "byte UsbSetupPacket.Request");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Length
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UsbSetupPacket.Length is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbSetupPacket", "uint UsbSetupPacket.Length");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Index
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UsbSetupPacket.Index is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbSetupPacket", "uint UsbSetupPacket.Index");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public UsbSetupPacket( global::Windows.Storage.Streams.IBuffer eightByteBuffer) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbSetupPacket", "UsbSetupPacket.UsbSetupPacket(IBuffer eightByteBuffer)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.UsbSetupPacket(Windows.Storage.Streams.IBuffer)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public UsbSetupPacket() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Usb.UsbSetupPacket", "UsbSetupPacket.UsbSetupPacket()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.UsbSetupPacket()
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.RequestType.get
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.RequestType.set
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.Request.get
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.Request.set
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.Value.get
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.Value.set
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.Index.get
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.Index.set
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.Length.get
		// Forced skipping of method Windows.Devices.Usb.UsbSetupPacket.Length.set
	}
}
