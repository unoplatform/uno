#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ExpandCollapseState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Collapsed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Expanded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PartiallyExpanded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeafNode,
		#endif
	}
	#endif
}
