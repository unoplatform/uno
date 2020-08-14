#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ItemsPickedEventArgs : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<object> AddedItems
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<object> ItemsPickedEventArgs.AddedItems is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<object> RemovedItems
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<object> ItemsPickedEventArgs.RemovedItems is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || false || false || false || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__MACOS__")]
		public ItemsPickedEventArgs() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsPickedEventArgs", "ItemsPickedEventArgs.ItemsPickedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPickedEventArgs.ItemsPickedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPickedEventArgs.AddedItems.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPickedEventArgs.RemovedItems.get
	}
}
