#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GraphicsCaptureItem 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string GraphicsCaptureItem.DisplayName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20GraphicsCaptureItem.DisplayName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.SizeInt32 Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member SizeInt32 GraphicsCaptureItem.Size is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SizeInt32%20GraphicsCaptureItem.Size");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureItem.DisplayName.get
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureItem.Size.get
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureItem.Closed.add
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureItem.Closed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Capture.GraphicsCaptureItem TryCreateFromWindowId( global::Windows.UI.WindowId windowId)
		{
			throw new global::System.NotImplementedException("The member GraphicsCaptureItem GraphicsCaptureItem.TryCreateFromWindowId(WindowId windowId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=GraphicsCaptureItem%20GraphicsCaptureItem.TryCreateFromWindowId%28WindowId%20windowId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Capture.GraphicsCaptureItem TryCreateFromDisplayId( global::Windows.Graphics.DisplayId displayId)
		{
			throw new global::System.NotImplementedException("The member GraphicsCaptureItem GraphicsCaptureItem.TryCreateFromDisplayId(DisplayId displayId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=GraphicsCaptureItem%20GraphicsCaptureItem.TryCreateFromDisplayId%28DisplayId%20displayId%29");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Capture.GraphicsCaptureItem.CreateFromVisual(Windows.UI.Composition.Visual)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Capture.GraphicsCaptureItem, object> Closed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.GraphicsCaptureItem", "event TypedEventHandler<GraphicsCaptureItem, object> GraphicsCaptureItem.Closed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Capture.GraphicsCaptureItem", "event TypedEventHandler<GraphicsCaptureItem, object> GraphicsCaptureItem.Closed");
			}
		}
		#endif
	}
}
