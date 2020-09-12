#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowTarget 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowStreamTarget TargetAsStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member PrintWorkflowStreamTarget PrintWorkflowTarget.TargetAsStream is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowObjectModelTargetPackage TargetAsXpsObjectModelPackage
		{
			get
			{
				throw new global::System.NotImplementedException("The member PrintWorkflowObjectModelTargetPackage PrintWorkflowTarget.TargetAsXpsObjectModelPackage is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowTarget.TargetAsStream.get
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowTarget.TargetAsXpsObjectModelPackage.get
	}
}
