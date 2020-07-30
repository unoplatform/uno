#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TreeViewDragItemsStartingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Cancel
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool TreeViewDragItemsStartingEventArgs.Cancel is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs", "bool TreeViewDragItemsStartingEventArgs.Cancel");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackage Data
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackage TreeViewDragItemsStartingEventArgs.Data is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<object> Items
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<object> TreeViewDragItemsStartingEventArgs.Items is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs.Cancel.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs.Cancel.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs.Data.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs.Items.get
	}
}
