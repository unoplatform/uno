#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowSubmittedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowSubmittedOperation Operation
		{
			get
			{
				throw new global::System.NotImplementedException("The member PrintWorkflowSubmittedOperation PrintWorkflowSubmittedEventArgs.Operation is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PrintWorkflowSubmittedOperation%20PrintWorkflowSubmittedEventArgs.Operation");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowSubmittedEventArgs.Operation.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowTarget GetTarget( global::Windows.Graphics.Printing.PrintTicket.WorkflowPrintTicket jobPrintTicket)
		{
			throw new global::System.NotImplementedException("The member PrintWorkflowTarget PrintWorkflowSubmittedEventArgs.GetTarget(WorkflowPrintTicket jobPrintTicket) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PrintWorkflowTarget%20PrintWorkflowSubmittedEventArgs.GetTarget%28WorkflowPrintTicket%20jobPrintTicket%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral PrintWorkflowSubmittedEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20PrintWorkflowSubmittedEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
