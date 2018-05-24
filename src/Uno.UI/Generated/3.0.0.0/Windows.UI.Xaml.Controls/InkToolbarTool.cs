#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InkToolbarTool 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BallpointPen,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pencil,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Highlighter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Eraser,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CustomPen,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CustomTool,
		#endif
	}
	#endif
}
