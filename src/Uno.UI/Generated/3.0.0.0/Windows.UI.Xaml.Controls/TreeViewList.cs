#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TreeViewList : global::Windows.UI.Xaml.Controls.ListView
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public TreeViewList() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeViewList", "TreeViewList.TreeViewList()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewList.TreeViewList()
	}
}
