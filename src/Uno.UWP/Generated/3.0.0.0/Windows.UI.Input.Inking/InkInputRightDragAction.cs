#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InkInputRightDragAction 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeaveUnprocessed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllowProcessing,
		#endif
	}
	#endif
}
