#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GroupStyleSelector 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public GroupStyleSelector() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.GroupStyleSelector", "GroupStyleSelector.GroupStyleSelector()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.GroupStyleSelector.GroupStyleSelector()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.GroupStyle SelectGroupStyle( object group,  uint level)
		{
			throw new global::System.NotImplementedException("The member GroupStyle GroupStyleSelector.SelectGroupStyle(object group, uint level) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		protected virtual global::Windows.UI.Xaml.Controls.GroupStyle SelectGroupStyleCore( object group,  uint level)
		{
			throw new global::System.NotImplementedException("The member GroupStyle GroupStyleSelector.SelectGroupStyleCore(object group, uint level) is not implemented in Uno.");
		}
		#endif
	}
}
