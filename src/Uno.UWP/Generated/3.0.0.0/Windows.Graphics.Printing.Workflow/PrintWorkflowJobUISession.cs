#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowJobUISession 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowSessionStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member PrintWorkflowSessionStatus PrintWorkflowJobUISession.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PrintWorkflowSessionStatus%20PrintWorkflowJobUISession.Status");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession.Status.get
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession.PdlDataAvailable.add
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession.PdlDataAvailable.remove
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession.JobNotification.add
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession.JobNotification.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession", "void PrintWorkflowJobUISession.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession, global::Windows.Graphics.Printing.Workflow.PrintWorkflowJobNotificationEventArgs> JobNotification
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession", "event TypedEventHandler<PrintWorkflowJobUISession, PrintWorkflowJobNotificationEventArgs> PrintWorkflowJobUISession.JobNotification");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession", "event TypedEventHandler<PrintWorkflowJobUISession, PrintWorkflowJobNotificationEventArgs> PrintWorkflowJobUISession.JobNotification");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession, global::Windows.Graphics.Printing.Workflow.PrintWorkflowPdlDataAvailableEventArgs> PdlDataAvailable
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession", "event TypedEventHandler<PrintWorkflowJobUISession, PrintWorkflowPdlDataAvailableEventArgs> PrintWorkflowJobUISession.PdlDataAvailable");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowJobUISession", "event TypedEventHandler<PrintWorkflowJobUISession, PrintWorkflowPdlDataAvailableEventArgs> PrintWorkflowJobUISession.PdlDataAvailable");
			}
		}
		#endif
	}
}
