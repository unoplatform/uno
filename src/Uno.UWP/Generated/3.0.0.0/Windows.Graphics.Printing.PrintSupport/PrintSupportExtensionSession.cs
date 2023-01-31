#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.PrintSupport
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintSupportExtensionSession 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Printers.IppPrintDevice Printer
		{
			get
			{
				throw new global::System.NotImplementedException("The member IppPrintDevice PrintSupportExtensionSession.Printer is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IppPrintDevice%20PrintSupportExtensionSession.Printer");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession.Printer.get
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession.PrintTicketValidationRequested.add
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession.PrintTicketValidationRequested.remove
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession.PrintDeviceCapabilitiesChanged.add
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession.PrintDeviceCapabilitiesChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession", "void PrintSupportExtensionSession.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession, global::Windows.Graphics.Printing.PrintSupport.PrintSupportPrintDeviceCapabilitiesChangedEventArgs> PrintDeviceCapabilitiesChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession", "event TypedEventHandler<PrintSupportExtensionSession, PrintSupportPrintDeviceCapabilitiesChangedEventArgs> PrintSupportExtensionSession.PrintDeviceCapabilitiesChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession", "event TypedEventHandler<PrintSupportExtensionSession, PrintSupportPrintDeviceCapabilitiesChangedEventArgs> PrintSupportExtensionSession.PrintDeviceCapabilitiesChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession, global::Windows.Graphics.Printing.PrintSupport.PrintSupportPrintTicketValidationRequestedEventArgs> PrintTicketValidationRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession", "event TypedEventHandler<PrintSupportExtensionSession, PrintSupportPrintTicketValidationRequestedEventArgs> PrintSupportExtensionSession.PrintTicketValidationRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintSupport.PrintSupportExtensionSession", "event TypedEventHandler<PrintSupportExtensionSession, PrintSupportPrintTicketValidationRequestedEventArgs> PrintSupportExtensionSession.PrintTicketValidationRequested");
			}
		}
		#endif
	}
}
