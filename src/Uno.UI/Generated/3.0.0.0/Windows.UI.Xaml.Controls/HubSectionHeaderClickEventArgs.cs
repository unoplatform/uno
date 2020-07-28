#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HubSectionHeaderClickEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.HubSection Section
		{
			get
			{
				throw new global::System.NotImplementedException("The member HubSection HubSectionHeaderClickEventArgs.Section is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public HubSectionHeaderClickEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.HubSectionHeaderClickEventArgs", "HubSectionHeaderClickEventArgs.HubSectionHeaderClickEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.HubSectionHeaderClickEventArgs.HubSectionHeaderClickEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.HubSectionHeaderClickEventArgs.Section.get
	}
}
