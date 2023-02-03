#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowJobNotificationEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowConfiguration Configuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member PrintWorkflowConfiguration PrintWorkflowJobNotificationEventArgs.Configuration is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PrintWorkflowConfiguration%20PrintWorkflowJobNotificationEventArgs.Configuration");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowPrinterJob PrinterJob
		{
			get
			{
				throw new global::System.NotImplementedException("The member PrintWorkflowPrinterJob PrintWorkflowJobNotificationEventArgs.PrinterJob is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PrintWorkflowPrinterJob%20PrintWorkflowJobNotificationEventArgs.PrinterJob");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowJobNotificationEventArgs.Configuration.get
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowJobNotificationEventArgs.PrinterJob.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral PrintWorkflowJobNotificationEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20PrintWorkflowJobNotificationEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
