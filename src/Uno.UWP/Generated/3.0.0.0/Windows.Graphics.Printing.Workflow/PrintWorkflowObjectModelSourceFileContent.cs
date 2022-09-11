#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowObjectModelSourceFileContent 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PrintWorkflowObjectModelSourceFileContent( global::Windows.Storage.Streams.IInputStream xpsStream) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowObjectModelSourceFileContent", "PrintWorkflowObjectModelSourceFileContent.PrintWorkflowObjectModelSourceFileContent(IInputStream xpsStream)");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowObjectModelSourceFileContent.PrintWorkflowObjectModelSourceFileContent(Windows.Storage.Streams.IInputStream)
	}
}
