#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Printers.Extensions
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Print3DWorkflow 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPrintReady
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Print3DWorkflow.IsPrintReady is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Printers.Extensions.Print3DWorkflow", "bool Print3DWorkflow.IsPrintReady");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceID
		{
			get
			{
				throw new global::System.NotImplementedException("The member string Print3DWorkflow.DeviceID is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Printers.Extensions.Print3DWorkflow.DeviceID.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object GetPrintModelPackage()
		{
			throw new global::System.NotImplementedException("The member object Print3DWorkflow.GetPrintModelPackage() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Printers.Extensions.Print3DWorkflow.IsPrintReady.get
		// Forced skipping of method Windows.Devices.Printers.Extensions.Print3DWorkflow.IsPrintReady.set
		// Forced skipping of method Windows.Devices.Printers.Extensions.Print3DWorkflow.PrintRequested.add
		// Forced skipping of method Windows.Devices.Printers.Extensions.Print3DWorkflow.PrintRequested.remove
		// Forced skipping of method Windows.Devices.Printers.Extensions.Print3DWorkflow.PrinterChanged.add
		// Forced skipping of method Windows.Devices.Printers.Extensions.Print3DWorkflow.PrinterChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Printers.Extensions.Print3DWorkflow, global::Windows.Devices.Printers.Extensions.Print3DWorkflowPrintRequestedEventArgs> PrintRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Printers.Extensions.Print3DWorkflow", "event TypedEventHandler<Print3DWorkflow, Print3DWorkflowPrintRequestedEventArgs> Print3DWorkflow.PrintRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Printers.Extensions.Print3DWorkflow", "event TypedEventHandler<Print3DWorkflow, Print3DWorkflowPrintRequestedEventArgs> Print3DWorkflow.PrintRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Printers.Extensions.Print3DWorkflow, global::Windows.Devices.Printers.Extensions.Print3DWorkflowPrinterChangedEventArgs> PrinterChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Printers.Extensions.Print3DWorkflow", "event TypedEventHandler<Print3DWorkflow, Print3DWorkflowPrinterChangedEventArgs> Print3DWorkflow.PrinterChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Printers.Extensions.Print3DWorkflow", "event TypedEventHandler<Print3DWorkflow, Print3DWorkflowPrinterChangedEventArgs> Print3DWorkflow.PrinterChanged");
			}
		}
		#endif
	}
}
