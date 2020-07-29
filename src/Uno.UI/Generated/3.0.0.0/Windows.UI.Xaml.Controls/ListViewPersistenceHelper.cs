#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ListViewPersistenceHelper 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetRelativeScrollPosition( global::Windows.UI.Xaml.Controls.ListViewBase listViewBase,  global::Windows.UI.Xaml.Controls.ListViewItemToKeyHandler itemToKeyHandler)
		{
			throw new global::System.NotImplementedException("The member string ListViewPersistenceHelper.GetRelativeScrollPosition(ListViewBase listViewBase, ListViewItemToKeyHandler itemToKeyHandler) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction SetRelativeScrollPositionAsync( global::Windows.UI.Xaml.Controls.ListViewBase listViewBase,  string relativeScrollPosition,  global::Windows.UI.Xaml.Controls.ListViewKeyToItemHandler keyToItemHandler)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ListViewPersistenceHelper.SetRelativeScrollPositionAsync(ListViewBase listViewBase, string relativeScrollPosition, ListViewKeyToItemHandler keyToItemHandler) is not implemented in Uno.");
		}
		#endif
	}
}
