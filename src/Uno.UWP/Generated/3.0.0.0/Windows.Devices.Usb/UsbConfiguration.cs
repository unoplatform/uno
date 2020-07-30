#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Usb
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UsbConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Usb.UsbConfigurationDescriptor ConfigurationDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UsbConfigurationDescriptor UsbConfiguration.ConfigurationDescriptor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbDescriptor> Descriptors
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbDescriptor> UsbConfiguration.Descriptors is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Usb.UsbInterface> UsbInterfaces
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UsbInterface> UsbConfiguration.UsbInterfaces is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Usb.UsbConfiguration.UsbInterfaces.get
		// Forced skipping of method Windows.Devices.Usb.UsbConfiguration.ConfigurationDescriptor.get
		// Forced skipping of method Windows.Devices.Usb.UsbConfiguration.Descriptors.get
	}
}
