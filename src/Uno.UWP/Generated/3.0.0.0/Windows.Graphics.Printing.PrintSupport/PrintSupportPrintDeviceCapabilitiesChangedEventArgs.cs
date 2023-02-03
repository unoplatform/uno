#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.PrintSupport
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintSupportPrintDeviceCapabilitiesChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Xml.Dom.XmlDocument GetCurrentPrintDeviceCapabilities()
		{
			throw new global::System.NotImplementedException("The member XmlDocument PrintSupportPrintDeviceCapabilitiesChangedEventArgs.GetCurrentPrintDeviceCapabilities() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=XmlDocument%20PrintSupportPrintDeviceCapabilitiesChangedEventArgs.GetCurrentPrintDeviceCapabilities%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UpdatePrintDeviceCapabilities( global::Windows.Data.Xml.Dom.XmlDocument updatedPdc)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintSupport.PrintSupportPrintDeviceCapabilitiesChangedEventArgs", "void PrintSupportPrintDeviceCapabilitiesChangedEventArgs.UpdatePrintDeviceCapabilities(XmlDocument updatedPdc)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral PrintSupportPrintDeviceCapabilitiesChangedEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20PrintSupportPrintDeviceCapabilitiesChangedEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
