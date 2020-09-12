#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowBackgroundSession 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowSessionStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member PrintWorkflowSessionStatus PrintWorkflowBackgroundSession.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession.SetupRequested.add
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession.SetupRequested.remove
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession.Submitted.add
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession.Submitted.remove
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession.Status.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession", "void PrintWorkflowBackgroundSession.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession, global::Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSetupRequestedEventArgs> SetupRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession", "event TypedEventHandler<PrintWorkflowBackgroundSession, PrintWorkflowBackgroundSetupRequestedEventArgs> PrintWorkflowBackgroundSession.SetupRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession", "event TypedEventHandler<PrintWorkflowBackgroundSession, PrintWorkflowBackgroundSetupRequestedEventArgs> PrintWorkflowBackgroundSession.SetupRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession, global::Windows.Graphics.Printing.Workflow.PrintWorkflowSubmittedEventArgs> Submitted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession", "event TypedEventHandler<PrintWorkflowBackgroundSession, PrintWorkflowSubmittedEventArgs> PrintWorkflowBackgroundSession.Submitted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowBackgroundSession", "event TypedEventHandler<PrintWorkflowBackgroundSession, PrintWorkflowSubmittedEventArgs> PrintWorkflowBackgroundSession.Submitted");
			}
		}
		#endif
	}
}
