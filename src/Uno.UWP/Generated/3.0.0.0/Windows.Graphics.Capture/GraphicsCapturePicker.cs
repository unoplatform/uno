#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GraphicsCapturePicker 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public GraphicsCapturePicker() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.GraphicsCapturePicker", "GraphicsCapturePicker.GraphicsCapturePicker()");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCapturePicker.GraphicsCapturePicker()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Capture.GraphicsCaptureItem> PickSingleItemAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GraphicsCaptureItem> GraphicsCapturePicker.PickSingleItemAsync() is not implemented in Uno.");
		}
		#endif
	}
}
