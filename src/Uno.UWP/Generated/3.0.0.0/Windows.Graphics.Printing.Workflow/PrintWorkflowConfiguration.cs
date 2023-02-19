#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string JobTitle
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PrintWorkflowConfiguration.JobTitle is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PrintWorkflowConfiguration.JobTitle");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SessionId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PrintWorkflowConfiguration.SessionId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PrintWorkflowConfiguration.SessionId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SourceAppDisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PrintWorkflowConfiguration.SourceAppDisplayName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PrintWorkflowConfiguration.SourceAppDisplayName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AbortPrintFlow( global::Windows.Graphics.Printing.Workflow.PrintWorkflowJobAbortReason reason)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowConfiguration", "void PrintWorkflowConfiguration.AbortPrintFlow(PrintWorkflowJobAbortReason reason)");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowConfiguration.SourceAppDisplayName.get
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowConfiguration.JobTitle.get
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowConfiguration.SessionId.get
	}
}
