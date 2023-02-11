#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowPdlTargetStream 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream GetOutputStream()
		{
			throw new global::System.NotImplementedException("The member IOutputStream PrintWorkflowPdlTargetStream.GetOutputStream() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IOutputStream%20PrintWorkflowPdlTargetStream.GetOutputStream%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void CompleteStreamSubmission( global::Windows.Graphics.Printing.Workflow.PrintWorkflowSubmittedStatus status)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.Workflow.PrintWorkflowPdlTargetStream", "void PrintWorkflowPdlTargetStream.CompleteStreamSubmission(PrintWorkflowSubmittedStatus status)");
		}
		#endif
	}
}
