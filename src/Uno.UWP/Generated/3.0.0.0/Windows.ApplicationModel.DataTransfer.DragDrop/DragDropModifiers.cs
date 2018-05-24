#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer.DragDrop
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DragDropModifiers 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Shift,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Control,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Alt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftButton,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MiddleButton,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightButton,
		#endif
	}
	#endif
}
