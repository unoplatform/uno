#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationViewList : global::Windows.UI.Xaml.Controls.ListView
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public NavigationViewList() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.NavigationViewList", "NavigationViewList.NavigationViewList()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewList.NavigationViewList()
	}
}
