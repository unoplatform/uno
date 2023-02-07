#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.PrintSupport
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintSupportPrintTicketValidationRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintTicket.WorkflowPrintTicket PrintTicket
		{
			get
			{
				throw new global::System.NotImplementedException("The member WorkflowPrintTicket PrintSupportPrintTicketValidationRequestedEventArgs.PrintTicket is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=WorkflowPrintTicket%20PrintSupportPrintTicketValidationRequestedEventArgs.PrintTicket");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.PrintSupport.PrintSupportPrintTicketValidationRequestedEventArgs.PrintTicket.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPrintTicketValidationStatus( global::Windows.Graphics.Printing.PrintSupport.WorkflowPrintTicketValidationStatus status)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintSupport.PrintSupportPrintTicketValidationRequestedEventArgs", "void PrintSupportPrintTicketValidationRequestedEventArgs.SetPrintTicketValidationStatus(WorkflowPrintTicketValidationStatus status)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral PrintSupportPrintTicketValidationRequestedEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20PrintSupportPrintTicketValidationRequestedEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
