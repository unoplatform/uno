#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowUILauncher 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsUILaunchEnabled()
		{
			throw new global::System.NotImplementedException("The member bool PrintWorkflowUILauncher.IsUILaunchEnabled() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PrintWorkflowUILauncher.IsUILaunchEnabled%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Printing.Workflow.PrintWorkflowUICompletionStatus> LaunchAndCompleteUIAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PrintWorkflowUICompletionStatus> PrintWorkflowUILauncher.LaunchAndCompleteUIAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CPrintWorkflowUICompletionStatus%3E%20PrintWorkflowUILauncher.LaunchAndCompleteUIAsync%28%29");
		}
		#endif
	}
}
