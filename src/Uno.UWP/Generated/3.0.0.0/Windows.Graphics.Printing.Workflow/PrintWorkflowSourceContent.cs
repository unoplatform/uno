#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowSourceContent 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Printing.PrintTicket.WorkflowPrintTicket> GetJobPrintTicketAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WorkflowPrintTicket> PrintWorkflowSourceContent.GetJobPrintTicketAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CWorkflowPrintTicket%3E%20PrintWorkflowSourceContent.GetJobPrintTicketAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowSpoolStreamContent GetSourceSpoolDataAsStreamContent()
		{
			throw new global::System.NotImplementedException("The member PrintWorkflowSpoolStreamContent PrintWorkflowSourceContent.GetSourceSpoolDataAsStreamContent() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PrintWorkflowSpoolStreamContent%20PrintWorkflowSourceContent.GetSourceSpoolDataAsStreamContent%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowObjectModelSourceFileContent GetSourceSpoolDataAsXpsObjectModel()
		{
			throw new global::System.NotImplementedException("The member PrintWorkflowObjectModelSourceFileContent PrintWorkflowSourceContent.GetSourceSpoolDataAsXpsObjectModel() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PrintWorkflowObjectModelSourceFileContent%20PrintWorkflowSourceContent.GetSourceSpoolDataAsXpsObjectModel%28%29");
		}
		#endif
	}
}
