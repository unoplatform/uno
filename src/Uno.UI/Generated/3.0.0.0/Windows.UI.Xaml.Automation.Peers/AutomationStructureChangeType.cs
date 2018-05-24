#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AutomationStructureChangeType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChildAdded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChildRemoved,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChildrenInvalidated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChildrenBulkAdded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChildrenBulkRemoved,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChildrenReordered,
		#endif
	}
	#endif
}
