#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PivotItemEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.PivotItem Item
		{
			get
			{
				throw new global::System.NotImplementedException("The member PivotItem PivotItemEventArgs.Item is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PivotItemEventArgs", "PivotItem PivotItemEventArgs.Item");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PivotItemEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PivotItemEventArgs", "PivotItemEventArgs.PivotItemEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.PivotItemEventArgs.PivotItemEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.PivotItemEventArgs.Item.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PivotItemEventArgs.Item.set
	}
}
