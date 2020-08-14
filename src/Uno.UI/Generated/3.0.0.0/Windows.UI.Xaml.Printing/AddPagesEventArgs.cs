#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Printing
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AddPagesEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintTaskOptions PrintTaskOptions
		{
			get
			{
				throw new global::System.NotImplementedException("The member PrintTaskOptions AddPagesEventArgs.PrintTaskOptions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AddPagesEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Printing.AddPagesEventArgs", "AddPagesEventArgs.AddPagesEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Printing.AddPagesEventArgs.AddPagesEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Printing.AddPagesEventArgs.PrintTaskOptions.get
	}
}
